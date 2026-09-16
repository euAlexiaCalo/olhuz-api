using olhuz.API.Models.DTOs.User;
using olhuz.API.Models.Responses;

namespace olhuz.API.Services.Interfaces
{
    public interface IUserService
    {
        Task<ApiResponse<UserResponseDto>> GetProfileAsync(Guid userId);
        Task<ApiResponse<UserResponseDto>> UpdateProfileAsync(Guid userId, UpdateUserProfileDto dto);

        Task<ApiResponse<object>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
        Task<ApiResponse<object>> DeactivateAccountAsync(Guid userId);
    }
}
