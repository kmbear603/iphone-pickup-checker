using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace iPhonePickupChecker
{
    class Availability
    {
        public Availability(Product product, Store store, Carrier carrier, bool isAvailable)
        {
            this.product = product;
            this.store = store;
            this.carrier = carrier;
            this.isAvailable = isAvailable;
        }

        public Product product { get; private set; }
        public Store store { get; private set; }
        public Carrier carrier { get; private set; }
        public bool isAvailable { get; private set; }

        public override string ToString()
        {
            return "[" + product + " " + carrier + " " + store + " " + isAvailable + "]";
        }

        /// <summary>
        /// create from json object from apple.com
        /// </summary>
        /// <param name="json"></param>
        /// <param name="carrier"></param>
        /// <param name="interestedProducts"></param>
        /// <returns></returns>
        public static Availability[] fromJson(JToken json, Carrier carrier, Product[] interestedProducts)
        {
            List<Availability> ret = new List<Availability>();

            var body_json = json["body"];
            var store_jsons = body_json.Value<JArray>("stores");
            foreach (var store_json in store_jsons)
            {
                string store_number = store_json.Value<string>("storeNumber");
                string store_name = store_json.Value<string>("storeName");

                var part_jsons = store_json.Value<JObject>("partsAvailability");
                foreach (var part_json in part_jsons)
                {
                    string part_number = part_json.Key;
                    bool is_available = part_json.Value.Value<bool>("storeSelectionEnabled");

                    var product = Array.Find(interestedProducts, p => p.partNumber == part_number);
                    if (product == null)
                        product = new Product(part_number, part_number);

                    Availability ava = new Availability(
                        product,
                        new Store(store_number, store_name),
                        carrier,
                        is_available);

                    ret.Add(ava);
                }
            }

            return ret.ToArray();
        }

        /// <summary>
        /// compare the key of two Availability objects
        /// </summary>
        /// <param name="ava1"></param>
        /// <param name="ava2"></param>
        /// <returns></returns>
        public static bool isSameKey(Availability ava1, Availability ava2)
        {
            return ava1.carrier.id == ava2.carrier.id
                && ava1.product.partNumber == ava2.product.partNumber
                && ava1.store.storeNumber == ava2.store.storeNumber;
        }

        /// <summary>
        /// check if two Availability array are same or not
        /// </summary>
        /// <param name="ava1"></param>
        /// <param name="ava2"></param>
        /// <returns></returns>
        public static bool isSame(Availability[] ava1, Availability[] ava2)
        {
            foreach (var a1 in ava1)
            {
                var a2 = Array.Find(ava2, t => isSameKey(a1, t));
                if (a2 == null) // not found in ava2
                    return false;

                if (a1.isAvailable != a2.isAvailable)
                    return false;
            }

            foreach (var a2 in ava2)
            {
                var a1 = Array.Find(ava1, t => isSameKey(a2, t));
                if (a1 == null) // not found in ava1
                    return false;

                if (a1.isAvailable != a2.isAvailable)
                    return false;
            }

            return true;
        }
    }
}
