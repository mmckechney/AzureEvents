using System.Collections.Generic;

namespace AzureEvents
{
    class JsonConfig
    {
        public JsonConfig()
        {
            emails = new List<Email>();
            defaultEvents = new List<DefaultEvent>();
        }
        public string applicationId { get; set; }
        public string directoryName { get; set; }
        public string defaultResourceLocation { get; set; }
        public List<Email> emails { get; set; }
        public List<DefaultEvent> defaultEvents { get; set; }
    }

    public class Email
    {
        public string address { get; set; }
    }

    public class DefaultEvent
    {
        public string @event { get; set; }
    }
}
