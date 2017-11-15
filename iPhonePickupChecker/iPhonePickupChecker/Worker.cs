using System;
using System.Linq;
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
                        notify(new_availability, last_availability, config);
                    }

                    last_availability = new_availability;
                }

                Thread.Sleep(config.refreshInterval);
            }
        }

        /// <summary>
        /// send email
        /// </summary>
        private static void notify(Availability[] availability, Availability[] previousAvailability, Configuration config)
        {
            // count number of available products
            int available_count = 0;
            {
                Dictionary<string/*part number*/, HashSet<string/*carrier id*/>> table = new Dictionary<string, HashSet<string>>();
                foreach (var ava in availability)
                {
                    if (!ava.isAvailable)
                        continue;

                    HashSet<string> carriers;
                    if (table.ContainsKey(ava.product.partNumber))
                        carriers = table[ava.product.partNumber];
                    else
                    {
                        carriers = new HashSet<string>();
                        table.Add(ava.product.partNumber, carriers);
                    }

                    if (carriers.Contains(ava.carrier.id))
                        continue;

                    carriers.Add(ava.carrier.id);
                    available_count++;
                }
            }

            // compose email body

            string body = "";
            body += "<!DOCTYPE html>";
            body += "<html>";
            body += "<head>";
            body += "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">";
            body += "</head>";
            body += "<body style=\"font-family: 'Arial';\">";

            body += "<div>";
            body += "<p>";
            body += "<small>Time:</small>";
            body += "<br/>";
            body += (DateTime.UtcNow + TimeSpan.FromHours(8)).ToString("yyyy-MM-dd HH:mm:ss") + " HKT";
            body += "</p>";
            body += "</div>";

            body += "<div>";
            body += "<p>";
            body += "<small>Zip code:</small>";
            body += "<br/>";
            body += config.zipCode;
            body += "</p>";
            body += "</div>";

            body += "<div>";
            body += "<p>";
            body += "<small>Available for pickup:</small>";
            body += "<br/>";
            body += (available_count == 0 ? "Nothing" : (available_count + " models"));
            body += "</p>";
            body += "</div>";

            body += "<div>";
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

                foreach (var ava in previousAvailability)
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
                        body += "<div>";
                        body += "<p>";
                        body += product.name + " (" + carrier.name + ")";
                        body += "<br/>";

                        var ava_new_all = availability.Where(a => a.carrier.id == carrier.id && a.product.partNumber == product.partNumber && a.isAvailable).ToArray();
                        var ava_prev_all = previousAvailability.Where(a => a.carrier.id == carrier.id && a.product.partNumber == product.partNumber && a.isAvailable).ToArray();

                        bool newly_available = ava_new_all.Length > 0;
                        bool previously_available = ava_prev_all.Length > 0;

                        if (!newly_available && !previously_available)
                            body += "Unavailable<br/>";
                        else
                        {
                            foreach (var ava_new in ava_new_all)
                            {
                                var ava_prev = ava_prev_all.FirstOrDefault(a => Availability.isSameKey(ava_new, a));

                                body += "- " + ava_new.store.name;
                                if (ava_prev == null)   // newly found
                                    body += " <small><font color=\"blue\">[new]</font></small>";
                                body += "<br/>";
                            }

                            foreach (var ava_prev in ava_prev_all)
                            {
                                var ava_new = ava_new_all.FirstOrDefault(a => Availability.isSameKey(ava_prev, a));
                                if (ava_new != null)
                                    continue;   // no change, already outputted in the previous loop, skip

                                body += "- <strike>" + ava_prev.store.name + "</strike>";
                                body += "<br/>";
                            }
                        }

                        body += "</p>";
                        body += "</div>";
                    }
                }
            }
            body += "</div>";

            body += "<div>";
            body += "<p><a href=\"" + config.reserveUrl + "\">Click here to reserve</a></p>";
            body += "</div>";

            body += "</body>";
            body += "</html>";

            // send

            Gmail gmail = new Gmail()
            {
                GmailId = config.notification.sender,
                GmailPassword = config.notification.senderPassword,
                To = config.notification.recipients,
                IsHtml = true,
                Subject = "iPhone X pickup availability " + (DateTime.UtcNow + TimeSpan.FromHours(8)).ToString("yyyy-MM-dd HH:mm") + " HKT",
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
