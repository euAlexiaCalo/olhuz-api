using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using olhuz.API.Models.DTOs.User;
using olhuz.API.Models.Responses;
using olhuz.API.Services.Interfaces;

namespace olhuz.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // Obter o ID do usuário autenticado a partir do token JWT
        private Guid GetUserIdFromToken()
        {
            // Busca a informação dentro das Claims do Token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            // Converte o texto para Guid com segurança
            return Guid.TryParse(userIdClaim, out Guid userId) ? userId : Guid.Empty;
        }

        // Obtém o perfil do usuário logado
        [HttpGet("profile")]
        [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserIdFromToken();
            var result = await _userService.GetProfileAsync(userId);
            return StatusCode(result.StatusCode, result);
        }

        // Atualiza os dados cadastrais (nome e telefone) do usuário logado
        [HttpPut("profile")]
        [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileDto dto)
        {
            var userId = GetUserIdFromToken();
            var result = await _userService.UpdateProfileAsync(userId, dto);
            return StatusCode(result.StatusCode, result);
        }

        // Altera a senha do usuário logado
        [HttpPost("profile/change-password")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = GetUserIdFromToken();
            var result = await _userService.ChangePasswordAsync(userId, dto);
            return StatusCode(result.StatusCode, result);
        }

        // Desativa a conta do usuário logado
        [HttpDelete("profile")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeactivateAccount()
        {
            var userId = GetUserIdFromToken();
            var result = await _userService.DeactivateAccountAsync(userId);
            return StatusCode(result.StatusCode, result);
        }
    }
}
