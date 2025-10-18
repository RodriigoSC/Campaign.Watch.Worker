# CampaignWatchWorker

Sistema de monitoramento de campanhas de marketing multicanal em tempo real.

## 📋 Sumário

- [Visão Geral](#visão-geral)
- [Arquitetura](#arquitetura)
- [Requisitos](#requisitos)
- [Configuração](#configuração)
- [Execução](#execução)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Funcionalidades](#funcionalidades)
- [Deployment](#deployment)
- [Monitoramento](#monitoramento)

## 🎯 Visão Geral

O CampaignWatchWorker é um sistema de monitoramento que acompanha a execução de campanhas de marketing em múltiplos canais (Email, SMS, Push, WhatsApp), identificando problemas em tempo real e gerando diagnósticos detalhados.

### Principais Características

- ✅ Monitoramento em tempo real de campanhas pontuais e recorrentes
- ✅ Suporte a múltiplos canais de comunicação
- ✅ Análise de saúde automatizada com diferentes níveis de severidade
- ✅ Arquitetura multitenancy
- ✅ Integração com RabbitMQ para processamento assíncrono
- ✅ Armazenamento em MongoDB
- ✅ Gestão de credenciais via Vault
- ✅ Configuração centralizada via Consul

## 🏗️ Arquitetura

### Estrutura em Camadas

```
┌─────────────────────────────────────────┐
│          1 - Presentation               │
│     (Worker / Consumer)                 │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│          2 - Application                │
│  (Processors, Mappers, Validators)      │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│          3 - Domain                     │
│         (Models, Interfaces)            │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│          4 - Infrastructure             │
│  (Data, MessageQueue, MultiTenant)      │
└─────────────────────────────────────────┘
```

### Componentes Principais

#### 1. **CampaignWatchWorker.Worker**
- Ponto de entrada da aplicação
- Gerencia o ciclo de vida do worker
- Configura injeção de dependências

#### 2. **CampaignWatchWorker.Application**
- **ProcessorApplication**: Orquestra o processamento de campanhas
- **CampaignMapper**: Transforma dados de leitura em modelos de domínio
- **CampaignHealthAnalyzer**: Analisa a saúde de campanhas e execuções
- **Validators**: Validadores específicos por tipo de step

#### 3. **CampaignWatchWorker.Domain.Models**
- Entidades de domínio
- Enums
- Interfaces
- Modelos de diagnóstico

#### 4. **CampaignWatchWorker.Infra**
- **Campaign**: Acesso aos dados de campanhas
- **Effmail/Effsms/Effpush/Effwhatsapp**: Integrações com canais
- **Data**: Repositórios e persistência
- **MessageQueue**: Integração com RabbitMQ
- **MultiTenant**: Gestão de múltiplos clientes

## 📦 Requisitos

### Ambiente de Desenvolvimento

- .NET 8.0 SDK
- Visual Studio 2022 ou VS Code
- MongoDB 4.4+
- RabbitMQ 3.8+
- Vault (HashiCorp)
- Consul

### Dependências Externas

```xml
<!-- Principais packages -->
<PackageReference Include="MongoDB.Driver" Version="2.26.0" />
<PackageReference Include="DTM_MessageQueue.RabbitMQ" Version="1.1.0" />
<PackageReference Include="DTM_Vault.Data" Version="1.0.5" />
<PackageReference Include="DTM_Consul.Data" Version="1.1.0" />
<PackageReference Include="DTM_Logging" Version="1.0.5" />
```

## ⚙️ Configuração

### 1. Variáveis de Ambiente

Configure as seguintes variáveis de ambiente:

```bash
# Ambiente
ASPNETCORE_ENVIRONMENT=Development|Staging|Production

# Vault
CONN_STRING_VAULT=http://vault-server:8200
USER_VAULT=monitoring-user
PASS_VAULT=monitoring-pass

# Tenant
TENANT=<tenant-id>

# Logging
PathLog=/var/logs/campaign-watch-worker
```

### 2. Configuração no Vault

O sistema espera as seguintes chaves no Vault:

```
monitoring/{environment}/data/keys/
├── MongoDB.Campaign.host
├── MongoDB.Campaign.user
├── MongoDB.Campaign.pass
├── MongoDB.Effmail.host
├── MongoDB.Effmail.user
├── MongoDB.Effmail.pass
├── MongoDB.Effsms.host
├── MongoDB.Effsms.user
├── MongoDB.Effsms.pass
├── MongoDB.Effpush.host
├── MongoDB.Effpush.user
├── MongoDB.Effpush.pass
├── MongoDB.Effwhatsapp.host
├── MongoDB.Effwhatsapp.user
├── MongoDB.Effwhatsapp.pass
├── MongoDB.Persistence.host
├── MongoDB.Persistence.user
├── MongoDB.Persistence.pass
├── MongoDB.Persistence.database
├── RabbitMQ.host
├── RabbitMQ.user
├── RabbitMQ.pass
├── RabbitMQ.virtualhost
├── Consul
└── Consul.token
```

### 3. Configuração no Consul

Registre as informações do tenant no Consul:

```json
{
  "id": "tenant-id",
  "name": "Nome do Cliente",
  "databaseCampaign": "campaign_db",
  "databaseEffmail": "effmail_db",
  "databaseEffsms": "effsms_db",
  "databaseEffpush": "effpush_db",
  "databaseEffwhatsapp": "effwhatsapp_db",
  "queueNameMonitoring": "campaign.monitoring.queue"
}
```

Caminho no Consul: `Monitoring/{TENANT_ID}`

## 🚀 Execução

### Desenvolvimento Local

1. Clone o repositório:
```bash
git clone <repository-url>
cd CampaignWatchWorker
```

2. Configure as variáveis de ambiente no `launchSettings.json`

3. Restaure as dependências:
```bash
dotnet restore
```

4. Execute o projeto:
```bash
dotnet run --project src/CampaignWatchWorker.Worker
```

### Docker

```bash
# Build
docker build -t campaign-watch-worker:latest .

# Run
docker run -d \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e CONN_STRING_VAULT=http://vault:8200 \
  -e USER_VAULT=monitoring \
  -e PASS_VAULT=secret \
  -e TENANT=tenant-id \
  -e PathLog=/var/logs \
  campaign-watch-worker:latest
```

## 📁 Estrutura do Projeto

```
CampaignWatchWorker/
├── src/
│   ├── CampaignWatchWorker.Worker/              # Camada de apresentação
│   ├── CampaignWatchWorker.Application/         # Lógica de aplicação
│   │   ├── Mappers/
│   │   ├── Processor/
│   │   ├── QueueEventHandler/
│   │   ├── Services/
│   │   └── Validators/
│   ├── CampaignWatchWorker.Domain.Models/       # Modelos de domínio
│   │   ├── Diagnostics/
│   │   ├── Enums/
│   │   ├── Interfaces/
│   │   └── Read/
│   ├── CampaignWatchWorker.Data/                # Persistência
│   ├── CampaignWatchWorker.Infra.Campaign/      # Dados de campanhas
│   ├── CampaignWatchWorker.Infra.Effmail/       # Canal Email
│   ├── CampaignWatchWorker.Infra.Effsms/        # Canal SMS
│   ├── CampaignWatchWorker.Infra.Effpush/       # Canal Push
│   ├── CampaignWatchWorker.Infra.Effwhatsapp/   # Canal WhatsApp
│   ├── CampaignWatchWorker.Infra.MessageQueue/  # RabbitMQ
│   ├── CampaignWatchWorker.Infra.MultiTenant/   # Multi-tenancy
│   └── CampaignWatchWorker.Infra.Ioc/           # Injeção de dependências
└── tests/
    └── CampaignWatchWorker.Tests/               # Testes unitários
```

## 🎯 Funcionalidades

### 1. Monitoramento de Campanhas

O sistema monitora dois tipos de campanhas:

#### Campanhas Pontuais
- Executadas uma única vez
- Verificação de conclusão
- Análise de resultados finais

#### Campanhas Recorrentes
- Executadas em intervalos regulares (crontab)
- Monitoramento contínuo
- Histórico de execuções

### 2. Validação de Steps

Cada step da jornada é validado por um validador específico:

#### FilterStepValidator
- Detecta filtros travados em consultas
- Alerta para filtros com tempo de execução excessivo
- Timeout: 1h (warning), 2h (critical)

#### ChannelStepValidator
- Valida integração com canais
- Analisa taxa de erro de envios
- Verifica processamento de arquivos
- Monitora status de leads

#### WaitStepValidator
- Verifica steps de espera/pausa
- Detecta atrasos em horários programados
- Valida temporização

#### EndStepValidator
- Valida finalização correta da jornada
- Verifica consistência de status
- Calcula duração total

### 3. Análise de Saúde

O sistema classifica problemas em 4 níveis de severidade:

- **Healthy**: Operação normal
- **Warning**: Alertas que requerem atenção
- **Error**: Erros que impedem funcionamento
- **Critical**: Erros críticos que requerem ação imediata

### 4. Diagnósticos Gerados

- **ExecutionDiagnostic**: Análise completa de uma execução
- **StepDiagnostic**: Diagnóstico de cada step individual
- **MonitoringHealthStatus**: Status consolidado da campanha

### 5. Agendamento de Verificações

O sistema calcula automaticamente a próxima verificação baseado em:

- Tipo de campanha (pontual/recorrente)
- Status atual
- Presença de erros
- Scheduler configurado

## 📊 Monitoramento

### Logs

O sistema gera logs estruturados em:
- Console (stdout)
- Arquivos de log (FileLogger)

Níveis de log:
- `Information`: Operações normais
- `Warning`: Alertas
- `Error`: Erros recuperáveis
- `Critical`: Erros críticos

### Métricas Coletadas

- Total de execuções processadas
- Execuções com erro
- Taxa de erro por canal
- Tempo de processamento
- Leads processados por status

### MongoDB Collections

#### CampaignMonitoring
Armazena campanhas monitoradas:
```javascript
{
  _id: ObjectId,
  clientName: String,
  idCampaign: String,
  name: String,
  campaignType: Number,
  statusCampaign: Number,
  monitoringStatus: Number,
  nextExecutionMonitoring: ISODate,
  lastCheckMonitoring: ISODate,
  healthStatus: {
    isFullyVerified: Boolean,
    hasPendingExecution: Boolean,
    hasIntegrationErrors: Boolean,
    lastMessage: String
  },
  // ... outros campos
}
```

#### ExecutionMonitoring
Armazena execuções monitoradas:
```javascript
{
  _id: ObjectId,
  campaignMonitoringId: ObjectId,
  originalCampaignId: String,
  originalExecutionId: String,
  status: String,
  startDate: ISODate,
  endDate: ISODate,
  totalDurationInSeconds: Number,
  hasMonitoringErrors: Boolean,
  steps: [{
    originalStepId: String,
    name: String,
    type: String,
    status: String,
    totalUser: Number,
    totalExecutionTime: Number,
    error: String,
    channelName: String,
    monitoringNotes: String,
    integrationData: {
      channelName: String,
      integrationStatus: String,
      templateId: String,
      leads: {
        success: Number,
        error: Number,
        blocked: Number,
        optout: Number,
        deduplication: Number
      },
      file: {
        name: String,
        total: Number,
        startedAt: ISODate,
        finishedAt: ISODate
      }
    }
  }]
}
```

### Índices Criados

```javascript
// CampaignMonitoring
db.CampaignMonitoring.createIndex(
  { clientName: 1, idCampaign: 1 },
  { unique: true, name: "Client_IdCampaign_Unique" }
);

db.CampaignMonitoring.createIndex(
  { isActive: 1, nextExecutionMonitoring: 1 },
  { name: "Worker_Monitoring_Query" }
);

// ExecutionMonitoring
db.ExecutionMonitoring.createIndex(
  { originalCampaignId: 1, originalExecutionId: 1 },
  { unique: true, name: "OriginalCampaign_OriginalExecution_Unique" }
);

db.ExecutionMonitoring.createIndex(
  { campaignMonitoringId: 1 },
  { name: "CampaignMonitoringId_Query" }
);
```

## 🚢 Deployment

### Pré-requisitos de Produção

1. MongoDB Replica Set configurado
2. RabbitMQ Cluster
3. Vault em modo HA
4. Consul Cluster
5. Sistema de logs centralizado

### Checklist de Deploy

- [ ] Variáveis de ambiente configuradas
- [ ] Credenciais no Vault
- [ ] Configuração do tenant no Consul
- [ ] Filas RabbitMQ criadas
- [ ] Bancos MongoDB com índices
- [ ] Permissões de rede configuradas
- [ ] Monitoramento configurado
- [ ] Alertas configurados
- [ ] Backup configurado

### Configurações Recomendadas

```yaml
# docker-compose.yml (exemplo)
version: '3.8'
services:
  campaign-watch-worker:
    image: campaign-watch-worker:latest
    restart: always
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - CONN_STRING_VAULT=http://vault:8200
      - USER_VAULT=monitoring
      - PASS_VAULT=${VAULT_PASSWORD}
      - TENANT=${TENANT_ID}
      - PathLog=/var/logs
    volumes:
      - /var/logs/campaign-watch:/var/logs
    depends_on:
      - mongodb
      - rabbitmq
      - vault
      - consul
    networks:
      - monitoring-network
```

### Escalabilidade

O sistema suporta múltiplas instâncias do worker:
- Cada instância processa um tenant específico
- Use múltiplas instâncias para diferentes tenants
- RabbitMQ garante que cada mensagem seja processada uma vez

## 🔧 Troubleshooting

### Problemas Comuns

#### 1. Worker não inicia
```
Erro: ASPNETCORE_ENVIRONMENT cannot be null
Solução: Configure a variável de ambiente ASPNETCORE_ENVIRONMENT
```

#### 2. Não consegue conectar no MongoDB
```
Erro: Unable to connect to MongoDB
Solução:
- Verifique credenciais no Vault
- Verifique conectividade de rede
- Verifique se o MongoDB está rodando
```

#### 3. Não recebe mensagens da fila
```
Solução:
- Verifique se a fila existe no RabbitMQ
- Verifique se o nome da fila no Consul está correto
- Verifique permissões do usuário RabbitMQ
```

#### 4. Erro ao buscar configuração do Consul
```
Erro: Tenant configuration not found
Solução:
- Verifique se o TENANT_ID está correto
- Verifique se a configuração existe no Consul no caminho: Monitoring/{TENANT_ID}
```

## 📝 Desenvolvimento

### Adicionando um Novo Validador

1. Crie uma classe que implemente `IStepValidator`:

```csharp
public class MyCustomStepValidator : IStepValidator
{
    public WorkflowStepTypeEnum SupportedStepType => WorkflowStepTypeEnum.MyCustomType;

    public async Task<StepDiagnostic> ValidateAsync(
        WorkflowStep step,
        ExecutionModel execution,
        CampaignModel campaign)
    {
        // Implementação
    }
}
```

2. Registre no `ResolverIoC`:

```csharp
services.AddTransient<IStepValidator, MyCustomStepValidator>();
```

### Adicionando um Novo Canal

1. Crie o modelo de leitura em `Domain.Models/Read/MyChannel/`
2. Crie a interface do serviço em `Domain.Models/Interfaces/Services/Read/MyChannel/`
3. Implemente o serviço em `Infra.MyChannel/Services/`
4. Crie a factory em `Infra.MyChannel/Factories/`
5. Registre no `Bootstrap.cs`

## 📄 Licença

[Especificar licença]

## 👥 Contribuidores

[Lista de contribuidores]

## 📞 Suporte

Para suporte, entre em contato:
- Email: support@example.com
- Slack: #campaign-watch-worker
- Issues: [GitHub Issues]