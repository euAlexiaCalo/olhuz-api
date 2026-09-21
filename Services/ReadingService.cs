using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace olhuz.API.Services
{
    public class ReadingService : IReadingService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public ReadingService(AppDbContext context, IWebHostEnvironment env, IConfiguration configuration)
        {
            _context = context;
            _env = env;
            _configuration = configuration;
            _httpClient = new HttpClient();
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
                UploadDate = r.UploadDate.ToString("dd 'de' MMMM 'de' yyyy"), // Formato em pt-BR
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
                string generatedDescription = dto.DescriptionText;

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

                    // Chama a IA do Gemini para gerar a descrição automática da imagem/arquivo
                    try
                    {
                        generatedDescription = await DescribeImageWithGeminiAsync(dto.File);
                    }
                    catch (Exception ex)
                    {
                        generatedDescription = "Erro ao gerar descrição pela IA: " + ex.Message;
                    }
                }

                // Salva no banco de dados com a descrição gerada pela IA
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
                    DescriptionText = generatedDescription
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
                    Message = "Leitura e descrição geradas com sucesso.",
                    Data = responseDto
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<ReadingHistoryResponse>
                {
                    Error = true,
                    Message = $"Erro ao processar aleitura: {ex.Message}",
                    Data = null
                };
            }
        }

        // Método privado que encapsula a chamada à API do Gemini
        private async Task<string> DescribeImageWithGeminiAsync(IFormFile arquivo)
        {
            // Pega a chave da API salva
            var apiKey = _configuration["OlhuzGeminiApiKey"];

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

            using var ms = new MemoryStream();
            await arquivo.CopyToAsync(ms);
            var fileBytes = ms.ToArray();
            var base64Data = Convert.ToBase64String(fileBytes);
            var contentType = arquivo.ContentType ?? "application/octet-stream";

            object filePart;

            // Se for um PDF ou documento de texto suportado pelo Gemini
            if (contentType.Contains("pdf") || contentType.Contains("text") || contentType.Contains("document"))
            {
                filePart = new
                {
                    inline_data = new
                    {
                        mime_type = contentType,
                        data = base64Data
                    }
                };
            }
            else // Se for imagem (jpg, png, etc.)
            {
                filePart = new
                {
                    inline_data = new
                    {
                        mime_type = contentType,
                        data = base64Data
                    }
                };
            }

            // Monta o "corpo" da requisição (payload) no formato que o Gemini espera
            var payload = new
            {
                contents = new[]
                {
                    new {
                        parts = new object[]
                        {
                            new { text = @"Você é o assistente de voz da Olhuz. Analise este arquivo enviado (que pode ser uma imagem, um documento PDF ou um texto) e elabore uma descrição extremamente detalhada para uma pessoa cega, seguindo REGRAS RÍGIDAS: 
                            1. NUNCA use negrito (**), itálico (*) ou listas com símbolos. Escreva apenas texto corrido com acentos e limpo.
                            2. Se for um documento ou PDF, faça um resumo completo do conteúdo, explicando os pontos principais, seções, tabelas ou dados importantes presentes nele.
                            3. Se for uma imagem, descreva cores, iluminação, objetos e posições detalhadamente.
                            4. ADAPTAÇÃO DE VOZ: Como o texto será lido por um sintetizador de voz, escreva de forma natural para soar perfeito em português brasileiro." },
                            filePart
                        }
                    }
                }
            };

            // Converte o objeto payload em JSON (string)
            var jsonPayload = JsonConvert.SerializeObject(payload);

            // Prepara o conteúdo da requisição HTTP (JSON + UTF8)
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Envia a requisição POST pro Google
            var response = await _httpClient.PostAsync(url, content);

            // Lê a resposta como texto
            var responseBody = await response.Content.ReadAsStringAsync();

            // Se deu tudo certo...
            if (response.IsSuccessStatusCode)
            {
                // Converte o JSON da resposta pra objeto dinâmico
                dynamic result = JsonConvert.DeserializeObject(responseBody);

                // Navega dentro do JSON gigante e pega só o texto da resposta da IA
                return result.candidates[0].content.parts[0].text;
            }

            return "Erro ao processar o arquivo com a IA do Gemini: " + responseBody;
        }
    }
}