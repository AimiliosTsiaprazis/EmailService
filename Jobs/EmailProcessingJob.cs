using System;
using Ical.Net.DataTypes;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.Extensions.Options;
public class EmailProcessingJob
{
    private readonly EmailService _emailService;
    private readonly CalendarService _calendarService;
    private readonly AppointmentRepository _appointmentRepository;
    private readonly List<string> _allowedDomains;

    public EmailProcessingJob(EmailService emailService, CalendarService calendarService, AppointmentRepository appointmentRepository, IOptions<AllowedDomainsConfig> config)
    {
        _emailService = emailService;
        _calendarService = calendarService;
        _appointmentRepository = appointmentRepository;
        _allowedDomains = config.Value.AllowedDomains;
    }

    // Funktion -> Organisierte Firmen aus der Email extrahieren (digital worx & asvin)
    private string GetOrganizingCompanyFromEmail(string email)
    {
        var domain = email.Split('@').Last().ToLower();

        // Domainteil ohne endung holen (aus digital-worx.de -> digital worx)
        if (!_allowedDomains.Contains(domain))
        {
            if (domain == "firma-1.de")
            {
                return "firma 1";
            }
            else if (domain == "firma-2.de")
            {
                return "firma 2";
            }
            else
            {
                return null;
            }
        }
        return null;
    }

    // Funktion -> Besucher Firmen aus der Email extrahieren (verschiedene Firmen für jeden Besucher)
    private string GetVisitorCompanyFromEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var emailParts = email.Split('@');
        if (emailParts.Length == 2)
        {
            var domain = emailParts[1];
            var domainName = domain.Split('.')[0];
            var company = domainName.Replace("-", " ").Replace("_", " ");
            return company;
        }
        return null;
    }

    //Funktion für den Job -> Verarbeitungsjob ausführen
    public async Task ExecuteAsync()
    {
        var emails = await _emailService.FetchUnreadEmailsAsync(); //Abrufen aller ungelesenen Emails

        //Foreach schleife über alle ungelesene Emails
        foreach (var email in emails)
        {
            var appointments = _calendarService.ExtractAppointments(email); //Termine extrahieren
                                                                            //Schleife über alle extrahierten Termine
            foreach (var appointment in appointments)
            {
                appointment.EmailId = email.MessageId;

                // Extrahierung des Email-Absenders
                var organizer = email.From.Mailboxes.FirstOrDefault();
                // ContactPerson nur den ersten Teil für die Datenbank auslesen (vorname-nachname | unternehmen)
                if (organizer != null && !string.IsNullOrEmpty(organizer.Name))
                {
                    var nameOnly = organizer.Name.Split('|')[0].Trim();
                    appointment.ContactPerson = nameOnly;

                    //OrganizingCompany aus der Email mit der Funktion GetOrganizingCompanyFromEmail auslesen
                    var organizerEmailAdress = organizer.Address; // EmailAdresse des Absenders auslesen
                    var organizingCompany = GetOrganizingCompanyFromEmail(organizerEmailAdress); // Unternehmen auslesen
                    appointment.OrganizingCompany = organizingCompany; // Daten übertragen

                    // Appointment.VisitorCompany -> die Erste Email von den Teilnehmern rauslesen
                    if (appointment.Attendees.Any())
                    {
                        var firstAttendeeEmail = appointment.Attendees.First();
                        appointment.VisitorCompany = GetVisitorCompanyFromEmail(firstAttendeeEmail);
                    }
                }
                // Festkodierter Meetingstatus -> als 'wird erfasst'
                appointment.MeetingsStatus = "3"; // id Status

                // Meeting objekt mit Daten aus Appointment ausfüllen
                var meeting = new Meeting
                {
                    ical_uid = appointment.IcalUid,
                    subject = appointment.Subject,
                    starttime = appointment.StartTime,
                    endtime = appointment.EndTime,
                    organizing_company = appointment.OrganizingCompany,
                    meeting_status = appointment.MeetingsStatus,
                    contact_person = appointment.ContactPerson,
                    visitor_company = appointment.VisitorCompany,
                    location = appointment.Location
                };

                // attendees zur visitor parsen (Teilnehmer von Email)
                var visitor = appointment.Attendees.Select(attendee =>
                {
                    var cleanAttendee = attendee.Trim();
                    if (cleanAttendee.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                    {
                        cleanAttendee = cleanAttendee.Substring("mailto:".Length);
                    }

                    var match = Regex.Match(cleanAttendee, @"(?<name>.*?)\s*<(?<email>[^>]+)>"); // Regex match in Name & Email
                    var fullName = ""; //name
                    var emailAddress = ""; //email

                    if (match.Success)
                    {
                        fullName = match.Groups["name"].Value.Trim(); // Name extrahieren
                        emailAddress = match.Groups["email"].Value.Trim(); // Email extrahieren
                    }

                    else
                    {
                        // Beim keinem success match dann die email teilen und den Namen auslesen
                        emailAddress = cleanAttendee;
                        fullName = emailAddress.Split('@').FirstOrDefault(); // Teil vor dem @
                    }
                    var company = GetVisitorCompanyFromEmail(emailAddress); //VisitorCompany - Firma auslesen

                    // Visitor objekt mit Daten übertragen - attentee
                    return new Visitor
                    {
                        full_name = fullName,
                        email = emailAddress,
                        visitor_company = company
                    };
                }).ToList();

                // Kombination Meeting & Visitor objekte als ein gemeinsames payload objekt
                var payload = new MeetingWithVisitors
                {
                    meeting = meeting,
                    visitors = visitor
                };

                // Meeting mit Teilnehmer importieren - Datenbank Supabase
                try
                {
                    var result = await _appointmentRepository.UpsertAppointment(payload);
                    Console.WriteLine($"Imported meeting with ID: {result}");

                    // Meeting id aus result als zahl extrahieren
                    var meetingId = result?.GetProperty("meeting_id").GetInt32();
                    // Einladungslink schicken mit Kommentar(Appointmentlink)
                    var link = $"https://besucherTestApp/meetings/{meetingId}";
                    await _emailService.SendEmailAsync(organizer.Address, $"Meeting Confirmation: {appointment.Subject}",
                     $"Hey {appointment.ContactPerson}, your meeting has been successfully scheduled.", link);
                }
                // Fehlerbehandlung beim Importieren des Meetings und Email verschickung an Organisator und Entwickler
                catch (Exception ex)
                {
                    Console.WriteLine($"Error importing meeting: {ex.Message}");
                    string organizerEmail = organizer.Address;

                    string errorText = "Ein Fehler ist im automatisierten Outlook EmailService aufgetreten.<br/><br/>" +
                    "<strong>Fehlermeldung:</strong><br/>" + ex.Message + "<br/><br/>" +
                    "<strong>StackTrace:</strong><br/>" + ex.StackTrace;

                    await _emailService.SendErrorNotificationAsync(organizerEmail, "Fehlermeldung im Outlook-EmailService", errorText);
                }
            }
        }
    }
}
