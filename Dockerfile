# Estágio 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar arquivos de configuração do NuGet
COPY ["src/CampaignWatchWorker.Worker/nuget.config", "src/CampaignWatchWorker.Worker/"]

# Copiar arquivos .csproj e restaurar dependências
COPY ["src/CampaignWatchWorker.Worker/CampaignWatchWorker.Worker.csproj", "src/CampaignWatchWorker.Worker/"]
COPY ["src/CampaignWatchWorker.Application/CampaignWatchWorker.Application.csproj", "src/CampaignWatchWorker.Application/"]
COPY ["src/CampaignWatchWorker.Domain.Models/CampaignWatchWorker.Domain.Models.csproj", "src/CampaignWatchWorker.Domain.Models/"]
COPY ["src/CampaignWatchWorker.Data/CampaignWatchWorker.Data.csproj", "src/CampaignWatchWorker.Data/"]
COPY ["src/CampaignWatchWorker.Campaign/CampaignWatchWorker.Infra.Campaign.csproj", "src/CampaignWatchWorker.Campaign/"]
COPY ["src/CampaignWatchWorker.Infra.Effmail/CampaignWatchWorker.Infra.Effmail.csproj", "src/CampaignWatchWorker.Infra.Effmail/"]
COPY ["src/CampaignWatchWorker.Infra.Effsms/CampaignWatchWorker.Infra.Effsms.csproj", "src/CampaignWatchWorker.Infra.Effsms/"]
COPY ["src/CampaignWatchWorker.Infra.Effpush/CampaignWatchWorker.Infra.Effpush.csproj", "src/CampaignWatchWorker.Infra.Effpush/"]
COPY ["src/CampaignWatchWorker.Infra.Effwhatsapp/CampaignWatchWorker.Infra.Effwhatsapp.csproj", "src/CampaignWatchWorker.Infra.Effwhatsapp/"]
COPY ["src/CampaignWatchWorker.Infra.MessageQueue/CampaignWatchWorker.Infra.MessageQueue.csproj", "src/CampaignWatchWorker.Infra.MessageQueue/"]
COPY ["src/CampaignWatchWorker.Infra.MultiTenant/CampaignWatchWorker.Infra.MultiTenant.csproj", "src/CampaignWatchWorker.Infra.MultiTenant/"]
COPY ["src/CampaignWatchWorker.Infra.Ioc/CampaignWatchWorker.Infra.Ioc.csproj", "src/CampaignWatchWorker.Infra.Ioc/"]

# Restaurar dependências
RUN dotnet restore "src/CampaignWatchWorker.Worker/CampaignWatchWorker.Worker.csproj" --configfile "src/CampaignWatchWorker.Worker/nuget.config"

# Copiar todo o código fonte
COPY . .

# Build do projeto
WORKDIR "/src/src/CampaignWatchWorker.Worker"
RUN dotnet build "CampaignWatchWorker.Worker.csproj" -c Release -o /app/build

# Estágio 2: Publish
FROM build AS publish
RUN dotnet publish "CampaignWatchWorker.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Estágio 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Criar usuário não-root para executar a aplicação
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Criar diretório de logs e dar permissões
RUN mkdir -p /var/logs && chown -R appuser:appuser /var/logs

# Copiar arquivos publicados
COPY --from=publish /app/publish .

# Mudar para usuário não-root
USER appuser

# Configurar timezone
ENV TZ=America/Sao_Paulo

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD pgrep -f "CampaignWatchWorker.Worker" || exit 1

# Ponto de entrada
ENTRYPOINT ["dotnet", "CampaignWatchWorker.Worker.dll"]