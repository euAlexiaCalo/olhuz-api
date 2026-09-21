using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!)),
                    ClockSkew = TimeSpan.Zero
                };
            });

            // ========================================
            // CONFIGURAÇÃO DOS CONTROLLERS
            // ========================================

            // Habilita os Controllers na API
            builder.Services.AddControllers();

            builder.Services.AddAuthorization();

            // ========================================
            // CONFIGURAÇÃO DO SWAGGER COM SUPORTE A JWT
            // ========================================

            // Permite que o Swagger identifique os endpoints
            builder.Services.AddEndpointsApiExplorer();

            // Habilita a geração automática da documentação e autenticação JWT
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "Olhuz API", Version = "v1" });

                // Define a definição de segurança para o Bearer Token
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Insira o token JWT gerado após o login."
                });

                // Aplica a exigência de segurança globalmente nos testes do Swagger
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // ========================================
            // REGISTRO DE SERVIÇOS PERSONALIZADOS
            // ========================================

            // Injeção de dependência das interfaces e implementações dos serviços
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IUserPreferencesService, UserPreferencesService>();
            builder.Services.AddScoped<IReadingService, ReadingService>();

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

            // Habilita arquivos estáticos
            app.UseStaticFiles();

            // Redireciona automaticamente requisições HTTP para HTTPS fora do ambiente de desenvolvimento
            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

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