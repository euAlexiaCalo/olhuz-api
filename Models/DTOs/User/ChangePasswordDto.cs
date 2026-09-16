using System.ComponentModel.DataAnnotations;

namespace olhuz.API.Models.DTOs.User
{
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "A senha atual é obrigatória.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "A nova senha é obrigatória.")]
        [MinLength(8, ErrorMessage = "A senha deve possuir pelo menos 8 caracteres.")]
        [MaxLength(255, ErrorMessage = "A senha deve possuir no máximo 255 caracteres.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "A confirmação da nova senha é obrigatória.")]
        [Compare(nameof(NewPassword), ErrorMessage = "A nova senha e a confirmação não conferem.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;

    }
}
