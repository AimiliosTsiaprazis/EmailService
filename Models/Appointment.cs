using System;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

// Supabase Inheritance with Basemodel, using Supabase Postgrest
[Table("meeting")]
public class Appointment:BaseModel
{
    [Column("ical_uid")]
    public string IcalUid { get; set; }
    public string EmailId { get; set; }

    [Column("subject")]
    public string Subject { get; set; }

    [Column("description")]
    public string Description { get; set; }

    [Column("starttime")]
    public DateTime StartTime { get; set; }

    [Column("endtime")]
    public DateTime EndTime { get; set; }

    [Column("location")]
    public string Location { get; set; }

    [Column("organizing_company")]
    public string OrganizingCompany { get; set; }

    [Column("meeting_status")]
    public string MeetingsStatus { get; set; }

    [Column("contact_person")]
    public string ContactPerson { get; set; }

    [Column("visitor_company")]
    public string VisitorCompany { get; set; }
    public List<string> Attendees { get; set; }
}