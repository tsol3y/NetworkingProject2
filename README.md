# Networking Project 2 - HTTP Server Using TCP
This code creates an HTTP server that directly communicates using TCP in C#.
The HTTP server in this code creates a website on which people can view, submit, and update the status of multiple issues.
This repository includes the code the server runs (Server.cs) and the HTML for the webpage (ComplaintPage.html and dropdown.html) as well as the project file (Proj2.csproj).
## How to build and run this code:
Install the latest version of [.NET Core SDK] (https://dotnet.microsoft.com/download) (we used version 3.1.101). Clone or download this repository.
In the directory of the repository, run this line:
```
dotnet run
```
Once this is running, open web browser, go to [this link](http://localhost:8080/ComplaintPage.html).

## Extra Feature
The client's page is refreshed every approximately 10 seconds so that it receives live updates of issues being added and updated.

### Helpful Sources in our work:
- [Making sure that unwanted html isn't being added to issues](https://stackoverflow.com/questions204646how-to-validate-that-a-string-doesnt-contain-html-using-c-sharp)
- [Deciphering the request we received](https://www.geeksforgeeks.org/c-sharp-get-an-icollection-containing-values-in-ordereddictionary/)
- [Live Updates to our website](https://stackoverflow.com/questions/8711888/auto-refresh-code-in-html-using-meta-tags)

### Known Deficiencies
Once this server is cut, all the created issues will be lost. Additionally, although our server does not allow HTML to be submitted, it does not  allow any text that looks like HTML, even if that text has legitimate information in it (for exmple, if one tried to enter <tr>My screen is broken<\tr into the "New Issue" form, the server would ignore the valid "My screen is broken" part.).

