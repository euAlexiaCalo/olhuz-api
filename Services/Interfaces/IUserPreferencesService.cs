using olhuz.API.Models.DTOs.Preferences;
using olhuz.API.Models.Responses;
using olhuz.API.Models.Responses.Preferences;

namespace olhuz.API.Services.Interfaces
{
    public interface IUserPreferencesService
    {
        Task<ApiResponse<UserPreferencesResponse>> GetByUserIdAsync(Guid userId);
        Task<ApiResponse<UserPreferencesResponse>> UpdatePreferencesAsync(Guid userId, UpdateUserPreferencesDto dto);
        Task<ApiResponse<UserPreferencesResponse>> ResetPreferencesAsync(Guid userId);
    }
}
