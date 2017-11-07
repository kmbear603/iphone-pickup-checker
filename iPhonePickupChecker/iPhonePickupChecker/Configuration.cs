using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace iPhonePickupChecker
{
    class Configuration
    {
        public Configuration()
        {
        }

        public void Load(string fileName)
        {
            if (!File.Exists(fileName))
                throw new FileNotFoundException(fileName);

            string text = File.ReadAllText(fileName);

            var json = JsonConvert.DeserializeObject<JToken>(text);

            notification = new Notification(json["notification"]);
            zipCode = json.Value<int>("zip-code");
            reserveUrl = json.Value<string>("reserve-url");
            carriers = json.Value<JArray>("carriers").ToArray()
                            .Where(c => c.Value<bool>("selected"))
                            .Select(c=>new Carrier(c.Value<string>("id"), c.Value<string>("name")))
                            .ToArray();
            products = json.Value<JArray>("products").ToArray()
                            .Where(c => c.Value<bool>("selected"))
                            .Select(c => new Product(c.Value<string>("id"), c.Value<string>("name")))
                            .ToArray();

            if (carriers.Length == 0)
                throw new Exception("no carrier is selected");

            if (products.Length == 0)
                throw new Exception("no product is selected");
        }

        public Notification notification
        {
            get; private set;
        }

        public int zipCode
        {
            get; private set;
        }

        public string reserveUrl
        {
            get; private set;
        }

        public Carrier[] carriers
        {
            get; private set;
        }

        public Product[] products
        {
            get; private set;
        }
    }
}
