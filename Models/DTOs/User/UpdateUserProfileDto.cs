using System.ComponentModel.DataAnnotations;

namespace olhuz.API.Models.DTOs.User
{
    public class UpdateUserProfileDto
    {
        [Required(ErrorMessage = "O nome completo é obrigatório.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome completo não pode exceder 100 caracteres.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "O número de telefone é obrigatório.")]
        [MaxLength(20, ErrorMessage = "O número de telefone não pode exceder 20 caracteres.")]
        [Phone(ErrorMessage = " O formato do telefone é inválido.")]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
