namespace iPhonePickupChecker
{
    class Carrier
    {
        public Carrier(string id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public string id { get; private set; }
        public string name { get; private set; }

        public override string ToString()
        {
            return name;
        }
    }
}
