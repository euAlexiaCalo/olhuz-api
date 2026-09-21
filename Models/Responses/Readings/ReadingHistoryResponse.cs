using System;

namespace olhuz.API.Models.Responses.Readings
{
    public class ReadingHistoryResponse
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string? FileSize { get; set; }
        public string? FileUri { get; set; } // URL pública ou caminho para acessar a imagem/arquivo
        public string UploadDate { get; set; } = string.Empty;
        public string DescriptionText { get; set; } = string.Empty;
    }
}