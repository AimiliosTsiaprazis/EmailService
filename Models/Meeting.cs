using System;

// Struktur für die Supabase RPC funktion (meeting)
public class Meeting
{
    public string ical_uid { get; set; }
    public string subject { get; set; }
    public DateTime starttime { get; set; }
    public DateTime endtime { get; set; }
    public string organizing_company { get; set; }  // Name als string
    public string meeting_status { get; set; }      // Status ID als string
    public string contact_person { get; set; }      // full_name
    public string visitor_company { get; set; }
    public string location { get; set; }
}
