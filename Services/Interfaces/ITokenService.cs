using olhuz.API.Models;
using olhuz.API.Models.DTOs.Auth;

namespace olhuz.API.Services.Interfaces
{
    public interface ITokenService
    {
        TokenResultDto GenerateToken(User user);
    }
}
