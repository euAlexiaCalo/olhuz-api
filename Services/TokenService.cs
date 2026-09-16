using olhuz.API.Models;
using olhuz.API.Models.DTOs.Auth;
using olhuz.API.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace olhuz.API.Services
{
    // É responsável por gerar tokens de autenticação JWT para usuários validados
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Método para gerar o token para o usuário autenticado
        public TokenResultDto GenerateToken(User user)
        {
            // ================================================
            // CONFIGURAÇÕES DO JWT
            // ================================================

            string secretKey = _configuration["Jwt:Key"]!;
            string issuer = _configuration["Jwt:Issuer"]!;
            string audience = _configuration["Jwt:Audience"]!;
            int expireDays = int.Parse(_configuration["Jwt:ExpireDays"]!);

            // Define a data/hora exata de expiração em UTC
            DateTime expiresAt = DateTime.UtcNow.AddDays(expireDays);

            // Chave de assinatura
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            // Credenciais de assinatura usando o algoritmo HMAC SHA256
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Informações sobre o usuário que serão armazenadas no token
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };


            // Cria o token JWT com as informações do usuário, tempo de expiração e credenciais de assinatura
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
              );

            // Converte o token JWT para string
            string tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // Retorna o objeto contendo o token e a data de expiração
            return new TokenResultDto
            {
                Token = tokenString,
                ExpiresAt = expiresAt
            };
        }
    }
}
