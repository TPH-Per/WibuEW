namespace DoAnLTWHQT.Models
{
    public class ApiValidationResponse
    {

        public bool Success { get; set; }
        public string Message { get; set; }
        public System.Collections.Generic.Dictionary<string, string[]> Errors { get; set; }

    }
}