using olhuz.API.Models.DTOs.Readings;
using olhuz.API.Models.Responses;
using olhuz.API.Models.Responses.Readings;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace olhuz.API.Services.Interfaces
{
    public interface IReadingService
    {
        Task<ApiResponse<List<ReadingHistoryResponse>>> GetReadingsByUserIdAsync(Guid userId);
        Task<ApiResponse<ReadingHistoryResponse>> CreateReadingAsync(Guid userId, CreateReadingDto dto);
    }
}