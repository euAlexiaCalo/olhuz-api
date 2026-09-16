using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using olhuz.API.Data;
using olhuz.API.Services;
using olhuz.API.Services.Interfaces;

namespace olhuz.API
{
    public class Program
    {
        // Método responsável por iniciar a aplicação
        public static void Main(string[] args)
        {
            // Configura todos os serviços e recursos que a aplicação utilizará
            var builder = WebApplication.CreateBuilder(args);

            // ========================================
            // CONFIGURAÇÃO DO BANCO DE DADOS
            // ========================================

            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                // Provedor do banco
                options.UseSqlServer(
                    // Obtém a string de conexão
                    builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            // ========================================
            // CONFIGURAÇÃO DA AUTENTICAÇÃO JWT
            // ========================================

            // Lê as strings registradas no appsettings.json
            var jwtKey = builder.Configuration["Jwt:Key"];
            var jwtIssuer = builder.Configuration["Jwt:Issuer"];
            var jwtAudience = builder.Configuration["Jwt:Audience"];

            // Configura o .NET para usar o JwtBearerDefaults com essas configurações
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!))
                };
            });

            // ========================================
            // CONFIGURAÇÃO DOS CONTROLLERS
            // ========================================

            // Habilita os Controllers na API
            builder.Services.AddControllers();

            // ========================================
            // CONFIGURAÇÃO DO SWAGGER PARA TESTES DE ENDPOINTS
            // ========================================

            // Permite que o Swagger identifique os endpoints
            builder.Services.AddEndpointsApiExplorer();

            // Habilita a geração automática da documentação.
            builder.Services.AddSwaggerGen();

            // ========================================
            // REGISTRO DE SERVIÇOS PERSONALIZADOS
            // ========================================

            // Injeção de dependência das interfaces e implementações dos serviços
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IAuthService, AuthService>();

            // ========================================
            // CRIA A APLICAÇÃO
            // ========================================

            // Constrói a aplicação com as configurações acima
            var app = builder.Build();

            // ========================================
            // PIPELINE DE EXECUÇÃO
            // ========================================
            if (app.Environment.IsDevelopment())
            {
                // Gera documentação da API.
                app.UseSwagger();

                // Interface gráfica do Swagger
                app.UseSwaggerUI();
            }

            // ========================================
            // MIDDLEWARES DA APLICAÇÃO
            // ========================================

            // Redireciona automaticamente requisições HTTP para HTTPS.
            app.UseHttpsRedirection();

            // Habilita autenticação JWT.
            app.UseAuthentication();

            // Habilita autorização baseada em permissões.
            app.UseAuthorization();

            // Mapeia automaticamente todos os Controllers.
            app.MapControllers();

            // ========================================
            // INICIA A API
            // ========================================

            // Coloca a aplicação em execução e aguardando requisições.
            app.Run();
        }
    }
}