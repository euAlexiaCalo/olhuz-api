using olhuz.API.Data;
using olhuz.API.Models;
using olhuz.API.Models.DTOs.Auth;
using olhuz.API.Models.DTOs.User;
using olhuz.API.Models.Responses;
using olhuz.API.Models.Responses.Auth;
using olhuz.API.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using static BCrypt.Net.BCrypt;

namespace olhuz.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthService> _logger;
        private readonly IEmailService _emailService;

        public AuthService(AppDbContext context, ITokenService tokenService, ILogger<AuthService> logger, IEmailService emailService)
        {
            _context = context;
            _tokenService = tokenService;
            _logger = logger;
            _emailService = emailService;
        }

        public async Task<ApiResponse<UserResponseDto>> RegisterAsync(RegisterDto dto)
        {
            // ================================================
            // VALIDAÇÕES DAS REGRAS DE NEGÓCIO DEFINIDAS NO BANCO
            // ================================================

            if (!IsValidFullName(dto.FullName, out string cleanFullName))
                return CreateErrorResponse<UserResponseDto>("Informe seu nome e sobrenome completos (cada palavra deve ter no mínimo 3 letras).");

            if (!IsValidCpf(dto.CPF, out string cleanCpf))
                return CreateErrorResponse<UserResponseDto>("O CPF informado é inválido.");

            if (dto.BirthDate.Date > DateTime.UtcNow.Date)
                return CreateErrorResponse<UserResponseDto>("A data de nascimento não pode ser uma data futura.");

            var minimumAgeDate = DateTime.UtcNow.Date.AddYears(-12);
            if (dto.BirthDate.Date > minimumAgeDate)
                return CreateErrorResponse<UserResponseDto>("O usuário deve ter pelo menos 12 anos para se cadastrar.");

            var maximumAgeDate = DateTime.UtcNow.Date.AddYears(-120);
            if (dto.BirthDate.Date < maximumAgeDate)
                return CreateErrorResponse<UserResponseDto>("Informe uma data de nascimento válida (limite de 120 anos).");

            if (!IsValidPhone(dto.PhoneNumber, out string cleanPhone))
                return CreateErrorResponse<UserResponseDto>("Informe um número de telefone válido com DDD (10 ou 11 dígitos).");

            if (!IsValidEmail(dto.Email, out string normalizedEmail))
                return CreateErrorResponse<UserResponseDto>("O endereço de e-mail informado é inválido.");

            if (!IsValidPassword(dto.Password))
                return CreateErrorResponse<UserResponseDto>("A senha deve conter no mínimo 8 caracteres, incluindo letras maiúsculas, minúsculas, números e caracteres especiais.");

            // ================================================
            // VERIFICAR OS DADOS NO BANCO
            // ================================================

            try
            {
                if (await _context.Users.AnyAsync(user => user.CPF == cleanCpf))
                return CreateErrorResponse<UserResponseDto>("Este CPF já está cadastrado no sistema.");

                if (await _context.Users.AnyAsync(user => user.PhoneNumber == cleanPhone))
                return CreateErrorResponse<UserResponseDto>("Este número de telefone já está cadastrado no sistema.");

                if (await _context.Users.AnyAsync(user => user.Email == normalizedEmail))
                    return CreateErrorResponse<UserResponseDto>("Este email já está cadastrado no sistema.");
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar dados existentes durante o cadastro para o e-mail {Email}", normalizedEmail);
                return CreateErrorResponse<UserResponseDto>("Ocorreu um erro interno ao realizar o cadastro.", 500);
            }

            // ================================================
            // INSTÂNCIA DA NOVA ENTIDADE
            // ================================================

            // Criptografar a senha do usuário
            string passwordHash = HashPassword(dto.Password);

            // Criar uma nova entidade de usuário
            var newUser = new User
            {
                FullName = cleanFullName,
                CPF = cleanCpf,
                BirthDate = dto.BirthDate.Date, // Pega só a data
                Email = normalizedEmail,
                PhoneNumber = cleanPhone,
                PasswordHash = passwordHash,
                Preferences = new UserPreferences(), // Salva as prefêrencias padrão
            };

            // ================================================
            // PROTEÇÃO ADICIONAL caso duas requisições tentem cadastrar os mesmos dados simultaneamente
            // ================================================
            try
            {
                // Salvar o usuário no banco
                await _context.Users.AddAsync(newUser);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Violação de unicidade ao cadastrar usuário com e-mail {Email} ou CPF {CPF}", normalizedEmail, cleanCpf);
                return CreateErrorResponse<UserResponseDto>("Já existe um usuário cadastrado com os dados informados.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno ao salvar novo usuário {Email} no banco de dados", normalizedEmail);
                return CreateErrorResponse<UserResponseDto>("Ocorreu um erro interno ao realizar o cadastro.", 500);
            }

            // ================================================
            // RETORNO MAPEADO DA ENTIDADE SALVA
            // ================================================
            var userResponse = MapToUserResponseDto(newUser);

            return CreateSuccessResponse("Usuário cadastrado com sucesso!", userResponse, 201);
        }

        public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginDto dto)
        {
            // ================================================
            // VALIDAÇÕES DE ENTRADA E FORMATO
            // ================================================

            if (!IsValidEmail(dto.Email, out string normalizedEmail))
                return CreateErrorResponse<LoginResponse>("E-mail ou senha inválidos.");

            if (string.IsNullOrWhiteSpace(dto.Password))
                return CreateErrorResponse<LoginResponse>("E-mail ou senha inválidos.");

            // ================================================
            // CONSULTA AO BANCO DE DADOS
            // ================================================

            User? user;

            try
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante autenticação do usuário.");
                return CreateErrorResponse<LoginResponse>("Ocorreu um erro interno ao processar o login.", 500);
            }

            // ================================================
            // VERIFICAÇÃO DE CREDENCIAIS
            // ================================================

            if (user == null || !Verify(dto.Password, user.PasswordHash))
                return CreateErrorResponse<LoginResponse>("E-mail ou senha inválidos.");

            if (!user.IsActive)
                return CreateErrorResponse<LoginResponse>("Sua conta está desativada. Entre em contato com o suporte.", 403);

            // ================================================
            // GERAR TOKEN JWT
            // ================================================

            TokenResultDto tokenResult;

            try
            {
                // Gera o token e a expiração com o serviço injetado
                tokenResult = _tokenService.GenerateToken(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar token JWT para o usuário {UserId}", user.Id);
                return CreateErrorResponse<LoginResponse>("Ocorreu um erro interno ao gerar a sessão de acesso.", 500);
            }

            // ================================================
            // MONTAGEM DO OBJETO DE RESPOSTA
            // ================================================

            var loginResponse = new LoginResponse
            {
                Token = tokenResult.Token,
                ExpiresAt = tokenResult.ExpiresAt,
                User = MapToUserResponseDto(user)
            };

            return CreateSuccessResponse("Login realizado com sucesso!", loginResponse);
        }
        
        public async Task<ApiResponse<object>> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            // ================================================
            // VALIDAÇÕES DE ENTRADA E FORMATO
            // ================================================

            if (!IsValidEmail(dto.Email, out string normalizedEmail))
                return CreateErrorResponse<object>("O endereço de e-mail informado é inválido.");

            // ================================================
            // CONSULTA AO BANCO DE DADOS
            // ================================================

            User? user;

            try
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar usuário durante a solicitação de recuperação de senha para {Email}", normalizedEmail);
                return CreateErrorResponse<object>("Ocorreu um erro interno ao processar a solicitação de recuperação de senha.", 500);
            }

            // ================================================
            // PROTEÇÃO ANTI-ENUMERAÇÃO DE USUÁRIOS
            // ================================================

            if (user == null || !user.IsActive)
                return CreateSuccessResponse<object>("Se o e-mail informado estiver cadastrado em nosso sistema, você receberá as instruções para redefinição de senha.");

            // ================================================
            // GERAÇÃO DO TOKEN E DEFINIÇÃO DA EXPIRAÇÃO
            // ================================================

            // Gera um token único e aleatório com 6 dígitos
            string token = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            // Atualiza os campos de recuperação na entidade do usuário
            user.RecoveryToken = HashPassword(token);
            user.TokenExpirationDate = DateTime.UtcNow.AddMinutes(15);
            user.TokenUsed = false;

            // ================================================
            // PERSISTÊNCIA NO BANCO E ENVIO DO E-MAIL
            // ================================================

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar token de recuperação para o e-mail {Email}", normalizedEmail);
                return CreateErrorResponse<object>("Ocorreu um erro interno ao processar a solicitação de recuperação de senha.", 500);
            }

            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, token);
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar e-mail de recuperação para {Email}", user.Email);
            }

            // ================================================
            // RETORNO
            // ================================================

            return CreateSuccessResponse<object>("Se o e-mail informado estiver cadastrado em nosso sistema, você receberá as instruções para redefinição de senha.");
        }

        public async Task<ApiResponse<object>> ResetPasswordAsync(ResetPasswordDto dto)
        {
            // ================================================
            // VALIDAÇÕES DE ENTRADA E FORMATO
            // ================================================

            if (!IsValidEmail(dto.Email, out string normalizedEmail))
                return CreateErrorResponse<object>("Código de verificação ou e-mail inválidos.");

            if (string.IsNullOrWhiteSpace(dto.Token) || dto.Token.Length != 6)
                return CreateErrorResponse<object>("O código de verificação deve ter exatamente 6 dígitos.");

            if (!IsValidPassword(dto.NewPassword))
                return CreateErrorResponse<object>("A nova senha deve conter no mínimo 8 caracteres, incluindo letras maiúsculas, minúsculas, números e caracteres especiais.");

            // ================================================
            // CONSULTA AO BANCO DE DADOS
            // ================================================

            User? user;

            try
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar usuário durante a redefinição de senha para {Email}", normalizedEmail);
                return CreateErrorResponse<object>("Ocorreu um erro interno ao processar a solicitação.", 500);
            }

            // ================================================
            // VERIFICAÇÃO DE VALIDADE DO TOKEN E USUÁRIO
            // ================================================

            if (user == null || !user.IsActive)
                return CreateErrorResponse<object>("Código de verificação inválido ou expirado.");

            if (user.TokenUsed)
                return CreateErrorResponse<object>("Este código de verificação já foi utilizado.");

            if (user.TokenExpirationDate == null || user.TokenExpirationDate < DateTime.UtcNow)
                return CreateErrorResponse<object>("O código de verificação expirou. Solicite um novo código.");

            if (string.IsNullOrEmpty(user.RecoveryToken) || !Verify(dto.Token, user.RecoveryToken))
                return CreateErrorResponse<object>("Código de verificação inválido.");

            // ================================================
            // VERIFICAÇÃO DE DIVERGÊNCIA DA SENHA ATUAL
            // ================================================

            if (Verify(dto.NewPassword, user.PasswordHash))
                return CreateErrorResponse<object>("A nova senha deve ser diferente da senha atual.");

            // ================================================
            // ATUALIZAÇÃO DA SENHA E INVALIDAÇÃO DO TOKEN
            // ================================================

            // Criptografa a nova senha do usuário
            user.PasswordHash = HashPassword(dto.NewPassword);
            user.RecoveryToken = null;
            user.TokenExpirationDate = null;
            user.TokenUsed = true;

            // ================================================
            // PERSISTÊNCIA NO BANCO DE DADOS
            // ================================================

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao redefinir a senha do usuário {Email}", normalizedEmail);
                return CreateErrorResponse<object>("Ocorreu um erro interno ao redefinir a senha.", 500);
            }

            // ================================================
            // RETORNO
            // ================================================

            return CreateSuccessResponse<object>("Senha redefinida com sucesso!");
        }


        // ================================================
        // MÉTODOS PRIVADOS AUXILIARES DE VALIDAÇÃO
        // ================================================

        private bool IsValidFullName(string? rawFullName, out string cleanFullName)
        {
            // Remove espaços das pontas e substitui múltiplos espaços internos por um único espaço
            cleanFullName = System.Text.RegularExpressions.Regex.Replace(rawFullName?.Trim() ?? string.Empty,
        @"\s+",
        " "
    );

            // Separa as palavras do nome
            var parts = cleanFullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Verifica se possui pelo menos 2 palavras com no mínimo 3 letras o primeiro e último
            return parts.Length >= 2 && parts.First().Length >= 3
        && parts.Last().Length >= 3;
        }

        private bool IsValidCpf(string? rawCpf, out string cleanCpf)
        {
            // Remove qualquer caractere que não seja número
            cleanCpf = System.Text.RegularExpressions.Regex.Replace(rawCpf ?? "", @"[^\d]", "");

            if (string.IsNullOrWhiteSpace(cleanCpf) || cleanCpf.Length != 11)
                return false;

            // Elimina CPFs inválidos com todos os dígitos iguais)
            if (cleanCpf.Distinct().Count() == 1)
                return false;

            // Cálculo do 1º Dígito Verificador
            int[] multiplier1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            string tempCpf = cleanCpf.Substring(0, 9);
            int sum = 0;

            for (int i = 0; i < 9; i++)
                sum += (tempCpf[i] - '0') * multiplier1[i];

            int remainder = sum % 11;
            int digit1 = remainder < 2 ? 0 : 11 - remainder;

            tempCpf += digit1;

            // Cálculo do 2º Dígito Verificador
            int[] multiplier2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            sum = 0;

            for (int i = 0; i < 10; i++)
                sum += (tempCpf[i] - '0') * multiplier2[i];

            remainder = sum % 11;
            int digit2 = remainder < 2 ? 0 : 11 - remainder;

            return cleanCpf.EndsWith(digit1.ToString() + digit2.ToString());
        }

        private bool IsValidPhone(string? rawPhone, out string cleanPhone)
        {
            // Sanitização: Remove qualquer caractere que não seja número
            cleanPhone = System.Text.RegularExpressions.Regex.Replace(rawPhone ?? "", @"[^\d]", "");

            // Checa se tem DDD + Fixo (10 dígitos) ou DDD + Celular (11 dígitos)
            if (cleanPhone.Length < 10 || cleanPhone.Length > 11)
                return false;

            // Bloqueio de números com todos os algarismos repetidos
            if (cleanPhone.Distinct().Count() == 1)
                return false;

            // Garante que não começe com 0
            if (cleanPhone.StartsWith("0"))
                return false;

            // Validação de formato para celular (o 3º dígito, logo após o DDD, deve ser 9)
            if (cleanPhone.Length == 11 && cleanPhone[2] != '9')
                return false;

            return true;
        }

        private bool IsValidEmail(string? rawEmail, out string normalizedEmail)
        {
            // Sanitizar: Tratar nulos, remover espaços e converter para minúsculas
            normalizedEmail = rawEmail?.Trim().ToLowerInvariant() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(normalizedEmail) || normalizedEmail.Length > 150)
                return false;

            // Regex estrito para validação de e-mail
            var emailRegex = new System.Text.RegularExpressions.Regex(
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$"
            );

            return emailRegex.IsMatch(normalizedEmail);
        }

        private bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return false;

            // Regex que valida: pelo menos 1 maiúscula, 1 minúscula, 1 número e 1 caractere especial
            var passwordRegex = new System.Text.RegularExpressions.Regex(
                @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$"
            );

            return passwordRegex.IsMatch(password);
        }

        private UserResponseDto MapToUserResponseDto(User user)
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

        private ApiResponse<T> CreateErrorResponse<T>(string message, int statusCode = 400)
        {
            return new ApiResponse<T>
            {
                Error = true,
                Message = message,
                StatusCode = statusCode
            };
        }

        private ApiResponse<T> CreateSuccessResponse<T>(string message, T? data = default, int statusCode = 200)
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
