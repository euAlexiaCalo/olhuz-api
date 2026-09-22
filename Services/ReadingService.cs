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

        public async Task<ApiResponse<ReadingHistoryResponse>> CreateReadingAsync( Guid userId, CreateReadingDto dto)
        {
            string? fullPath = null;

            try
            {
                string? filePath = null;
                string? fileName = null;
                string? fileSizeStr = null;
                string generatedDescription = dto.DescriptionText;
                string finalTitle = dto.Title;

                // ============================================================
                // PROCESSAMENTO DO ARQUIVO
                // ============================================================

                if (dto.File != null && dto.File.Length > 0)
                {
                    fileName = dto.File.FileName;

                    // Calcula o tamanho do arquivo
                    long sizeInBytes = dto.File.Length;

                    fileSizeStr = sizeInBytes > 1024 * 1024
                        ? $"{Math.Round((double)sizeInBytes / (1024 * 1024), 1)} MB"
                        : $"{Math.Round((double)sizeInBytes / 1024, 1)} KB";

                    // ========================================================
                    // DEFINE A PASTA DE UPLOAD
                    // ========================================================

                    string uploadsFolder = Path.Combine(
                        _env.WebRootPath ?? Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot"
                        ),
                        "uploads"
                    );

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // ========================================================
                    // CRIA UM NOME ÚNICO PARA O ARQUIVO
                    // ========================================================

                    string uniqueFileName =
                        $"{Guid.NewGuid()}_{fileName}";

                    fullPath = Path.Combine(
                        uploadsFolder,
                        uniqueFileName
                    );

                    // ========================================================
                    // SALVA O ARQUIVO
                    // ========================================================

                    using (var stream = new FileStream(
                        fullPath,
                        FileMode.Create))
                    {
                        await dto.File.CopyToAsync(stream);
                    }

                    // Caminho relativo que será salvo no banco
                    filePath = $"/uploads/{uniqueFileName}";

                    // ========================================================
                    // ENVIA PARA A IA
                    // ========================================================

                    string aiResult =
                        await DescribeImageWithGeminiAsync(dto.File);

                    // ========================================================
                    // EXTRAI TÍTULO E DESCRIÇÃO
                    // ========================================================

                    if (!string.IsNullOrWhiteSpace(aiResult))
                    {
                        var upperResult = aiResult.ToUpperInvariant();

                        if (
                            upperResult.Contains("TITULO:") &&
                            upperResult.Contains("DESCRICAO:")
                        )
                        {
                            int titleIndex =
                                upperResult.IndexOf("TITULO:");

                            int descIndex =
                                upperResult.IndexOf("DESCRICAO:");

                            if (descIndex > titleIndex)
                            {
                                finalTitle = aiResult
                                    .Substring(
                                        titleIndex + "TITULO:".Length,
                                        descIndex -
                                        (titleIndex + "TITULO:".Length)
                                    )
                                    .Trim();

                                generatedDescription = aiResult
                                    .Substring(
                                        descIndex + "DESCRICAO:".Length
                                    )
                                    .Trim();
                            }
                        }
                        else
                        {
                            // Caso a IA não siga exatamente o formato
                            var lines = aiResult.Split(
                                new[] { '\r', '\n' },
                                StringSplitOptions.RemoveEmptyEntries
                            );

                            if (lines.Length > 0)
                            {
                                finalTitle =
                                    lines[0].Length > 30
                                        ? lines[0].Substring(0, 30) + "..."
                                        : lines[0];

                                generatedDescription = aiResult;
                            }
                        }
                    }

                    // ========================================================
                    // VERIFICA SE A IA REALMENTE GEROU UMA DESCRIÇÃO
                    // ========================================================

                    if (string.IsNullOrWhiteSpace(generatedDescription))
                    {
                        throw new Exception(
                            "A IA não retornou uma descrição válida."
                        );
                    }
                }

                // ============================================================
                // SALVA A LEITURA NO BANCO
                // ============================================================

                var reading = new ReadingHistory
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Type = dto.Type,
                    Title = finalTitle,
                    FileName = fileName,
                    FileSize = fileSizeStr,
                    FilePath = filePath,
                    UploadDate = DateTime.UtcNow,
                    DescriptionText = generatedDescription
                };

                _context.ReadingHistories.Add(reading);

                await _context.SaveChangesAsync();

                // ============================================================
                // MONTA A RESPOSTA
                // ============================================================

                var responseDto = new ReadingHistoryResponse
                {
                    Id = reading.Id,
                    Type = reading.Type,
                    Title = reading.Title,
                    FileName = reading.FileName,
                    FileSize = reading.FileSize,
                    FileUri = reading.FilePath,
                    UploadDate = reading.UploadDate
                        .ToString("dd 'de' MMMM 'de' yyyy"),
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
                // ============================================================
                // SE A IA OU O PROCESSAMENTO FALHAR,
                //     NÃO SALVA A LEITURA NO BANCO
                // ============================================================

                if (!string.IsNullOrEmpty(fullPath) &&
                    File.Exists(fullPath))
                {
                    try
                    {
                        File.Delete(fullPath);
                    }
                    catch
                    {
                        // Não interrompe o tratamento do erro principal
                    }
                }

                return new ApiResponse<ReadingHistoryResponse>
                {
                    Error = true,
                    Message = $"Erro ao processar a leitura: {ex.Message}",
                    Data = null
                };
            }
        }

        // Método privado que encapsula a chamada à API do Gemini
        private async Task<string> DescribeImageWithGeminiAsync(
    IFormFile arquivo)
        {
            // ============================================================
            // PEGA A CHAVE DA API
            // ============================================================

            var apiKey =
                _configuration["OlhuzGeminiApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new Exception(
                    "A chave da API do Gemini não foi configurada."
                );
            }

            // ============================================================
            // MODELO GEMINI
            // ============================================================

            const string model = "gemini-3.6-flash";

            string url =
                $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";

            // ============================================================
            // CONVERTE O ARQUIVO PARA BASE64
            // ============================================================

            using var ms = new MemoryStream();

            await arquivo.CopyToAsync(ms);

            var fileBytes = ms.ToArray();

            var base64Data =
                Convert.ToBase64String(fileBytes);

            // ============================================================
            // IDENTIFICA O TIPO DO ARQUIVO
            // ============================================================

            var contentType =
                arquivo.ContentType;

            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = "application/octet-stream";
            }

            // ============================================================
            // PARTE DO ARQUIVO ENVIADA PARA O GEMINI
            // ============================================================

            object filePart = new
            {
                inline_data = new
                {
                    mime_type = contentType,
                    data = base64Data
                }
            };

            // ============================================================
            // PROMPT DA OLHUZ
            // ============================================================

            var promptTreinamento = @"
Você é o assistente de inteligência artificial especialista
em acessibilidade visual da Olhuz.

Sua função é interpretar imagens e documentos para pessoas
cegas ou com baixa visão.

Analise cuidadosamente todo o conteúdo visual disponível.

Descreva informações importantes como:

- pessoas;
- objetos;
- ambientes;
- posições e relações espaciais;
- cores;
- textos visíveis;
- sinais e símbolos;
- gráficos;
- tabelas;
- documentos;
- elementos relevantes para compreensão do contexto.

A descrição deve ser objetiva, clara, detalhada e útil
para uma pessoa que não consegue enxergar o conteúdo.

Não invente informações que não estejam presentes.

Retorne a resposta EXATAMENTE neste formato:

TITULO: [título curto de no máximo 4 palavras]

DESCRICAO: [descrição detalhada em texto corrido,
sem markdown, sem negrito e sem listas]
";

            // ============================================================
            // MONTA O PAYLOAD
            // ============================================================

            var payload = new
            {
                contents = new[]
                {
            new
            {
                parts = new object[]
                {
                    new
                    {
                        text = promptTreinamento
                    },

                    filePart
                }
            }
        }
            };

            // ============================================================
            // CONVERTE O PAYLOAD PARA JSON
            // ============================================================

            var jsonPayload =
                JsonConvert.SerializeObject(payload);

            // ============================================================
            // TENTA ATÉ 3 VEZES
            // ============================================================

            const int maxAttempts = 3;

            for (int attempt = 1;
                 attempt <= maxAttempts;
                 attempt++)
            {
                using var request =
                    new HttpRequestMessage(
                        HttpMethod.Post,
                        url
                    );

                // ========================================================
                // ENVIA A API KEY PELO HEADER
                // ========================================================

                request.Headers.Add(
                    "x-goog-api-key",
                    apiKey
                );

                // ========================================================
                // ENVIA O JSON
                // ========================================================

                request.Content =
                    new StringContent(
                        jsonPayload,
                        Encoding.UTF8,
                        "application/json"
                    );

                // ========================================================
                // FAZ A REQUISIÇÃO
                // ========================================================

                var response =
                    await _httpClient.SendAsync(request);

                var responseBody =
                    await response.Content.ReadAsStringAsync();

                // ========================================================
                // SUCESSO
                // ========================================================

                if (response.IsSuccessStatusCode)
                {
                    dynamic? result =
                        JsonConvert.DeserializeObject(
                            responseBody
                        );

                    string? generatedText =
                        result?.candidates?[0]
                            ?.content?.parts?[0]?.text;

                    if (string.IsNullOrWhiteSpace(
                        generatedText))
                    {
                        throw new Exception(
                            "O Gemini respondeu, mas não retornou texto."
                        );
                    }

                    return generatedText;
                }

                // ========================================================
                // ERROS TEMPORÁRIOS
                // ========================================================

                if (
                    response.StatusCode ==
                        System.Net.HttpStatusCode.ServiceUnavailable
                    ||
                    response.StatusCode ==
                        System.Net.HttpStatusCode.TooManyRequests
                    ||
                    response.StatusCode ==
                        System.Net.HttpStatusCode.BadGateway
                    ||
                    response.StatusCode ==
                        System.Net.HttpStatusCode.GatewayTimeout
                )
                {
                    Console.WriteLine(
                        $"Gemini retornou {(int)response.StatusCode}. " +
                        $"Tentativa {attempt}/{maxAttempts}."
                    );

                    // Se ainda houver tentativa,
                    // espera antes de tentar novamente.
                    if (attempt < maxAttempts)
                    {
                        int delaySeconds =
                            attempt * 2;

                        await Task.Delay(
                            TimeSpan.FromSeconds(
                                delaySeconds
                            )
                        );

                        continue;
                    }
                }

                // ========================================================
                // ERRO DEFINITIVO
                // ========================================================

                throw new Exception(
                    $"Erro ao processar o arquivo com a IA do Gemini. " +
                    $"HTTP {(int)response.StatusCode}: " +
                    responseBody
                );
            }

            throw new Exception(
                "Não foi possível obter uma resposta do Gemini."
            );
        }
    }
}