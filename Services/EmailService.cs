using System;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MimeKit;
using Microsoft.Extensions.Options;
using Ical.Net.DataTypes;

public class EmailService
{
    // EmailSettings zugreifen
    private readonly EmailSettings _settings;
    public EmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }
    // Funktion Email-Bestätigung mit einladungslink
    public async Task SendEmailAsync(string receiverEmail, string subject, string body, string appointmentLink, string cc = null)
    {
        // Verbindung SmtpClient
        using var SmtpClient = new SmtpClient();
        await SmtpClient.ConnectAsync(_settings.SmtpServer, _settings.SmtpPort, MailKit.Security.SecureSocketOptions.SslOnConnect);
        await SmtpClient.AuthenticateAsync(_settings.EmailAddress, _settings.Password);

        // Nachricht definieren
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("VisitorApp-EmailService", _settings.EmailAddress));
        message.To.Add(new MailboxAddress("", receiverEmail));

        // CC hinzufügen wenn angegeben
        if (!string.IsNullOrEmpty(cc))
        {
            message.Cc.Add(new MailboxAddress("", cc));
        }

        message.Subject = subject;


        // Einladungslink ins body
        message.Body = new TextPart("html")
        {
            Text = $"{body}<br/><br/><a href='{appointmentLink}'>{appointmentLink}</a>"
        };

        // Email schicken und ausloggen
        await SmtpClient.SendAsync(message);
        await SmtpClient.DisconnectAsync(true);
    }


    // Funktion ungelesener Emails
    public async Task<List<MimeMessage>> FetchUnreadEmailsAsync()
    {
        //IMAP-Client erzeugen und verbindung herstellen
        using var client = new ImapClient();
        await client.ConnectAsync(_settings.ImapServer, _settings.Port, true);
        await client.AuthenticateAsync(_settings.EmailAddress, _settings.Password);

        // Posteingang
        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadWrite);

        var messages = await inbox.SearchAsync(SearchQuery.NotSeen);

        //Liste zur Rückgabe der gefilterten Emails
        var result = new List<MimeMessage>();

        // Foreach schleife für jede ungelesene Email, Emails abrufen - Domain prüfen - als gelesen markieren
        foreach (var uid in messages)
        {
            var message = await inbox.GetMessageAsync(uid);
            if (_settings.AllowedDomains.Any(domain => message.From.Mailboxes.Any(m => m.Address.EndsWith(domain))))
            {

                // Email auf Absage prüfen -> damit die Absage nicht nochmal als Termin gespeichert wird
                string[] canceledKeywords = { "Absage", "Canceled", "Cancelled", "Abgesagt" };

                if (message.Subject != null && canceledKeywords.Any(kw => message.Subject.Contains(kw, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                result.Add(message);

                await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true);
            }
        }
        await client.DisconnectAsync(true);
        return result;
    }

    public async Task SendErrorNotificationAsync(string organizerEmail, string subject, string body)
    {
        string ccEmail = "aimiliostsiaprazis@gmail.com";

        await SendEmailAsync(organizerEmail, subject, body, null, ccEmail);
    }
}