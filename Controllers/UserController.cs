using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using olhuz.API.Controllers.Base;
using olhuz.API.Models.DTOs.User;
using olhuz.API.Models.Responses;
using olhuz.API.Services.Interfaces;

namespace olhuz.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public class UserController : MainController
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // ================================================
        // OBTÉM O PERFIL DO USUÁRIO LOGADO
        // ================================================
        [HttpGet("profile")]
        [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProfile()
        {
            if (!TryGetAuthenticatedUserId<UserResponseDto>(out var userId, out var unauthorizedResult))
                return unauthorizedResult!;
            var result = await _userService.GetProfileAsync(userId);
            return StatusCode(result.StatusCode, result);
        }

        // ================================================
        // ATUALIZA OS DADOS CADASTRAIS PERMITIDOS
        // ================================================
        [HttpPut("profile")]
        [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileDto dto)
        {
            if (!TryGetAuthenticatedUserId<UserResponseDto>(out var userId, out var unauthorizedResult))
                return unauthorizedResult!;
            var result = await _userService.UpdateProfileAsync(userId, dto);
            return StatusCode(result.StatusCode, result);
        }

        // ================================================
        // ALTERA A SENHA DO USUÁRIO LOGADO
        // ================================================
        [HttpPost("profile/change-password")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!TryGetAuthenticatedUserId<object>(out var userId, out var unauthorizedResult))
                return unauthorizedResult!;
            var result = await _userService.ChangePasswordAsync(userId, dto);
            return StatusCode(result.StatusCode, result);
        }

        // ================================================
        // DESATIVA A CONTA DO USUÁRIO LOGADO
        // ================================================
        [HttpDelete("profile")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeactivateAccount()
        {
            if (!TryGetAuthenticatedUserId<object>(out var userId, out var unauthorizedResult))
                return unauthorizedResult!;
            var result = await _userService.DeactivateAccountAsync(userId);
            return StatusCode(result.StatusCode, result);
        }
    }
}
