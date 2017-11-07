namespace iPhonePickupChecker
{
    class Store
    {
        public Store(string storeNumber, string name)
        {
            this.storeNumber = storeNumber;
            this.name = name;
        }

        public string storeNumber { get; private set; }
        public string name { get; private set; }

        public override string ToString()
        {
            return name;
        }
    }
}
