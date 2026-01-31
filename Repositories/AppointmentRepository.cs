using System;
using System.Text.Json;
using Hangfire.Storage.Monitoring;
using Supabase;

public class AppointmentRepository
{

    // Supabase Client mit Url und Key
    private readonly Supabase.Client _client;
    public AppointmentRepository(string url, string key)
    {
        var options = new Supabase.SupabaseOptions
        {
            AutoConnectRealtime = true
        };
        _client = new Supabase.Client(url, key, options);
        _client.InitializeAsync();
    }

    // Import Funktion für Appointment (meeting) & Visitors
    public async Task<JsonElement?> ImportAppointment(MeetingWithVisitors data)
    {
        // Importieren der Daten durch .rpc Supabase
        var args = new Dictionary<string, object> { { "data", data } };
        var response = await _client.Rpc("import_meeting_with_visitors", args);

        if (response != null && !string.IsNullOrWhiteSpace(response.Content))
        {
            var jsonDoc = JsonDocument.Parse(response.Content);
            return jsonDoc.RootElement.Clone();
        }
        return null;
    }

    // Upsert Funktion für Appointment
    public async Task<JsonElement?> UpsertAppointment(MeetingWithVisitors data)
    {
        var args = new Dictionary<string, object> { { "data", data } };
        var response = await _client.Rpc("upsert_meeting_with_visitors", args);

        if (response != null && !string.IsNullOrEmpty(response.Content))
        {
            using var jsonDoc = JsonDocument.Parse(response.Content);
            return jsonDoc.RootElement.Clone();
        }
        return null;
    }
}