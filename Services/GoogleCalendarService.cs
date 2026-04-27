using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;

public class GoogleCalendarService
{
    private readonly IConfiguration _config;

    public GoogleCalendarService(IConfiguration config)
    {
        _config = config;
    }

    private CalendarService CreateCalendarService()
    {
        var clientId = _config["GoogleOAuth:ClientId"];
        var clientSecret = _config["GoogleOAuth:ClientSecret"];
        var refreshToken = _config["GoogleOAuth:RefreshToken"];

        var credential = GoogleCredential.FromJson($@"
        {{
            ""client_id"": ""{clientId}"",
            ""client_secret"": ""{clientSecret}"",
            ""refresh_token"": ""{refreshToken}"",
            ""type"": ""authorized_user""
        }}");

        return new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "VyukaApp"
        });
    }

    // ⭐ NOVÁ 8-parametrová verze s barvou + jménem studenta
    public async Task<(string MeetLink, string EventId)> CreateMeetEventAsync(
        DateTime start,
        DateTime end,
        string title,
        string studentEmail,
        string teacherEmail,
        string studentFirstName,
        string studentLastName,
        string hexColor)
    {
        var service = CreateCalendarService();

        // převod HEX → Google ColorId
        string colorId = MapHexToGoogleColorId(hexColor);

        var ev = new Event
        {
            Summary = $"{title} – {studentLastName} {studentFirstName[0]}.",
            Description = "Online lekce přes Google Meet",
            ColorId = colorId,
            Start = new EventDateTime
            {
                DateTime = start,
                TimeZone = "Europe/Prague"
            },
            End = new EventDateTime
            {
                DateTime = end,
                TimeZone = "Europe/Prague"
            },
            Attendees = new List<EventAttendee>
            {
                new EventAttendee { Email = studentEmail },
                new EventAttendee { Email = teacherEmail }
            },
            ConferenceData = new ConferenceData
            {
                CreateRequest = new CreateConferenceRequest
                {
                    RequestId = Guid.NewGuid().ToString()
                }
            }
        };

        var request = service.Events.Insert(ev, "zakalois@ucitelzak.eu");
        request.ConferenceDataVersion = 1;

        var created = await request.ExecuteAsync();

        return (created.HangoutLink, created.Id);
    }

    // ⭐ Mazání události – opraveno
    public async Task DeleteEventAsync(string eventId)
    {
        var service = CreateCalendarService();
        var request = service.Events.Delete("zakalois@ucitelzak.eu", eventId);
        await request.ExecuteAsync();
    }

    // ⭐ Mapování HEX → Google ColorId
    private string MapHexToGoogleColorId(string hex)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "#F44336", "11" }, // red
            { "#E91E63", "3" },  // pink
            { "#9C27B0", "6" },  // purple
            { "#673AB7", "9" },  // deep purple
            { "#3F51B5", "1" },  // indigo
            { "#2196F3", "2" },  // blue
            { "#03A9F4", "10" }, // light blue
            { "#00BCD4", "4" },  // cyan
            { "#009688", "5" },  // teal
            { "#4CAF50", "7" },  // green
            { "#8BC34A", "8" }   // light green
        };

        if (map.TryGetValue(hex, out var id))
            return id;

        return "2"; // default: modrá
    }
}
