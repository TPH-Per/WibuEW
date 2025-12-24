using Newtonsoft.Json;

namespace DoAnLTWHQT.Models
{
    public class ApiChangePasswordRequest
    {
        public long UserId { get; set; }

        [JsonProperty("current_password")]
        public string CurrentPassword { get; set; }

        [JsonProperty("new_password")]
        public string NewPassword { get; set; }

        [JsonProperty("confirm_password")]
        public string ConfirmPassword { get; set; }
    }
}
