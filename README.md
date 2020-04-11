# Networking Project 2
This code creates an HTTP server that directly communicates with TCP in C#.
The HTTP server in this code creates a website on which people can submit issues.
The website also displays previous issues and the status of those issues.
People viewing the website can update the status of the issues as well.
## How to build this code:
To build this code, you must have a TCP listener to receive data from.
The listener will receive three types of data: GET requests, POST requests, HEAD requests.
Create a function that will to interpret this data.
For GET requests, create a function that will stream the path that is requested with the appropriate data (In our project, this is the "SendTemplate" function).
There are two types of POST requests you will receive: requests to add an issue, and requests to update an existing issue. 
- For adding an issue, create a function that will add the issue to a dictionary variable that exists throughout our code. This function should add the html to add the function to the table that will be displayed too.
- For updating an issue, create a function that will update the existing issue in the dictionary.
    * In this part, you must take into consideration how to identify the issues in the dictionary.
        + In ours, we attach a number to each, based on the order that we receive the issue, and then locate the issue from that.

The body of the HTML code should include: 

- A table (This will be added to from the code above)
- A form to add new issues. 
## How to run this code:
The HTML Code should be in a subfolder titled static.
In the directory of the C# file, run this line:
```
dotnet run
```
Once this is running, open web browser, and click localhost:8080/ComplaintPage.html



