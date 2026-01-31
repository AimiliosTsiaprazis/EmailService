using System;

public class EmailSettings{
    public string ImapServer{get;set;}
    public int Port {get;set;}
    public string EmailAddress {get;set;}
    public string Password {get;set;}
    public List<string> AllowedDomains {get;set;}
    public string SmtpServer {get;set;}
    public int SmtpPort {get;set;}
}