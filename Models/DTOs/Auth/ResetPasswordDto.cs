using System.ComponentModel.DataAnnotations;

namespace olhuz.API.Models.DTOs.Auth
{
    // DTO utilizado para redefinição de senha
    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "O campo Token é obrigatório.")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo Email é obrigatório.")]
        [EmailAddress(ErrorMessage = "O email informado é inválido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo Nova Senha é obrigatório.")]
        [MinLength(6, ErrorMessage = "A senha deve possuir pelo menos 6 caracteres.")]
        [MaxLength(255, ErrorMessage = "A senha deve possuir no máximo 255 caracteres.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo Confirmar Senha é obrigatório.")]
        [Compare(nameof(NewPassword),
            ErrorMessage = "As senhas informadas não coincidem.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
