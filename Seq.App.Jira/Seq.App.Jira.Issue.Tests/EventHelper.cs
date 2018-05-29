using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Seq.Apps;
using Seq.Apps.LogEvents;

static internal class EventHelper
{
    public static Event<LogEventData> CreateEventFromFile(string filePath)
    {
        var eventId = Path.GetFileName(filePath);
        var eventJobj = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(filePath));
        var properties = eventJobj.Properties()
            .Where(x => !x.Name.StartsWith("@"))
            .ToDictionary(x => x.Name, x => eventJobj[x.Name].Value<object>());

        var @event = new Event<LogEventData>(
            id: eventId,
            eventType: Convert.ToUInt32(eventJobj.Value<string>("@i"), 16),
            timestampUtc: eventJobj.Value<DateTime>("@t"),
            data: new LogEventData
            {
                Id = eventJobj.Value<string>(eventId),
                Level = (LogEventLevel) Enum.Parse(typeof(LogEventLevel), eventJobj.Value<string>("@l")),
                LocalTimestamp = eventJobj.Value<DateTime>("@t"),
                MessageTemplate = eventJobj.Value<string>("@mt"),
                RenderedMessage = eventJobj.Value<string>("@m"),
                Exception = eventJobj.Value<string>("@x"),
                Properties = properties,
            });
        return @event;
    }
}