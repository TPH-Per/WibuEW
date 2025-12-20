namespace DoAnLTWHQT.Models
{
    public class ApiAddressRequest
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string RecipientName { get; set; }
        public string RecipientPhone { get; set; }
        public string StreetAddress { get; set; }
        public string Ward { get; set; }
        public string District { get; set; }
        public string City { get; set; }
        public bool IsDefault { get; set; }
    }

    public class AddressDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string RecipientName { get; set; }
        public string RecipientPhone { get; set; }
        public string StreetAddress { get; set; }
        public string Ward { get; set; }
        public string District { get; set; }
        public string City { get; set; }
        public bool IsDefault { get; set; }
    }
}
