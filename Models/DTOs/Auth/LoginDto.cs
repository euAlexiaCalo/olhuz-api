using System.ComponentModel.DataAnnotations;

namespace olhuz.API.Models.DTOs.Auth
{
    // DTO responsável pelo login dos usuários
    public class LoginDto
    {
        [Required(ErrorMessage = "O campo Email é obrigatório.")]
        [EmailAddress(ErrorMessage = "O email informado é inválido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo Senha é obrigatório.")]
        public string Password { get; set; } = string.Empty;
    }
}
