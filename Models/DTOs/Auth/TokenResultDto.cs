namespace olhuz.API.Models.DTOs.Auth
{
    public class TokenResultDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
