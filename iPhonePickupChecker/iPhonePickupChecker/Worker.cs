using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace iPhonePickupChecker
{
    class Worker
    {
        /// <summary>
        /// get pickup availabilty from apple.com
        /// </summary>
        /// <param name="zipCode"></param>
        /// <param name="carriers"></param>
        /// <param name="products"></param>
        /// <returns></returns>
        private async Task<Availability[]> fetchAsync(int zipCode, Carrier[] carriers, Product[] products)
        {
            List<Availability> list = new List<Availability>();

            string[] part_numbers = Array.ConvertAll(products, p => p.partNumber);

            foreach (var carrier in carriers)
            {
                string url = getUrl(zipCode, carrier.id, part_numbers);

                using (HttpClient client = new HttpClient())
                {
                    string text = await client.GetStringAsync(url);
                    var json = JsonConvert.DeserializeObject<JToken>(text);
                    list.AddRange(Availability.fromJson(json, carrier, products));
                }
            }

            return list.ToArray();
        }

        /// <summary>
        /// get url for the request
        /// </summary>
        /// <param name="zipCode"></param>
        /// <param name="carrier"></param>
        /// <param name="partNumbers"></param>
        /// <returns></returns>
        private static string getUrl(int zipCode, string carrier, string[] partNumbers)
        {
            string url = "https://www.apple.com/shop/retail/pickup-message?pl=true";
            url += "&cppart=" + carrier;

            int index = 0;
            foreach (string pn in partNumbers)
                url += "&parts." + (index++) + "=" + pn;

            url += "&location=" + zipCode;

            return url;
        }

        /// <summary>
        /// main loop
        /// </summary>
        public void run()
        {
            Configuration config = new Configuration();
            config.Load("config.json");

            Availability[] last_availability = null;

            while (true)
            {
                var new_availability = fetchAsync(config.zipCode, config.carriers, config.products).Result;

                if (last_availability == null || !Availability.isSame(last_availability, new_availability))
                {
                    int available_count = 0;

                    foreach (var ava in new_availability)
                    {
                        if (!ava.isAvailable)
                            continue;
                        showStatus("Availabile: " + ava.product.name + " " + ava.carrier.name + " " + ava.store.name);
                        available_count++;
                    }

                    if (available_count == 0)
                        showStatus("Nothing available");

                    showStatus("");

                    if (last_availability != null)
                    {
                        // notify
                        notify(config.notification, new_availability, config.reserveUrl);
                    }

                    last_availability = new_availability;
                }

                Thread.Sleep(60 * 1000);
            }
        }

        /// <summary>
        /// send email
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="availability"></param>
        /// <param name="reserveUrl"></param>
        private static void notify(Notification notification, Availability[] availability, string reserveUrl)
        {
            // email body

            string body = "";

            body += "<div>";
            body += "<small>Time:</small>";
            body += "<br/>";
            body += (DateTime.UtcNow + TimeSpan.FromHours(8)).ToString("yyyy-MM-dd HH:mm:ss") + " HKT";
            body += "</div>";

            body += "<br/>";

            int available_count = 0;
            foreach (var ava in availability)
            {
                if (ava.isAvailable)
                    available_count++;
            }

            body += "<div>";
            body += "<small>Available for pickup:</small>";
            body += "<br/>";
            if (available_count == 0)
                body += "Nothing";
            else
            {
                Dictionary<string, Carrier> carriers = new Dictionary<string, Carrier>();
                Dictionary<string, Product> products = new Dictionary<string, Product>();

                foreach (var ava in availability)
                {
                    if (!products.ContainsKey(ava.product.partNumber))
                        products.Add(ava.product.partNumber, ava.product);

                    if (!carriers.ContainsKey(ava.carrier.id))
                        carriers.Add(ava.carrier.id, ava.carrier);
                }

                foreach (var carrier in carriers.Values)
                {
                    foreach (var product in products.Values)
                    {
                        body += product.name + " (" + carrier.name + ")";
                        body += "<br/>";
                        foreach (var ava in availability)
                        {
                            if (ava.isAvailable && ava.carrier.id == carrier.id && ava.product.partNumber == product.partNumber)
                            {
                                body += "- " + ava.store.name;
                                body += "<br/>";
                            }
                        }

                        body += "<br/>";
                    }
                }
            }
            body += "</div>";

            body += "<br/>";

            body += "<div>";
            body += "<a href=\"" + reserveUrl + "\">Click here to reserve</a>";
            body += "</div>";

            body += "<hr/>";

            // send

            Gmail gmail = new Gmail()
            {
                GmailId = notification.sender,
                GmailPassword = notification.senderPassword,
                To = notification.recipients,
                IsHtml = true,
                Subject = "iPhone X pickup availability changed",
                Body = body
            };

            gmail.Send();
        }

        /// <summary>
        /// show status on console and write to log file
        /// </summary>
        /// <param name="msg"></param>
        private static void showStatus(string msg)
        {
            string line = string.IsNullOrEmpty(msg) ? "" : DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + msg;

            Console.WriteLine(line);

            for (int retry = 0; retry < 5; retry++)
            {
                try
                {
                    using (var sw = new StreamWriter("availability.log", true))
                        sw.WriteLine(line);
                    break;
                }
                catch { }
            }
        }
    }
}
