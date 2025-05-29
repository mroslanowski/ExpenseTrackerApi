using System.Text.Json.Serialization;

namespace SecureAuthApi.Models
{
    public class AuthResponse
    {
        [JsonPropertyName("token")]
        public required string Token { get; set; }

        [JsonPropertyName("message")]
        public required string Message { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }
} 