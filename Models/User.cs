using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace olhuz.API.Models
{
    [Table("TB_USER")]
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(11)]
        public string CPF { get; set; } = string.Empty;

        [Required]
        public DateTime BirthDate { get; set; }

        [Required]
        [MaxLength(20)]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public bool IsActive { get; set; }
        // Token utilizado no processo de recuperação de senha
        public string? RecoveryToken { get; set; }
        // Data limite para utilização do token de recuperação
        public DateTime? TokenExpirationDate { get; set; }
        // Indica se o token já foi utilizado
        public bool TokenUsed { get; set; }

        // Relacionamento 1:1
        // Um usuário possui apenas um conjunto de preferências
        public UserPreferences? Preferences { get; set; }

        public User()
        {
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
            TokenUsed = false;
        }
    }
}
