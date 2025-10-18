# Arquitetura do Sistema

## Visão Geral

O CampaignWatchWorker é um sistema de monitoramento distribuído que analisa a execução de campanhas de marketing multicanal em tempo real.

## Diagrama de Componentes

```
┌─────────────────────────────────────────────────────────────────┐
│                        EXTERNAL SYSTEMS                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Campaign System    Effmail    Effsms    Effpush    Effwhatsapp │
│       (MongoDB)    (MongoDB)  (MongoDB)  (MongoDB)   (MongoDB)  │
│                                                                 │
└────────────┬────────────────────────────────────────────────────┘
             │
             │ Read Data
             ▼
┌─────────────────────────────────────────────────────────────────┐
│                    CAMPAIGNWATCHWORKER                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                   1. PRESENTATION                        │   │
│  │                                                          │   │
│  │  ┌────────────┐        ┌──────────────┐                  │   │
│  │  │   Worker   │◄──────►│   Consumer   │                  │   │
│  │  └────────────┘        └──────────────┘                  │   │
│  │         │                      │                         │   │
│  └─────────┼──────────────────────┼──────────────────────_──┘   │
│            │                      │                             │
│            ▼                      ▼                             │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                   2. APPLICATION                         │   │
│  │                                                          │   │
│  │  ┌──────────────────┐  ┌──────────────────────────┐      │   │
│  │  │ Processor        │  │ CampaignHealthAnalyzer   │      │   │
│  │  │ Application      │──│ - Execution Analysis     │      │   │
│  │  │ - Orchestration  │  │ - Campaign Analysis      │      │   │
│  │  │ - Error Handling │  └──────────────────────────┘      │   │
│  │  └─────────┬────────┘                                    │   │
│  │            │                                             │   │
│  │            ├─────┐                                       │   │
│  │            │     │                                       │   │
│  │  ┌─────────▼─┐ ┌─▼──────────┐  ┌──────────────────┐      │   │
│  │  │ Campaign  │ │ Step       │  │ Queue Event      │      │   │
│  │  │ Mapper    │ │ Validators │  │ Handler          │      │   │
│  │  └───────────┘ └────────────┘  └──────────────────┘      │   │
│  │                                                          │   │
│  └──────────────────────────────────────────────────────────┘   │
│            │                                                    │
│            ▼                                                    │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                     3. DOMAIN                            │   │
│  │                                                          │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐       │   │
│  │  │   Models    │  │ Diagnostics │  │  Enums      │       │   │
│  │  │ - Campaign  │  │ - Execution │  │ - Health    │       │   │
│  │  │ - Execution │  │   Diagnostic│  │ - Step Type │       │   │
│  │  │ - Steps     │  │ - Step Diag │  │ - Channel   │       │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘       │   │
│  │                                                          │   │
│  └──────────────────────────────────────────────────────────┘   │
│            │                                                    │
│            ▼                                                    │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                  4. INFRASTRUCTURE                       │   │
│  │                                                          │   │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │   │
│  │  │ Data Layer   │  │ Message      │  │ Multi-Tenant │    │   │
│  │  │ - Repos      │  │ Queue        │  │ - Tenant     │    │   │
│  │  │ - Factories  │  │ - RabbitMQ   │  │ - Resolver   │    │   │
│  │  └──────────────┘  └──────────────┘  └──────────────┘    │   │
│  │                                                          │   │
│  │  ┌──────────────────────────────────────────────────┐    │   │
│  │  │          Channel Integrations                    │    │   │
│  │  │  - Campaign  - Effmail  - Effsms  - Effpush      │    │   │
│  │  │  - Effwhatsapp                                   │    │   │
│  │  └──────────────────────────────────────────────────┘    │   │
│  │                                                          │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
             │                                │
             ▼                                ▼
┌─────────────────────┐          ┌──────────────────────┐
│   Persistence DB    │          │    RabbitMQ          │
│     (MongoDB)       │          │  (Message Broker)    │
└─────────────────────┘          └──────────────────────┘
             │                                │
             ▼                                ▼
┌─────────────────────────────────────────────────────────┐
│              CONFIGURATION & SECRETS                    │
├─────────────────────────────────────────────────────────┤
│  Vault (Credentials)          Consul (Tenant Config)    │
└─────────────────────────────────────────────────────────┘
```

## Fluxo de Processamento

### 1. Recepção de Mensagem

```
RabbitMQ Queue → QueueEventHandler → Consumer → ProcessorApplication
```

**Detalhes:**
- Mensagem contém ID da campanha
- Worker registra handler na fila do tenant
- Mensagem é deserializada como objeto
- ACK é enviado após processamento

### 2. Busca de Dados

```
ProcessorApplication → CampaignReadModelService → MongoDB (Campaign DB)
                    ↓
                    └→ GetCampaignById(campaignId)
                    └→ GetExecutionsByCampaign(campaignId)
```

**Detalhes:**
- Busca dados da campanha
- Busca todas as execuções (onde FlagCount = false)
- Dados são retornados como ReadModels

### 3. Mapeamento de Dados

```
ReadModels → CampaignMapper → Domain Models
                            ↓
                            ├→ CampaignModel
                            └→ ExecutionModel[]
                                    ├→ WorkflowStep[]
                                    └→ ChannelIntegrationData
```

**Detalhes:**
- CampaignReadModel → CampaignModel
- ExecutionReadModel → ExecutionModel
- Para cada step do tipo Channel:
  - Busca dados do canal correspondente
  - Agrega contagens de leads
  - Mapeia dados de arquivo

### 4. Análise de Saúde

```
ExecutionModel → CampaignHealthAnalyzer → ExecutionDiagnostic
                                        ↓
                                        └→ StepValidators
                                              ├→ FilterStepValidator
                                              ├→ ChannelStepValidator
                                              ├→ WaitStepValidator
                                              └→ EndStepValidator
```

**Para cada execução:**
1. Analisa cada step individualmente
2. Gera StepDiagnostic com severity (Healthy/Warning/Error/Critical)
3. Consolida diagnósticos em ExecutionDiagnostic
4. Determina OverallHealth da execução

**Para a campanha:**
1. Analisa todas as execuções
2. Verifica tipo (Pontual/Recorrente)
3. Calcula MonitoringHealthStatus
4. Define próxima verificação

### 5. Persistência

```
Domain Models → Repositories → MongoDB (Persistence DB)
             ↓
             ├→ CampaignModelRepository.AtualizarCampanhaAsync()
             └→ ExecutionModelRepository.AtualizarExecucaoAsync()
```

**Detalhes:**
- Upsert baseado em chaves únicas
- CampaignMonitoring: (ClientName, IdCampaign)
- ExecutionMonitoring: (OriginalCampaignId, OriginalExecutionId)

## Componentes Detalhados

### Step Validators

#### FilterStepValidator
**Responsabilidade:** Validar steps de filtro de dados

**Regras:**
- Status "Completed" → Healthy
- Em execução > 1h → Warning
- Em execução > 2h → Critical
- Com erro → Error

#### ChannelStepValidator
**Responsabilidade:** Validar integração com canais

**Regras:**
- Sem dados de integração → Warning
- Status de integração "Error" → Error
- Taxa de erro > 50% → Error
- Taxa de erro > 20% → Warning
- Arquivo processando > 1h → Warning

#### WaitStepValidator
**Responsabilidade:** Validar steps de espera/timing

**Regras:**
- Status "Completed" → Healthy
- Atraso > 15min → Error
- Atraso > 5min → Warning

#### EndStepValidator
**Responsabilidade:** Validar finalização da jornada

**Regras:**
- Step completo + Execution completa → Healthy
- Step completo + Execution não completa → Warning
- Campanha pontual não finalizada → Warning

### CampaignHealthAnalyzer

**Método:** `AnalyzeExecutionAsync`
- Itera sobre cada step
- Seleciona validator apropriado
- Coleta diagnósticos
- Calcula OverallHealth
- Gera summary

**Método:** `AnalyzeCampaignHealthAsync`
- Verifica tipo de campanha
- Conta execuções por status
- Identifica problemas
- Gera MonitoringHealthStatus
- Define mensagem consolidada

### Cálculo de Próxima Verificação

```csharp
if (Campaign inativa || deletada)
    return null;
    
if (HasIntegrationErrors)
    return now + 5 minutos;
    
if (CampanhaPontual) {
    if (Completed) return null;
    if (Executing) return now + 10 minutos;
    if (Scheduled && futuro) return StartDateTime - 5 minutos;
    return now + 30 minutos;
}

if (CampanhaRecorrente) {
    if (HasPendingExecution) return now + 10 minutos;
    if (ForaPeríodo) return null;
    return now + 1 hora;
}
```

## Padrões de Design Utilizados

### 1. Repository Pattern
- Abstração do acesso a dados
- `ICampaignModelRepository`, `IExecutionModelRepository`

### 2. Factory Pattern
- Criação de conexões MongoDB
- `MongoDbFactory`, `PersistenceMongoFactory`

### 3. Strategy Pattern
- Validadores de steps
- `IStepValidator` com implementações específicas

### 4. Dependency Injection
- Inversão de controle total
- Configuração em `Bootstrap.cs`

### 5. Multi-Tenancy
- Isolamento por tenant
- `ITenant` com configurações específicas

## Decisões Arquiteturais

### Por que MongoDB?
- Flexibilidade de schema
- Performance em leitura
- Suporte a documentos aninhados
- Agregações complexas para contagem de leads

### Por que RabbitMQ?
- Confiabilidade (persistent messages)
- Suporte a múltiplos consumers
- Dead Letter Queue
- Fácil monitoramento

### Por que Vault + Consul?
- **Vault:** Segurança de credenciais
- **Consul:** Configuração dinâmica
- Separação de concerns
- Multi-ambiente

### Por que não usar Cache?
- Dados mudam frequentemente
- Verificações são agendadas (não sob demanda)
- MongoDB é suficientemente rápido
- Cache adicionaria complexidade

## Considerações de Segurança

### Credenciais
- Nunca em código ou configuração
- Sempre no Vault
- Rotação via Vault

### Comunicação
- MongoDB com autenticação
- RabbitMQ com usuário/senha
- TLS em produção (recomendado)

### Dados Sensíveis
- Logs não contêm PII
- Dados de integração são opcionais
- Raw data é armazenado apenas para debug

## Escalabilidade

### Horizontal Scaling
- Múltiplas instâncias do worker
- Cada instância processa um tenant
- RabbitMQ distribui mensagens

### Vertical Scaling
- Aumentar recursos do container
- Otimizar queries MongoDB
- Índices adequados

### Limitações Atuais
- Processamento síncrono por campanha
- Sem paralelização de execuções
- Sem batching de persistência

### Melhorias Futuras
- Processamento paralelo de execuções
- Batch insert/update no MongoDB
- Cache de configurações do Vault/Consul
- Métricas e observabilidade (Prometheus/Grafana)

## Monitoramento e Observabilidade

### Logs
- Estruturados (FileLogger)
- Níveis: Info, Warning, Error, Critical
- Contexto: TenantId, CampaignId, ExecutionId

### Métricas (Futuro)
- Taxa de processamento
- Latência por operação
- Erros por tipo
- Health checks

### Alertas (Futuro)
- Critical errors
- Alta taxa de falhas
- Processamento travado
- Filas cheias

## Performance

### Otimizações Implementadas
- Índices MongoDB
- Upsert em vez de find+update
- Agregações otimizadas para leads
- Reuso de conexões (Factory pattern)

### Benchmarks Esperados
- Processamento: < 5s por campanha simples
- Processamento: < 30s por campanha complexa
- Throughput: 100+ campanhas/minuto
- Latência MongoDB: < 50ms

## Testes

### Unitários
- Validators
- Mappers
- Health Analyzer

### Integração
- Repositories
- Factories
- Message Queue

### End-to-End
- Fluxo completo de processamento
- Múltiplos tipos de campanhas
- Cenários de erro

## Manutenção

### Adicionando um Novo Canal
1. Criar ReadModel em `Domain.Models/Read/`
2. Criar interface em `Domain.Models/Interfaces/Services/Read/`
3. Implementar serviço em `Infra.NewChannel/`
4. Criar factory
5. Registrar em `Bootstrap.cs`
6. Adicionar ao `CampaignMapper`

### Adicionando um Novo Validador
1. Implementar `IStepValidator`
2. Registrar em `Application/Resolver/ResolverIoC.cs`
3. Adicionar testes

### Modificando Lógica de Análise
1. Atualizar `CampaignHealthAnalyzer`
2. Ajustar validators se necessário
3. Atualizar testes
4. Documentar mudanças

## Referências

- [Clean Architecture - Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [MongoDB Best Practices](https://www.mongodb.com/docs/manual/administration/production-notes/)
- [RabbitMQ Patterns](https://www.rabbitmq.com/getstarted.html)
- [.NET Dependency Injection](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [HashiCorp Vault](https://www.vaultproject.io/docs)
- [Consul Service Discovery](https://www.consul.io/docs)