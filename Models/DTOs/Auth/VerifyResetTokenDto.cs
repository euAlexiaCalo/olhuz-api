using System.ComponentModel.DataAnnotations;

namespace olhuz.API.Models.DTOs.Auth
{
    public class VerifyResetTokenDto
    {
        [Required(ErrorMessage = "O token é obrigatório.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "O token deve conter 6 dígitos.")]
        public string Token { get; set; }
        [Required(ErrorMessage = "O campo Email é obrigatório.")]
        [EmailAddress(ErrorMessage = "O email informado é inválido.")]
        public string Email { get; set; }
    }
}
