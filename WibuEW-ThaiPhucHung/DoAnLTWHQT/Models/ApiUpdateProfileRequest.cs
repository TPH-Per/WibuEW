namespace DoAnLTWHQT.Models
{
    public class ApiUpdateProfileRequest
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    }
}
