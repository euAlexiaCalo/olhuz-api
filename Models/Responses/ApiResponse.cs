namespace olhuz.API.Models.Responses
{
    // Classe padrão utilizada para retorno das requisições
    public class ApiResponse<TData>
    {
        public bool Error { get; set; }
        public string Message { get; set; } = string.Empty;
        public TData? Data { get; set; }
        public int StatusCode { get; set; }
    }
}
