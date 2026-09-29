# ---------------------------------------------------
# ETAPA DE BUILD (SDK do .NET para compilação)
# ---------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia o arquivo do projeto (.csproj) e restaura as dependências
COPY ["olhuz.API.csproj", "./"]
RUN dotnet restore "olhuz.API.csproj"

# Copia todo o código-fonte restante e compila a aplicação
COPY . .
RUN dotnet build "olhuz.API.csproj" -c Release -o /app/build

# Publica os artefatos otimizados de produção
FROM build AS publish
RUN dotnet publish "olhuz.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ---------------------------------------------------
# ETAPA DE EXECUÇÃO (Runtime leve)
# ---------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copia os arquivos publicados da etapa anterior
COPY --from=publish /app/publish .

# Define a porta padrão utilizada pelo container no Render
ENV PORT=8080
EXPOSE 8080

# Comando para iniciar a API ao rodar o container
ENTRYPOINT ["dotnet", "olhuz.API.dll"]