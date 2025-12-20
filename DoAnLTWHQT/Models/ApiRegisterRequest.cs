namespace DoAnLTWHQT.Models
{
    public class ApiRegisterRequest
    {

        public string Name { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public string PasswordConfirmation { get; set; }

    }
}