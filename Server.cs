using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace http_example
{
    class Program
    {
        static Dictionary<string,List<List<string>>> issues = new Dictionary<string,List<List<string>>>();
        static string dropDown = File.ReadAllText("static/dropdown.html"); 
        static int numIssues = 0;
        static async void RunClient(TcpClient client)
        {
            
            try
            {
                var stream = client.GetStream();
                using (var streamReader = new StreamReader(stream))
                {
                    var request = await streamReader.ReadLineAsync();
                    if (request == null)
                        return;
                    Console.WriteLine($"> {request}");
                    var requestLine1 = request.Split(" ");
                    

                    switch(requestLine1[0]){
                        case "GET":
                            if (requestLine1[1] == "/" || requestLine1[1] == "/ComplaintPage.html"){
                                SendTemplate(stream, "static/ComplaintPage.html", issues);
                            }
                            else{
                                Send404(stream);
                            }
                            break;
                        case "POST":
                            if(requestLine1[1] == "/" || requestLine1[1] == "/ComplaintPage.html"){
                                var headers = new Dictionary<string, string>();
                                while (true)
                                {
                                    var line = await streamReader.ReadLineAsync();
                                    var kv = line.Split(":", 2);
                                    if (kv.Length == 2)
                                        headers.Add(kv[0].Trim(), kv[1].Trim());
                                    Console.WriteLine($"> {line}");
                                    if (string.IsNullOrEmpty(line))
                                        break;
                                }
                                if(headers.ContainsKey("Content-Length")){
                                    int l;
                                    if (int.TryParse(headers["Content-Length"], out l) && 0 <= l && l < 150)
                                    {
                                        var cs = new char[l];
                                        await streamReader.ReadAsync(cs);
                                        var formDataRaw = new string(cs);
                                        Console.WriteLine($"> --- {formDataRaw}");
                                        var formData = UrlDecode(formDataRaw);
                                        var keys = formData.Keys;
                                        var values = formData.Values;
                                        String actualKey = "";
                                        if(values.Count > 0){
                                            String[] myKey = new String[keys.Count];
                                            keys.CopyTo(myKey,0);
                                            actualKey = myKey[0];
                                        }
                                        if (formData.ContainsKey("newIssue"))
                                        {
                                            lock (issues)
                                            {
                                                issues["list"].Add(createNewRequest(formData["newIssue"]));
                                                numIssues++;
                                            }
                                        }
                                        else if (actualKey.Contains("dropdown")){
                                            lock (issues)
                                            {
                                                changeStatus(actualKey, formData[actualKey]);
                                            }
                                        }

                                        if (formData.ContainsKey("next"))
                                        {
                                            Send302(stream, "/" + formData["next"]);
                                            return;
                                        }
                                    }
                                    Send302(stream, "/ComplaintPage.html");
                                }
                            }
                            else{
                                Send404(stream);
                            }  
                            break;
                        case "HEAD":
                            if(requestLine1[1] == "/" || requestLine1[1] == "/ComplaintPage.html"){
                                SendTemplate(stream, "static/ComplaintPage.html", issues, false);
                            }
                            else 
                            {
                                Send404(stream);
                            }
                            break;
                        default:
                            Send405(stream);
                            break;
                    }

                }
            }
            
            catch (IOException e)
            {
                Console.WriteLine(e);
            }
        }

        private static void Send405(NetworkStream stream)
        {
            stream.Write(Encoding.ASCII.GetBytes($"HTTP/1.1 405 METHOD NOT ALLOWED\r\nAllow: GET, POST, HEAD\r\n\r\n"));
        }

        static bool ContainsHTML(string CheckString)
        {
            return Regex.IsMatch(CheckString, "<(.|\n)*?>");
        }
        
        static List<string> createNewRequest(string newIssue)
        {
            List<string> Request = new List<string>();
            if(!ContainsHTML(newIssue)){
                Request.AddRange(new String[]{"<tr>",$"<td>{newIssue}</td>", "<td>Open</td>",
                $"<td><form method=\"POST\"><select name=\"dropdown {numIssues}\">",dropDown,"</tr>"});
            }
            
            return Request;
        }
        static void changeStatus(string dropDownNum, string newStatus)
        {
            int index;
            if(newStatus.Equals("Select Option") == false && int.TryParse(dropDownNum.Split(" ", 2)[1], NumberStyles.Integer, null, out index)){
                issues["list"][index][2] = $"<td>{newStatus}</td>";
            }
        }
        private static IDictionary<string,string> UrlDecode(string s)
        {
            var d = new Dictionary<string,string>();
            var pairs = s.Split("&");
            foreach (var p in pairs)
            {
                var kv = p.Split("=", 2);
                if (kv.Length == 2)
                {
                    var key = unescape(kv[0]);
                    var value = unescape(kv[1]);
                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                        d[key] = value;
                }
            }
            return d;
        }

        enum State
        {
            Normal,
            SawPercent,
            SawFirstDigit,
        }

        private static string unescape(string s)
        {
            var sb = new StringBuilder();
            var state = State.Normal;
            var digits = "";
            foreach (var c in s)
            {
                switch (state)
                {
                    default:
                    case State.Normal:
                        switch (c)
                        {
                            case '+': sb.Append(' '); break;
                            case '%': state = State.SawPercent; break;
                            default:
                                sb.Append(c);
                                break;
                        }
                        break;
                    case State.SawPercent:
                        if (char.IsDigit(c) || "abcdef".Contains(char.ToLower(c)))
                            digits = c.ToString();
                        else
                            return "";
                        state = State.SawFirstDigit;
                        break;
                    case State.SawFirstDigit:
                        if (char.IsDigit(c) || "abcdef".Contains(char.ToLower(c)))
                            digits += c;
                        else
                            return "";
                        int v;
                        if (int.TryParse(digits, NumberStyles.HexNumber, null, out v))
                            sb.Append((char)v);
                        state = State.Normal;
                        break;
                }
            }
            return sb.ToString();
        }

        

        private static void SendTemplate(NetworkStream stream, string path, Dictionary<string, List<List<string>>> dict, bool sendAll=true)
        {
            var fi = new FileInfo(path);
            var contentType = GetContentType(fi.Extension);
            var sendBytes = new List<byte[]>();
            var contentLength = 0;
            using (var f = new StreamReader(fi.OpenRead()))
            {
                var line = f.ReadLine();
                while (line != null)
                {
                    
                    if (line.StartsWith("$"))
                    {
                        if(dict.Count > 0){
                            var vs = line.Split(" ", 2);
                            foreach (var s in dict[vs[1]])
                            {
                                foreach(var html in s){
                                    var bs = Encoding.UTF8.GetBytes(html);
                                    contentLength += bs.Length;
                                    sendBytes.Add(bs);
                                }
                            }
                        }
                    }
                    else
                    {
                        var bs = Encoding.UTF8.GetBytes(line);
                        contentLength += bs.Length;
                        sendBytes.Add(bs);
                    }
                    line = f.ReadLine();
                }
            }
            stream.Write(Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Length: {contentLength}\r\n{contentType}\r\n\r\n"));
            if(sendAll)
            {
                foreach (var bs in sendBytes)
                stream.Write(bs);
            }
        }

        private static void Send404(NetworkStream stream)
        {
            stream.Write(Encoding.ASCII.GetBytes($"HTTP/1.1 404 NOT FOUND\r\n\r\n"));
        }

        private static void Send302(NetworkStream stream, string path)
        {
            stream.Write(Encoding.ASCII.GetBytes($"HTTP/1.1 302 FOUND\r\nLocation: {path}\r\n\r\n"));
        }

        
        private static void SentContent(NetworkStream stream, string content)
        {
            var bs = Encoding.UTF8.GetBytes(content);
            var ct = GetContentType(".html");
            stream.Write(Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Length: {bs.Length}\r\n{ct}\r\n\r\n"));
            stream.Write(bs);
        }

        private static async Task SendFile(NetworkStream stream, string path)
        {
            var fi = new FileInfo(path);
            var ct = GetContentType(fi.Extension);
            stream.Write(Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Length: {fi.Length}\r\n{ct}\r\n\r\n"));
            using (var f = fi.OpenRead())
                await f.CopyToAsync(stream);
        }

        private static string GetContentType(string extension)
        {
            switch (extension)
            {
                case ".html":
                case ".htm":
                    return "Content-Type: text/html; charset=UTF-8";
                case ".png":
                    return "Content-Type: image/png";
                default:
                    return "Content-Type: text/plain";
            }
        }

        static async Task Main(string[] args)
        {
            var ip = IPAddress.Parse("127.0.0.1");
            var port = 8080;
            
            issues["list"] = new List<List<string>>();

            var server = new TcpListener(ip, port);
            server.Start();

            while (true)
            {
                try
                {
                    var client = await server.AcceptTcpClientAsync();
                    ThreadPool.QueueUserWorkItem(RunClient, client, false);
                }
                catch (IOException e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}