using olhuz.API.Models.DTOs.User;
using olhuz.API.Models.Responses;

namespace olhuz.API.Services.Interfaces
{
    public interface IUserService
    {
        Task<ApiResponse<UserResponseDto>> GetProfileAsync(int userId);
        Task<ApiResponse<UserResponseDto>> UpdateProfileAsync();
        Task<ApiResponse<object>> DeactivateAccountAsync(int userId);
    }
}
