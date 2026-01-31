using System;
using MimeKit;
using MimeKit.Utils;
using Ical;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;

public class CalendarService
{
    public List<Appointment> ExtractAppointments(MimeMessage message)
    {
        var appointments = new List<Appointment>();

        // Alle Body-Teile der Nachricht finden, die den MIME-Typ "text/calendar" haben
        var calendarParts = message.BodyParts
             .Where(p =>
                 (p is TextPart text && text.ContentType.MimeType == "text/calendar") ||
                 (p is MimePart part && part.ContentType.MimeType == "text/calendar"))
             .ToList();

        // Foreach Schleife mit alle gefundenen Kalenderteile
        foreach (var part in calendarParts)
        {
            string icsContent = null;

            if (part is TextPart textPart)
            {
                icsContent = textPart.Text; // Einfach Text extrahieren
            }
            else if (part is MimePart mimePart)
            {
                // Inhalt dekodieren beim MIME-Teil
                using var memoryStream = new MemoryStream();
                mimePart.Content.DecodeTo(memoryStream);
                memoryStream.Position = 0;

                using var reader = new StreamReader(memoryStream);
                icsContent = reader.ReadToEnd();
            }
            // Wenn kein Inhalt vorhanden ist einfach continue
            if (string.IsNullOrWhiteSpace(icsContent))
                continue;

            // ICS Inhalt in Kalnderobjekt parsen
            var calendar = Calendar.Load(icsContent);

            // Foreach Schleife um alle Kalender-Events in Appointment Objekte umzuwandeln
            foreach (var evt in calendar.Events)
            {
                var appointment = new Appointment
                {
                    IcalUid = evt.Uid,
                    Subject = evt.Summary,
                    Description = evt.Description,
                    StartTime = evt.DtStart.AsSystemLocal,
                    EndTime = evt.DtEnd.AsSystemLocal,
                    Location = evt.Location,
                    Attendees = new List<string>()
                };

                // Foreach Schleife für jeden eingeladenen Teilnehmer
                foreach (var participant in evt.Attendees)
                {
                    var name = participant.Value.OriginalString; // Nur Email ausgeben
                    appointment.Attendees.Add(name);
                }
                appointments.Add(appointment);
            }
        }
        return appointments;
    }
}