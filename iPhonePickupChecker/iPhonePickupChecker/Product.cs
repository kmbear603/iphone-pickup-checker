namespace iPhonePickupChecker
{
    class Product
    {
        public Product(string partNumber, string name)
        {
            this.partNumber = partNumber;
            this.name = name;
        }

        public string partNumber { get; private set; }
        public string name { get; private set; }

        public override string ToString()
        {
            return name;
        }
    }
}
