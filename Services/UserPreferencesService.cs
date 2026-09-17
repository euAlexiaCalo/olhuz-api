using Microsoft.EntityFrameworkCore;
using olhuz.API.Data;
using olhuz.API.Models;
using olhuz.API.Models.DTOs.Preferences;
using olhuz.API.Models.Responses;
using olhuz.API.Models.Responses.Preferences;
using olhuz.API.Services.Interfaces;

namespace olhuz.API.Services
{
    public class UserPreferencesService : IUserPreferencesService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserPreferencesService> _logger;

        public UserPreferencesService(AppDbContext context, ILogger<UserPreferencesService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ================================================
        // OBTER PREFERÊNCIAS
        // ================================================

        public async Task<ApiResponse<UserPreferencesResponse>> GetByUserIdAsync(Guid userId)
        {
            // CONSULTA

            // Busca ou cria as preferências do usuário
            var (preferences, errorResponse) = await GetPreferencesByUserIdAsync(userId, "busca de preferências");
            if (errorResponse != null) return errorResponse;

            // RETORNO MAPEADO

            var response = MapToResponse(preferences!);
            return CreateSuccessResponse("Preferências recuperadas com sucesso!", response);
        }
        // ================================================
        // ATUALIZAR PREFERÊNCIAS
        // ================================================

        public async Task<ApiResponse<UserPreferencesResponse>> UpdatePreferencesAsync(Guid userId, UpdateUserPreferencesDto dto)
        {
            // CONSULTA E INSTÂNCIA DAS PREFERÊNCIAS
            
            // Obtém as preferências rastreadas no banco de dados para o usuário informado
            var (preferences, errorResponse) = await GetPreferencesByUserIdAsync(userId, "atualização de preferências");
            if (errorResponse != null) return errorResponse;

            // ATUALIZAÇÃO DOS CAMPOS DA ENTIDADE

            preferences!.ScreenReader = dto.ScreenReader;
            preferences.SpeechRate = dto.SpeechRate;
            preferences.VoiceType = dto.VoiceType;
            preferences.VolumeLevel = dto.VolumeLevel;
            preferences.Theme = dto.Theme;
            preferences.VibrationEnabled = dto.VibrationEnabled;
            preferences.AlertSoundEnabled = dto.AlertSoundEnabled;

            // PERSISTÊNCIA NO BANCO DE DADOS

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar atualização de preferências para o usuário {UserId}", userId);
                return CreateErrorResponse<UserPreferencesResponse>("Ocorreu um erro interno ao atualizar as preferências.", 500);
            }

            // RETORNO MAPEADO

            var response = MapToResponse(preferences);
            return CreateSuccessResponse("Preferências atualizadas com sucesso!", response);
        }

        // ================================================
        // RESETAR PREFERÊNCIAS PARA O PADRÃO
        // ================================================
        public async Task<ApiResponse<UserPreferencesResponse>> ResetPreferencesAsync(Guid userId)
        {
            // CONSULTA E INSTÂNCIA DAS PREFERÊNCIAS

            // Obtém as preferências do usuário no banco
            var (preferences, errorResponse) = await GetPreferencesByUserIdAsync(userId, "reset de preferências");
            if (errorResponse != null) return errorResponse;

            // RESTAURAÇÃO PARA OS VALORES PADRÃO

            // Instancia o modelo padrão para obter os valores de inicialização
            var defaultValues = new UserPreferences();

            // Restaura cada propriedade para a sua configuração padrão
            preferences!.ScreenReader = defaultValues.ScreenReader;
            preferences.SpeechRate = defaultValues.SpeechRate;
            preferences.VoiceType = defaultValues.VoiceType;
            preferences.VolumeLevel = defaultValues.VolumeLevel;
            preferences.Theme = defaultValues.Theme;
            preferences.VibrationEnabled = defaultValues.VibrationEnabled;
            preferences.AlertSoundEnabled = defaultValues.AlertSoundEnabled;

            // PERSISTÊNCIA NO BANCO DE DADOS

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao resetar preferências para o usuário {UserId}", userId);
                return CreateErrorResponse<UserPreferencesResponse>("Ocorreu um erro interno ao restaurar os padrões.", 500);
            }

            // RETORNO MAPEADO

            var response = MapToResponse(preferences);
            return CreateSuccessResponse("Preferências restauradas para o padrão com sucesso!", response);
        }

        // ================================================
        // MÉTODOS PRIVADOS AUXILIARES
        // ================================================

        // Obtém as preferências do usuário ativo diretamente no banco de dados
        private async Task<(UserPreferences? preferences, ApiResponse<UserPreferencesResponse>? errorResponse)> GetPreferencesByUserIdAsync(Guid userId, string contextAction)
        {
            try
            {
                // Busca diretamente o registro de preferências atrelado ao usuário
                var preferences = await _context.UserPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.User!.IsActive);

                if (preferences == null)
                {
                    _logger.LogWarning("Preferências não encontradas para o usuário {UserId} durante {ContextAction}", userId, contextAction);
                    return (null, CreateErrorResponse<UserPreferencesResponse>("Preferências do usuário não encontradas ou usuário inativo.", 404));
                }

                return (preferences, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar/gerar preferências do usuário {UserId} durante {ContextAction}", userId, contextAction);
                return (null, CreateErrorResponse<UserPreferencesResponse>("Ocorreu um erro interno ao processar as preferências do usuário.", 500));
            }
        }

        // Converte a entidade de banco 'UserPreferences' no DTO de resposta 'UserPreferencesResponse'
        private static UserPreferencesResponse MapToResponse(UserPreferences entity)
        {
            return new UserPreferencesResponse
            {
                ScreenReader = entity.ScreenReader,
                SpeechRate = entity.SpeechRate,
                VoiceType = entity.VoiceType,
                VolumeLevel = entity.VolumeLevel,
                Theme = entity.Theme,
                VibrationEnabled = entity.VibrationEnabled,
                AlertSoundEnabled = entity.AlertSoundEnabled
            };
        }

        // Formata e retorna um objeto de resposta padronizado para erros da API
        private static ApiResponse<T> CreateErrorResponse<T>(string message, int statusCode = 400)
        {
            return new ApiResponse<T>
            {
                Error = true,
                Message = message,
                StatusCode = statusCode
            };
        }

        // Formata e retorna um objeto de resposta padronizado para sucesso da API
        private static ApiResponse<T> CreateSuccessResponse<T>(string message, T? data = default, int statusCode = 200)
        {
            return new ApiResponse<T>
            {
                Error = false,
                Message = message,
                Data = data,
                StatusCode = statusCode
            };
        }
    }
}
