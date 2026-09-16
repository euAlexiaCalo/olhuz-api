using olhuz.API.Models.DTOs.Auth;
using olhuz.API.Models.DTOs.User;
using olhuz.API.Models.Responses;
using olhuz.API.Models.Responses.Auth;

namespace olhuz.API.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<UserResponseDto>> RegisterAsync(RegisterDto dto);
        Task<ApiResponse<LoginResponse>> LoginAsync(LoginDto dto);
        Task<ApiResponse<object>> ForgotPasswordAsync(ForgotPasswordDto dto);
        Task<ApiResponse<object>> ResetPasswordAsync(ResetPasswordDto dto);
    }
}
