namespace QrSharp.PayloadTypes;

public static partial class PayloadGenerator
{
    public class CalendarEvent : QrSharp.PayloadGenerator.Payload
    {
        public enum EventEncoding
        {
            // ReSharper disable once InconsistentNaming
            iCalComplete,
            Universal
        }

        private readonly EventEncoding _encoding;
        private readonly string _subject, _description, _location, _start, _end;

        /// <summary>
        ///     Generates a calender entry/event payload.
        /// </summary>
        /// <param name="subject">Subject/title of the calender event</param>
        /// <param name="description">Description of the event</param>
        /// <param name="location">Location (lat:long or address) of the event</param>
        /// <param name="start">Start time of the event</param>
        /// <param name="end">End time of the event</param>
        /// <param name="allDayEvent">Is it a full day event?</param>
        /// <param name="encoding">Type of encoding (universal or iCal)</param>
        public CalendarEvent(string subject, string description, string location, DateTime start, DateTime end,
            bool allDayEvent, EventEncoding encoding = EventEncoding.Universal)
        {
            _subject = subject;
            _description = description;
            _location = location;
            _encoding = encoding;
            var dtFormat = allDayEvent ? "yyyyMMdd" : "yyyyMMddTHHmmss";
            _start = start.ToString(dtFormat);
            _end = end.ToString(dtFormat);
        }

        public override string ToString()
        {
            var vEvent = $"BEGIN:VEVENT{Environment.NewLine}";
            vEvent += $"SUMMARY:{_subject}{Environment.NewLine}";
            vEvent += !string.IsNullOrEmpty(_description) ? $"DESCRIPTION:{_description}{Environment.NewLine}" : "";
            vEvent += !string.IsNullOrEmpty(_location) ? $"LOCATION:{_location}{Environment.NewLine}" : "";
            vEvent += $"DTSTART:{_start}{Environment.NewLine}";
            vEvent += $"DTEND:{_end}{Environment.NewLine}";
            vEvent += "END:VEVENT";

            if (_encoding == EventEncoding.iCalComplete)
            {
                vEvent =
                    $@"BEGIN:VCALENDAR{Environment.NewLine}VERSION:2.0{Environment.NewLine}{vEvent}{Environment.NewLine}END:VCALENDAR";
            }

            return vEvent;
        }
    }
}