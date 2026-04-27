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

        // ------------------------------------------------------
        // 🔵 Vytvoření Meet události + barva podle studenta (bez PATCH)
        // ------------------------------------------------------
        public async Task<(string MeetLink, string EventId)> CreateMeetEventAsync(
            DateTime start,
            DateTime end,
            string title,
            string studentEmail,
            string teacherEmail,
            string studentFirstName,
            string studentLastName,
            string studentHexColor
        )
        {
            var initial = studentFirstName[0];

            // 1️⃣ převedeme HEX barvu na Google colorId
            var colorId = MapHexToGoogleColorId(studentHexColor);

            // 2️⃣ vytvoříme událost s barvou už při INSERT
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

                ColorId = colorId, // ← 🔥 barva už při vytvoření

                Transparency = "opaque",

                Attendees = new List<EventAttendee>
                {
                    new EventAttendee { Email = studentEmail },
                    new EventAttendee { Email = teacherEmail },
                    new EventAttendee { Email = "zakalois@gmail.com" },
                    new EventAttendee { Email = "zakalois@ucitelzak.eu" }


                },

                ConferenceData = new ConferenceData
                {
                    CreateRequest = new CreateConferenceRequest
                    {
                        RequestId = Guid.NewGuid().ToString()
                    }
                }
            };

            // 3️⃣ vytvoření události
            var request = _calendar.Events.Insert(ev, "zakalois@ucitelzak.eu");
            request.ConferenceDataVersion = 1;

            var created = await request.ExecuteAsync();

            // 4️⃣ vracíme Meet link + EventId
            return (created.HangoutLink, created.Id);
        }

        // ------------------------------------------------------
        // 🔵 Mazání události + volba odeslat / neodeslat zrušení
        // ------------------------------------------------------
        public async Task DeleteEventAsync(string eventId, bool notifyStudent)
        {
            var request = _calendar.Events.Delete("zakalois@ucitelzak.eu", eventId);

            request.SendUpdates = notifyStudent
                ? EventsResource.DeleteRequest.SendUpdatesEnum.All
                : EventsResource.DeleteRequest.SendUpdatesEnum.None;

            await request.ExecuteAsync();
        }

        // ------------------------------------------------------
        // 🔵 Mapování HEX → Google Calendar colorId (1–11)
        // ------------------------------------------------------
        private string MapHexToGoogleColorId(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return "1";

            string[] palette = new[]
            {
                "#FFCDD2","#F8BBD0","#E1BEE7","#D1C4E9","#C5CAE9",
                "#BBDEFB","#B3E5FC","#B2EBF2","#B2DFDB","#C8E6C9",
                "#DCEDC8","#F0F4C3","#FFF9C4","#FFECB3","#FFE0B2",
                "#FFCCBC","#D7CCC8","#CFD8DC","#F5F5F5","#E0F7FA",
                "#E8F5E9","#FFF3E0","#F3E5F5","#EDE7F6","#E1F5FE"
            };

            int index = Array.IndexOf(palette, hex);

            if (index < 0)
                return "1";

            int googleId = (index % 11) + 1;

            return googleId.ToString();
        }
    }
}
