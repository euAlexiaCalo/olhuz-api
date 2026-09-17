using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using olhuz.API.Models.Responses;
using System.Security.Claims;

namespace olhuz.API.Controllers.Base
{
    [ApiController]
    [Produces("application/json")]
    public abstract class MainController : ControllerBase
    {
        // OBTER O ID DO USUÁRIO AUTENTICADO A PARTIR DO TOKEN JWT
        protected Guid GetUserIdFromToken()
        {
            // Busca a informação dentro das Claims do Token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            // Converte o texto para Guid com segurança
            return Guid.TryParse(userIdClaim, out Guid userId) ? userId : Guid.Empty;
        }

        // TENTAR EXTRAIR E VALIDAR O ID DO USUÁRIO A PARTIR DO TOKEN
        protected bool TryGetAuthenticatedUserId<T>(out Guid userId, out IActionResult? unauthorizedResult)
        {
            // Extrai o ID do usuário contido no token JWT
            userId = GetUserIdFromToken();

            // Se o token foi validado pelo middleware, mas não possui a claim NameIdentifier/ID
            if (userId == Guid.Empty)
            {
                unauthorizedResult = StatusCode(
                    StatusCodes.Status401Unauthorized,
                    CreateErrorResponse<T>("Não foi possível identificar o usuário no token de acesso.", 401)
                );
                return false;
            }

            unauthorizedResult = null;
            return true;
        }

        // MÉTODO AUXILIAR PROTEGIDO PARA FORMATAR RESPOSTAS DE ERRO PADRONIZADOS NOS CONTROLLERS
        protected ApiResponse<T> CreateErrorResponse<T>(string message, int statusCode = 400)
        {
            return new ApiResponse<T>
            {
                Error = true,
                Message = message,
                StatusCode = statusCode
            };
        }
    }
}