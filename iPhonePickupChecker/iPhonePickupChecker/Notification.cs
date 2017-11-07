using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace iPhonePickupChecker
{
    /// <summary>
    /// information for sending notification
    /// </summary>
    class Notification
    {
        public Notification(JToken json)
        {
            sender = json["sender"].Value<string>("email");
            senderPassword = json["sender"].Value<string>("password");
            recipients = Array.ConvertAll(json.Value<JArray>("recipients").ToArray(), j => j.Value<string>());
        }

        public string sender { get; private set; }
        public string senderPassword { get; private set; }
        public string[] recipients { get; private set; }
    }
}
