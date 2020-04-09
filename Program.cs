using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace http_example
{
    class Program
    {
        static Dictionary<string,IEnumerable<string>> dict = new Dictionary<string,IEnumerable<string>>();
        static async void RunClient(TcpClient c)
        {
            string dropDown = File.ReadAllText("static/dropdown.html"); 
            try
            {
                var stream = c.GetStream();
                using (var tr = new StreamReader(stream))
                {
                    var request = await tr.ReadLineAsync();
                    if (request == null)
                        return;
                    Console.WriteLine($"> {request}");
                    var vs = request.Split(" ");
                    //if (vs.Length == 3)
                    //{
                    //    Console.WriteLine($"* Method: {vs[0]}, path: {vs[1]}, version: {vs[2]}");
                   // }

                    switch(vs[0]){
                        case "GET":
                            
                            if (vs[1] == "/" || vs[1] == "/ComplaintPage.html"){
                                SendTemplate(stream, "static/ComplaintPage.html", dict);
                            }
                            break;
                        case "POST":
                            if(vs[1] == "/ComplaintPage.html"){
                                var headers = new Dictionary<string, string>();
                                while (true)
                                {
                                    var line = await tr.ReadLineAsync();
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
                                        await tr.ReadAsync(cs);
                                        var formDataRaw = new string(cs);
                                        Console.WriteLine($"> --- {formDataRaw}");
                                        var formData = UrlDecode(formDataRaw);
                                        
                                        if (formData.ContainsKey("newItem"))
                                        {
                                            lock (dict)
                                            {
                                                var items = dict["list"] as List<string>;
                                                items.Add("<tr>");
                                                items.Add($"<td>{formData["newItem"]}</td>");
                                                items.Add("<td>Broken</td>");
                                                items.Add("<td><form method=\"POST\">")
                                                items.Add("<select name=\"dropdown\">");
                                                items.Add(dropDown);
                                                items.Add("</tr>");
                                            }
                                        }
                                        else if (formData.ContainsKey("dropdown")){
                                            lock (dict)
                                            {
                                                var items = dict["list"] as List<string>;
                                                
                                                
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
                                
                            

                            break;
                        case "HEAD":
                            break;
                        default:
                            break;
                    }

                    

                    /*if (vs[1] == "/" || vs[1] == "/index.html")
                        await SendFile(stream, "static/index.html");
                    else if (vs[1] == "/cool.png")
                        await SendFile(stream, "static/cool.png");
                    else if (vs[1] == "/template.html")
                        SendTemplate(stream, "static/template.html", dict);
                    else if (vs[1] == "/shop-static.html")
                        await SendFile(stream, "static/shop-static.html");
                    else if (vs[1] == "/all-items")
                    {
                        var content = "";
                        lock (dict)
                        {
                            var items = dict["list"] as List<string>;
                            content = items.Aggregate("", (a,b) => a + b);
                        }
                        SentContent(stream, content);
                    }
                    else if (vs[1] == "/ComplaintPage.html")
                        if (vs[0] == "POST" && headers.ContainsKey("Content-Length"))
                        {
                            int l;
                            if (int.TryParse(headers["Content-Length"], out l) && 0 <= l && l < 150)
                            {
                                var cs = new char[l];
                                await tr.ReadAsync(cs);
                                var formDataRaw = new string(cs);
                                Console.WriteLine($"> --- {formDataRaw}");
                                var formData = UrlDecode(formDataRaw);
                                
                                if (formData.ContainsKey("newItem"))
                                {
                                    lock (dict)
                                    {
                                        var items = dict["list"] as List<string>;
                                        items.Add("<tr>");
                                        items.Add($"<td>{formData["newItem"]}</td>");
                                        items.Add("<td>Broken</td>");
                                        items.Add(dropDown);
                                        items.Add("</tr>");
                                    }
                                }
                                else if (formData.ContainsKey("option")){
                                    lock (dict)
                                    {
                                        var items = dict["list"] as List<string>;
                                        
                                        
                                    }
                                }

                                if (formData.ContainsKey("next"))
                                {
                                    Console.WriteLine("TEST 4");
                                    Send302(stream, "/" + formData["next"]);
                                    return;
                                }
                            }
                            Send302(stream, "/ComplaintPage.html");
                        }
                        else
                            SendTemplate(stream, "static/ComplaintPage.html", dict);
                    else
                        Send404(stream);*/
                }
            }
            
            catch (IOException e)
            {
                Console.WriteLine(e);
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
                    var k = unescape(kv[0]);
                    var v = unescape(kv[1]);
                    if (!string.IsNullOrEmpty(k) && !string.IsNullOrEmpty(v))
                        d[k] = v;
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

        /*private static void SendTemplate(NetworkStream stream, string path, Dictionary<string, IEnumerable<string>> dict)
        {
            var fi = new FileInfo(path);
            var ct = GetContentType(fi.Extension);
            var ls = new List<byte[]>();
            var n = 0;
            using (var f = new StreamReader(fi.OpenRead()))
            {
                var l = f.ReadLine();
                while (l != null)
                {
                    if (l.StartsWith("$"))
                    {
                        var vs = l.Split(" ", 2);
                        foreach (var s in dict[vs[1]])
                        {
                            var bs = Encoding.UTF8.GetBytes(s);
                            n += bs.Length;
                            ls.Add(bs);
                        }
                    }
                    else
                    {
                        var bs = Encoding.UTF8.GetBytes(l);
                        n += bs.Length;
                        ls.Add(bs);
                    }
                    l = f.ReadLine();
                }
            }
            stream.Write(Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Length: {n}\r\n{ct}\r\n\r\n"));
            
            foreach (var bs in ls)
                stream.Write(bs);
        }*/

        private static void SendTemplate(NetworkStream stream, string path, Dictionary<string, IEnumerable<string>> dict)
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
                                var bs = Encoding.UTF8.GetBytes(s);
                                contentLength += bs.Length;
                                sendBytes.Add(bs);
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
            foreach (var bs in sendBytes)
                stream.Write(bs);
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

            dict["title"] = new string[]{"Hello world"};
            dict["issues"] = new string[]{"Things aren't working", "Hi", "other"};
            dict["list"] = new List<string>();

            var server = new TcpListener(ip, port);
            server.Start();

            while (true)
            {
                try
                {
                    var c = await server.AcceptTcpClientAsync();
                    ThreadPool.QueueUserWorkItem(RunClient, c, false);
                }
                catch (IOException e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
