using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static BCrypt.Net.BCrypt;
using olhuz.API.Data;
using olhuz.API.Models;
using olhuz.API.Models.DTOs.User;
using olhuz.API.Models.Responses;
using olhuz.API.Services.Interfaces;

namespace olhuz.API.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(AppDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ================================================
        // OBTER PERFIL DO USUÁRIO
        // ================================================
        public async Task<ApiResponse<UserResponseDto>> GetProfileAsync(Guid userId)
        {
            // ================================================
            // CONSULTA AO BANCO DE DADOS
            // ================================================

            var (user, errorResponse) = await GetActiveUserByIdAsync<UserResponseDto>(userId, "busca de perfil");
            if (errorResponse != null) return errorResponse;

            // ================================================
            // RETORNO MAPEADO
            // ================================================

            var userResponse = MapToUserResponseDto(user!);
            return CreateSuccessResponse("Dados do usuário recuperados com sucesso!", userResponse);
        }

        // ================================================
        // ATUALIZAÇÃO DE PERFIL
        // ================================================
        public async Task<ApiResponse<UserResponseDto>> UpdateProfileAsync(Guid userId, UpdateUserProfileDto dto)
        {
            // ================================================
            // VALIDAÇÕES DE ENTRADA E FORMATO
            // ================================================

            if (!IsValidFullName(dto.FullName, out string cleanFullName))
                return CreateErrorResponse<UserResponseDto>("Informe seu nome e sobrenome completos (cada palavra deve ter no mínimo 3 letras).");

            if (!IsValidPhone(dto.PhoneNumber, out string cleanPhone))
                return CreateErrorResponse<UserResponseDto>("Informe um número de telefone válido com DDD (10 ou 11 dígitos).");

            // ================================================
            // CONSULTA AO BANCO DE DADOS
            // ================================================

            var (user, errorResponse) = await GetActiveUserByIdAsync<UserResponseDto>(userId, "atualização de perfil");
            if (errorResponse != null) return errorResponse;

            // ================================================
            // VERIFICAÇÃO DE DADOS DUPLICADOS (TELEFONE)
            // ================================================

            try
            {
                bool phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == cleanPhone && u.Id != userId);
                if (phoneExists)
                    return CreateErrorResponse<UserResponseDto>("Este número de telefone já está em uso por outro usuário.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar duplicidade de telefone para o usuário {UserId}", userId);
                return CreateErrorResponse<UserResponseDto>("Ocorreu um erro interno ao validar os dados do usuário.", 500);
            }

            // ================================================
            // ATUALIZAÇÃO DA ENTIDADE E PERSISTÊNCIA
            // ================================================

            user.FullName = cleanFullName;
            user.PhoneNumber = cleanPhone;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Violação de unicidade ao atualizar dados do usuário {UserId}", userId);
                return CreateErrorResponse<UserResponseDto>("Não foi possível atualizar os dados. O telefone informado já pode estar em uso.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar atualização de perfil para o usuário {UserId}", userId);
                return CreateErrorResponse<UserResponseDto>("Ocorreu um erro interno ao atualizar o perfil.", 500);
            }

            // ================================================
            // RETORNO MAPEADO
            // ================================================

            var userResponse = MapToUserResponseDto(user);
            return CreateSuccessResponse("Perfil atualizado com sucesso!", userResponse);
        }

        // ================================================
        // ALTERAÇÃO DE SENHA (ÁREA LOGADA)
        // ================================================

        public async Task<ApiResponse<object>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
            // ================================================
            // VALIDAÇÕES DE ENTRADA
            // ================================================

            if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
                return CreateErrorResponse<object>("A senha atual deve ser informada.");

            if (!IsValidPassword(dto.NewPassword))
                return CreateErrorResponse<object>("A nova senha deve conter no mínimo 8 caracteres, incluindo letras maiúsculas, minúsculas, números e caracteres especiais.");

            if(dto.NewPassword != dto.ConfirmNewPassword)
                return CreateErrorResponse<object>("A nova senha e a confirmação de senha não conferem.");

            // ================================================
            // CONSULTA AO BANCO DE DADOS
            // ================================================

            var (user, errorResponse) = await GetActiveUserByIdAsync<object>(userId, "alteração de senha");
            if (errorResponse != null) return errorResponse;

            // ================================================
            // VERIFICAÇÕES DE SEGURANÇA DA SENHA
            // ================================================

            if (!Verify(dto.CurrentPassword, user.PasswordHash))
                return CreateErrorResponse<object>("A senha atual informada está incorreta.");

            if (Verify(dto.NewPassword, user.PasswordHash))
                return CreateErrorResponse<object>("A nova senha deve ser diferente da senha atual.");

            // ================================================
            // ATUALIZAÇÃO DA SENHA E PERSISTÊNCIA
            // ================================================

            user.PasswordHash = HashPassword(dto.NewPassword);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar nova senha para o usuário {UserId}", userId);
                return CreateErrorResponse<object>("Ocorreu um erro interno ao atualizar a senha.", 500);
            }

            // ================================================
            // RETORNO
            // ================================================

            return CreateSuccessResponse<object>("Senha alterada com sucesso!");
        }

        // ================================================
        // DESATIVAÇÃO DA CONTA
        // ================================================

        public async Task<ApiResponse<object>> DeactivateAccountAsync(Guid userId)
        {
            // CONSULTA AO BANCO DE DADOS

            var (user, errorResponse) = await GetActiveUserByIdAsync<object>(userId, "Desa de conta");
            if (errorResponse != null) return errorResponse;

            // PERSISTÊNCIA E INVALIDAÇÃO DE TOKENS

            user.IsActive = false;
            user.RecoveryToken = null;
            user.TokenExpirationDate = null;
            user.TokenUsed = false;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar desativação de conta para o usuário {UserId}", userId);
                return CreateErrorResponse<object>("Ocorreu um erro interno ao desativar a conta.", 500);
            }

            // RETORNO

            return CreateSuccessResponse<object>("Sua conta foi desativada com sucesso.");
        }

        // ================================================
        // MÉTODOS PRIVADOS AUXILIARES DE VALIDAÇÃO
        // ================================================

        private async Task<(User? user, ApiResponse<T>? errorResponse)> GetActiveUserByIdAsync<T>(Guid userId, string contextAction)
        {
            User? user;

            try
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar usuário {UserId} durante {ContextAction}", userId, contextAction);
                return (null, CreateErrorResponse<T>("Ocorreu um erro interno ao processar os dados do usuário.", 500));
            }

            if (user == null || !user.IsActive)
                return (null, CreateErrorResponse<T>("Usuário não encontrado ou inativo.", 404));

            return (user, null);
        }

        private static bool IsValidFullName(string? rawFullName, out string cleanFullName)
        {
            cleanFullName = System.Text.RegularExpressions.Regex.Replace(rawFullName?.Trim() ?? string.Empty, @"\s+", " ");
            var parts = cleanFullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2 && parts.First().Length >= 3 && parts.Last().Length >= 3;
        }

        private static bool IsValidPhone(string? rawPhone, out string cleanPhone)
        {
            cleanPhone = System.Text.RegularExpressions.Regex.Replace(rawPhone ?? "", @"[^\d]", "");

            if (cleanPhone.Length < 10 || cleanPhone.Length > 11)
                return false;

            if (cleanPhone.Distinct().Count() == 1 || cleanPhone.StartsWith("0"))
                return false;

            if (cleanPhone.Length == 11 && cleanPhone[2] != '9')
                return false;

            return true;
        }

        private static bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return false;

            var passwordRegex = new System.Text.RegularExpressions.Regex(
                @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$"
            );

            return passwordRegex.IsMatch(password);
        }

        private static UserResponseDto MapToUserResponseDto(User user)
        {
            return new UserResponseDto
            {
                Id = user.Id,
                FullName = user.FullName,
                CPF = user.CPF,
                BirthDate = user.BirthDate,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive
            };
        }

        private static ApiResponse<T> CreateErrorResponse<T>(string message, int statusCode = 400)
        {
            return new ApiResponse<T>
            {
                Error = true,
                Message = message,
                StatusCode = statusCode
            };
        }

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
