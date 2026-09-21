using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace olhuz.API.Models.DTOs.Readings
{
    public class CreateReadingDto
    {
        [Required(ErrorMessage = "O tipo do arquivo é obrigatório.")]
        public string Type { get; set; } = string.Empty;

        [Required(ErrorMessage = "O título é obrigatório.")]
        public string Title { get; set; } = string.Empty;

        public IFormFile? File { get; set; } // Arquivo enviado da câmera ou galeria

        public string DescriptionText { get; set; } = "Nenhuma descrição disponível.";
    }
}