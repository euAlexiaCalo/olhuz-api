using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using olhuz.API.Controllers.Base;
using olhuz.API.Models.DTOs.Preferences;
using olhuz.API.Models.Responses;
using olhuz.API.Models.Responses.Preferences;
using olhuz.API.Services.Interfaces;

namespace olhuz.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/user/preferences")]
    [Produces("application/json")]
    public class UserPreferencesController : MainController
    {
        private readonly IUserPreferencesService _userPreferencesService;

        public UserPreferencesController(IUserPreferencesService userPreferencesService)
        {
            _userPreferencesService = userPreferencesService;
        }

        // ================================================
        // OBTER PREFERÊNCIAS DO USUÁRIO LOGADO
        // ================================================
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<UserPreferencesResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UserPreferencesResponse>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<UserPreferencesResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPreferences()
        {
            if (!TryGetAuthenticatedUserId<UserPreferencesResponse>(out var userId, out var unauthorizedResult))
                return unauthorizedResult!;

            // Busca através do serviço de preferências
            var result = await _userPreferencesService.GetByUserIdAsync(userId);

            // Retorna o resultado com o código de status HTTP mapeado no serviço
            return StatusCode(result.StatusCode, result);
        }

        // ================================================
        // ATUALIZAR PREFERÊNCIAS DO USUÁRIO LOGADO
        // ================================================
        [HttpPut]
        [ProducesResponseType(typeof(ApiResponse<UserPreferencesResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UserPreferencesResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<UserPreferencesResponse>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<UserPreferencesResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<UserPreferencesResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdatePreferences([FromBody] UpdateUserPreferencesDto dto)
        {
            if (!TryGetAuthenticatedUserId<UserPreferencesResponse>(out var userId, out var unauthorizedResult))
                return unauthorizedResult!;

            // Executa a atualização através do serviço de preferências
            var result = await _userPreferencesService.UpdatePreferencesAsync(userId, dto);

            // Retorna o resultado com o código de status HTTP mapeado no serviço
            return StatusCode(result.StatusCode, result);
        }

        // ================================================
        // RESETAR PREFERÊNCIAS PARA O PADRÃO
        // ================================================
        [HttpPost("reset")]
        [ProducesResponseType(typeof(ApiResponse<UserPreferencesResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UserPreferencesResponse>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<UserPreferencesResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<UserPreferencesResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ResetPreferences()
        {
            if (!TryGetAuthenticatedUserId<UserPreferencesResponse>(out var userId, out var unauthorizedResult))
                return unauthorizedResult!;

            // Executa a restauração das preferências padrão através do serviço
            var result = await _userPreferencesService.ResetPreferencesAsync(userId);

            // Retorna o resultado com o código de status HTTP mapeado no serviço
            return StatusCode(result.StatusCode, result);
        }
    }
}
