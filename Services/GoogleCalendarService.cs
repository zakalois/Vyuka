using Google;
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
        var calendarOwner = _config["GoogleCalendar:CalendarOwner"];

        string colorId = MapHexToGoogleColorId(hexColor);

        var ev = new Event
        {
            Summary = $"{title} – {studentLastName} {studentFirstName[0]}.",
            Description = "Online lekce přes Google Meet",
            ColorId = colorId,
            Start = new EventDateTime { DateTime = start, TimeZone = "Europe/Prague" },
            End = new EventDateTime { DateTime = end, TimeZone = "Europe/Prague" },
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

        var request = service.Events.Insert(ev, calendarOwner);
        request.ConferenceDataVersion = 1;

        // ⭐ Vypnout Google pozvánky
        request.SendUpdates = EventsResource.InsertRequest.SendUpdatesEnum.None;

        var created = await request.ExecuteAsync();
        return (created.HangoutLink, created.Id);
    }

    public async Task DeleteEventAsync(string eventId)
    {
        var service = CreateCalendarService();
        var calendarOwner = _config["GoogleCalendar:CalendarOwner"];

        var request = service.Events.Delete(calendarOwner, eventId);

        try
        {
            await request.ExecuteAsync();
        }
        catch (GoogleApiException ex)
        {
            if (ex.HttpStatusCode == System.Net.HttpStatusCode.Gone)
                return;
            throw;
        }
    }

    private string MapHexToGoogleColorId(string hex)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "#F44336", "11" }, { "#E91E63", "3" }, { "#9C27B0", "6" },
            { "#673AB7", "9" }, { "#3F51B5", "1" }, { "#2196F3", "2" },
            { "#03A9F4", "10" }, { "#00BCD4", "4" }, { "#009688", "5" },
            { "#4CAF50", "7" }, { "#8BC34A", "8" }
        };

        return map.TryGetValue(hex, out var id) ? id : "2";
    }
}
