using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;

namespace Vyuka.Services
{
    public class GoogleCalendarService
    {
        private readonly CalendarService _calendar;

        public GoogleCalendarService(CalendarService calendar)
        {
            _calendar = calendar;
        }

        public async Task<string> CreateMeetEventAsync(
      DateTime start,
      DateTime end,
      string title,
      string studentEmail,
      string teacherEmail,
      string studentFirstName,
      string studentLastName)

        {
            var initial = studentFirstName[0];   // ← TADY tvorba iniciály 
            var ev = new Event
            {
                Summary = $"{title} – {studentLastName} {initial}.",
                Description = "Online lekce přes Google Meet",
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
                Transparency = "opaque",
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

            // 🔥 Workspace kalendář – správný účet
            var request = _calendar.Events.Insert(ev, "zakalois@ucitelzak.eu");

            // 🔥 Bez toho se Meet nevytvoří
            request.ConferenceDataVersion = 1;

            var created = await request.ExecuteAsync();

            // 🔥 Tady je Meet link
            return created.HangoutLink;
        }
    }
}