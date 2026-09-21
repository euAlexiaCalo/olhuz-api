using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using olhuz.API.Data;
using olhuz.API.Models;
using olhuz.API.Models.DTOs.Readings;
using olhuz.API.Models.Responses;
using olhuz.API.Models.Responses.Readings;
using olhuz.API.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace olhuz.API.Services
{
    public class ReadingService : IReadingService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ReadingService(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<ApiResponse<List<ReadingHistoryResponse>>> GetReadingsByUserIdAsync(Guid userId)
        {
            var readings = await _context.ReadingHistories
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.UploadDate)
                .ToListAsync();

            var responseList = readings.Select(r => new ReadingHistoryResponse
            {
                Id = r.Id,
                Type = r.Type,
                Title = r.Title,
                FileName = r.FileName,
                FileSize = r.FileSize,
                FileUri = r.FilePath,
                UploadDate = r.UploadDate.ToString("dd 'de' MMMM 'de' yyyy"), // Formato em pt-BR compatível com o app
                DescriptionText = r.DescriptionText
            }).ToList();

            return new ApiResponse<List<ReadingHistoryResponse>>
            {
                Error = false,
                Message = "Histórico de leituras recuperado com sucesso.",
                Data = responseList
            };
        }

        public async Task<ApiResponse<ReadingHistoryResponse>> CreateReadingAsync(Guid userId, CreateReadingDto dto)
        {
            try
            {
                string? filePath = null;
                string? fileName = null;
                string? fileSizeStr = null;

                // Processa o upload do arquivo se ele existir
                if (dto.File != null && dto.File.Length > 0)
                {
                    fileName = dto.File.FileName;

                    // Calcula o tamanho formatado em KB ou MB
                    long sizeInBytes = dto.File.Length;
                    fileSizeStr = sizeInBytes > 1024 * 1024
                        ? $"{Math.Round((double)sizeInBytes / (1024 * 1024), 1)} MB"
                        : $"{Math.Round((double)sizeInBytes / 1024, 1)} KB";

                    // Define o caminho de salvamento em wwwroot/uploads
                    string uploadsFolder = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), "wwwroot", "uploads");

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                    string fullPath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await dto.File.CopyToAsync(stream);
                    }

                    // Caminho relativo ou URL para acessar no app
                    filePath = $"/uploads/{uniqueFileName}";
                }

                var reading = new ReadingHistory
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Type = dto.Type,
                    Title = dto.Title,
                    FileName = fileName,
                    FileSize = fileSizeStr,
                    FilePath = filePath,
                    UploadDate = DateTime.UtcNow,
                    DescriptionText = string.IsNullOrEmpty(dto.DescriptionText)
                        ? "Nenhuma descrição disponível."
                        : dto.DescriptionText
                };

                _context.ReadingHistories.Add(reading);
                await _context.SaveChangesAsync();

                var responseDto = new ReadingHistoryResponse
                {
                    Id = reading.Id,
                    Type = reading.Type,
                    Title = reading.Title,
                    FileName = reading.FileName,
                    FileSize = reading.FileSize,
                    FileUri = reading.FilePath,
                    UploadDate = reading.UploadDate.ToString("dd 'de' MMMM 'de' yyyy"),
                    DescriptionText = reading.DescriptionText
                };

                return new ApiResponse<ReadingHistoryResponse>
                {
                    Error = false,
                    Message = "Leitura salva com sucesso.",
                    Data = responseDto
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<ReadingHistoryResponse>
                {
                    Error = true,
                    Message = $"Erro ao salvar a leitura: {ex.Message}",
                    Data = null
                };
            }
        }
    }
}