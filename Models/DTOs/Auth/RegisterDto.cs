using System.ComponentModel.DataAnnotations;

namespace olhuz.API.Models.DTOs.Auth
{
    // DTO responsável pelo cadastro de novos usuários
    public class RegisterDto
    {
        [Required(ErrorMessage = "O campo Nome é obrigatório.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O campo Nome não pode exceder 100 caracteres.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo CPF é obrigatório.")]
        [StringLength(11, ErrorMessage = "O CPF deve possuir no máximo 11 caracteres.")]
        public string CPF { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo Data de Nascimento é obrigatório.")]
        public DateTime BirthDate { get; set; }

        [Required(ErrorMessage = "O campo Telefone é obrigatório.")]
        [MaxLength(20, ErrorMessage = "O campo Telefone deve ter no máximo 20 caracteres.")]
        [Phone(ErrorMessage = " O formato do telefone é inválido.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo Email é obrigatório.")]
        [StringLength(150, ErrorMessage = "O campo Email deve ter no máximo 150 caracteres.")]
        [EmailAddress(ErrorMessage = "O campo Email deve conter um endereço de email válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo Senha é obrigatório.")]
        [MinLength(8, ErrorMessage = "A senha deve possuir pelo menos 8 caracteres.")]
        [MaxLength(255, ErrorMessage = "A senha deve possuir no máximo 255 caracteres.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo Confirmar Senha é obrigatório.")]
        [Compare("Password", ErrorMessage = "As senhas não coincidem.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
