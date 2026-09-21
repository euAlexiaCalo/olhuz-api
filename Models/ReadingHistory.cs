using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace olhuz.API.Models
{
    [Table("TB_READING_HISTORY")]
    public class ReadingHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Type { get; set; } = string.Empty; // "Imagem", "Documento", etc.

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? FileName { get; set; }

        public string? FileSize { get; set; }

        public string? FilePath { get; set; } // Caminho onde o arquivo foi salvo no servidor

        [Required]
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        [Required]
        public string DescriptionText { get; set; } = string.Empty;

        // Relacionamento com o Usuário
        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }
        public User? User { get; set; }
    }
}