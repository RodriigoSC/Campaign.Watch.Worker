# Guia de Troubleshooting

Este guia contém soluções para problemas comuns do CampaignWatchWorker.

## Índice

- [Problemas de Inicialização](#problemas-de-inicialização)
- [Problemas de Conectividade](#problemas-de-conectividade)
- [Problemas de Processamento](#problemas-de-processamento)
- [Problemas de Performance](#problemas-de-performance)
- [Problemas de Dados](#problemas-de-dados)

## Problemas de Inicialização

### Worker não inicia

#### Erro: "ASPNETCORE_ENVIRONMENT cannot be null"

**Causa:** Variável de ambiente não configurada.

**Solução:**
```bash
export ASPNETCORE_ENVIRONMENT=Development
```

#### Erro: "Unable to connect to Vault"

**Causa:** Vault não está acessível ou credenciais incorretas.

**Diagnóstico:**
```bash
# Verificar se o Vault está rodando
curl http://localhost:8200/v1/sys/health

# Testar autenticação
export VAULT_ADDR=http://localhost:8200
export VAULT_TOKEN=root
vault status
```

**Solução:**
1. Verificar se o Vault está rodando: `docker ps | grep vault`
2. Verificar as variáveis de ambiente: `CONN_STRING_VAULT`, `USER_VAULT`, `PASS_VAULT`
3. Se for desenvolvimento, executar: `make setup-vault`

#### Erro: "Tenant configuration not found"

**Causa:** Configuração do tenant não existe no Consul.

**Diagnóstico:**
```bash
# Verificar se o tenant existe
curl http://localhost:8500/v1/kv/Monitoring/YOUR_TENANT_ID?raw
```

**Solução:**
```bash
# Configurar tenant no Consul
make setup-consul TENANT_ID=your-tenant-id

# Ou manualmente
./scripts/setup-consul.sh your-tenant-id
```

## Problemas de Conectividade

### Não consegue conectar no MongoDB

#### Erro: "MongoDB connection timeout"

**Diagnóstico:**
```bash
# Verificar se o MongoDB está rodando
docker ps | grep mongodb

# Testar conexão
mongosh "mongodb://admin:admin123@localhost:27017"

# Verificar credenciais no Vault
vault kv get monitoring/development/data/keys | grep MongoDB
```

**Solução:**
1. Verificar se o MongoDB está rodando
2. Verificar credenciais no Vault
3. Verificar network/firewall
4. Verificar se o banco existe:
```bash
mongosh "mongodb://admin:admin123@localhost:27017"
> show dbs
> use campaign_db
```

### Não recebe mensagens do RabbitMQ

#### Erro: "Queue not found"

**Diagnóstico:**
```bash
# Verificar se a fila existe
curl -u admin:admin123 http://localhost:15672/api/queues/%2F/campaign.monitoring.queue

# Listar todas as filas
curl -u admin:admin123 http://localhost:15672/api/queues | jq '.[].name'
```

**Solução:**
```bash
# Criar filas necessárias
make setup-rabbitmq

# Ou manualmente
./scripts/setup-rabbitmq.sh
```

#### Problema: Fila existe mas não recebe mensagens

**Diagnóstico:**
1. Verificar se há mensagens na fila:
```bash
curl -u admin:admin123 http://localhost:15672/api/queues/%2F/campaign.monitoring.queue | jq '.messages'
```

2. Verificar bindings:
```bash
curl -u admin:admin123 http://localhost:15672/api/bindings/%2F/e/campaign.monitoring.exchange/q/campaign.monitoring.queue
```

**Solução:**
```bash
# Enviar mensagem de teste
make send-test-message

# Verificar logs do worker
make watch-logs
```

## Problemas de Processamento

### Campanhas não são processadas

#### Problema: Worker está rodando mas não processa mensagens

**Diagnóstico:**
1. Verificar se há mensagens na fila:
```bash
curl -u admin:admin123 http://localhost:15672/api/queues/%2F/campaign.monitoring.queue | jq '.messages_ready'
```

2. Verificar logs do worker:
```bash
tail -f logs/*.log | grep "Buscando item da fila"
```

3. Verificar se o worker está consumindo:
```bash
curl -u admin:admin123 http://localhost:15672/api/queues/%2F/campaign.monitoring.queue | jq '.consumers'
```

**Solução:**
1. Se `consumers` for 0, o worker não está conectado à fila
2. Verificar configuração do tenant no Consul (campo `queueNameMonitoring`)
3. Reiniciar o worker

#### Problema: Processamento falha com erro

**Diagnóstico:**
```bash
# Verificar logs de erro
tail -f logs/*.log | grep -i error

# Verificar status da campanha no MongoDB
mongosh "mongodb://admin:admin123@localhost:27017/campaign_monitoring"
> db.CampaignMonitoring.find({ idCampaign: "campaign-id" }).pretty()
```

**Solução:** Analisar o erro específico nos logs e aplicar a correção apropriada.

### Steps não são validados

#### Problema: IntegrationData está null

**Causa:** Dados não foram encontrados no banco do canal.

**Diagnóstico:**
```bash
# Verificar se os dados existem no banco do canal
mongosh "mongodb://admin:admin123@localhost:27017/effmail_db"
> db.Trigger.find({ "Parameters.WorkflowId": "workflow-id" }).pretty()
```

**Solução:**
1. Verificar se o workflowId está correto
2. Verificar se o banco de dados do canal está correto no Consul
3. Verificar se há dados para o workflowId específico

## Problemas de Performance

### Processamento lento

#### Problema: Worker demora muito para processar mensagens

**Diagnóstico:**
```bash
# Verificar tempo de processamento nos logs
grep "processada com sucesso" logs/*.log | tail -20

# Verificar latência do MongoDB
mongosh "mongodb://admin:admin123@localhost:27017"
> db.serverStatus().opcounters
```

**Solução:**
1. **MongoDB lento:**
   - Verificar índices: `db.CampaignMonitoring.getIndexes()`
   - Adicionar índices se necessário
   - Verificar explain de queries lentas

2. **Muitas execuções:**
   - Otimizar query de execuções no `CampaignReadModelService`
   - Adicionar paginação se necessário

3. **Agregações pesadas nos canais:**
   - Otimizar pipelines de agregação
   - Adicionar índices nas collections de Lead

### Alta utilização de memória

**Diagnóstico:**
```bash
# Monitorar uso de memória
docker stats campaign-watch-worker

# Verificar em produção
top -p $(pgrep -f CampaignWatchWorker)
```

**Solução:**
1. Ajustar limites de memória no Docker
2. Implementar processamento em lote se necessário
3. Adicionar garbage collection manual em casos específicos

## Problemas de Dados

### Dados inconsistentes

#### Problema: Execução marcada como completa mas steps não

**Diagnóstico:**
```bash
# Verificar no MongoDB
mongosh "mongodb://admin:admin123@localhost:27017/campaign_monitoring"
> db.ExecutionMonitoring.find({
    status: "Completed",
    "steps.status": { $ne: "Completed" }
  }).pretty()
```

**Solução:**
Isso gerará um alerta no validador `EndStepValidator`. Investigar a causa raiz no sistema de origem.

#### Problema: Campanha sem execuções

**Diagnóstico:**
```bash
# Verificar no banco de origem
mongosh "mongodb://admin:admin123@localhost:27017/campaign_db"
> db.ExecutionPlan.find({ 
    CampaignId: ObjectId("campaign-id"),
    FlagCount: false
  }).count()
```

**Solução:**
1. Verificar se a campanha realmente foi executada
2. Verificar campo `FlagCount` - pode estar como `true`
3. Verificar se `CampaignId` está correto

### Duplicação de dados

#### Problema: Mesma execução processada múltiplas vezes

**Diagnóstico:**
```bash
# Verificar duplicatas
mongosh "mongodb://admin:admin123@localhost:27017/campaign_monitoring"
> db.ExecutionMonitoring.aggregate([
    { $group: { 
        _id: { 
          campaignId: "$originalCampaignId", 
          executionId: "$originalExecutionId" 
        },
        count: { $sum: 1 }
    }},
    { $match: { count: { $gt: 1 } }}
  ])
```

**Solução:**
Isso não deveria acontecer devido ao índice único. Se acontecer:
1. Verificar se os índices estão criados corretamente
2. Remover duplicatas manualmente
3. Investigar logs para entender a causa

## Comandos Úteis

### Verificar saúde dos serviços
```bash
make check-health
```

### Reiniciar ambiente de desenvolvimento
```bash
make docker-down
make docker-clean
make dev
```

### Limpar todos os dados
```bash
# MongoDB
mongosh "mongodb://admin:admin123@localhost:27017"
> use campaign_monitoring
> db.CampaignMonitoring.deleteMany({})
> db.ExecutionMonitoring.deleteMany({})

# RabbitMQ
curl -u admin:admin123 -X DELETE http://localhost:15672/api/queues/%2F/campaign.monitoring.queue/contents
```

### Forçar reprocessamento
```bash
# Enviar ID da campanha para a fila
make send-test-message TENANT_ID=your-campaign-id
```

### Monitorar logs em tempo real
```bash
# Todos os logs
make watch-logs

# Apenas erros
tail -f logs/*.log | grep -i error

# Apenas processamento
tail -f logs/*.log | grep "Campanha.*processada"
```

### Debug de queries MongoDB
```bash
# Habilitar profiling
mongosh "mongodb://admin:admin123@localhost:27017/campaign_db"
> db.setProfilingLevel(2)

# Ver queries lentas
> db.system.profile.find().sort({ts:-1}).limit(10).pretty()
```

## Logs Importantes

### Identificando problemas nos logs

**Inicialização bem-sucedida:**
```
Iniciando processamento para a Campanha ID: xxx
Campanha com ID xxx não encontrada -> OK se não existir
Campanha 'Nome' processada com sucesso
```

**Erro de integração:**
```
ERRO: Etapa do tipo Canal, mas não foi possível identificar o canal específico
ERRO FATAL ao processar a mensagem
```

**Timeout de filtro:**
```
ALERTA: Etapa de filtro está em execução há mais de 1 hora(s)
```

**Alta taxa de erro em canal:**
```
Alta taxa de erro no envio: XX% (YY/ZZ leads)
```

## Contato para Suporte

Se o problema persistir após seguir este guia:

1. Colete os logs relevantes
2. Documente os passos para reproduzir o problema
3. Verifique se é um problema conhecido nas Issues do GitHub
4. Entre em contato com a equipe de suporte

**Informações úteis para incluir no report:**
- Versão do worker
- Ambiente (Development/Staging/Production)
- Logs completos do erro
- Configuração do tenant (sem credenciais)
- Prints de tela se aplicável