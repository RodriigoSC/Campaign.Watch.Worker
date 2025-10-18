#!/bin/bash

# Script para configurar o Vault para desenvolvimento
# Uso: ./scripts/setup-vault.sh [environment]

ENVIRONMENT=${1:-development}
VAULT_ADDR=${VAULT_ADDR:-http://localhost:8200}
VAULT_TOKEN=${VAULT_TOKEN:-root}

echo "=========================================="
echo "Configurando Vault para ambiente: $ENVIRONMENT"
echo "Vault Address: $VAULT_ADDR"
echo "=========================================="

# Exportar variáveis de ambiente
export VAULT_ADDR
export VAULT_TOKEN

# Habilitar engine KV v2
echo "Habilitando KV secrets engine..."
vault secrets enable -path=monitoring kv-v2 2>/dev/null || echo "KV engine já existe"

# Função para criar secrets
create_secret() {
    local path=$1
    shift
    echo "Criando secret: $path"
    vault kv put "$path" "$@"
}

# Criar secrets para MongoDB
echo ""
echo "Configurando MongoDB..."
create_secret "monitoring/$ENVIRONMENT/data/keys" \
    MongoDB.Campaign.host="localhost:27017" \
    MongoDB.Campaign.user="admin" \
    MongoDB.Campaign.pass="admin123"

create_secret "monitoring/$ENVIRONMENT/data/keys" \
    MongoDB.Effmail.host="localhost:27017" \
    MongoDB.Effmail.user="admin" \
    MongoDB.Effmail.pass="admin123"

create_secret "monitoring/$ENVIRONMENT/data/keys" \
    MongoDB.Effsms.host="localhost:27017" \
    MongoDB.Effsms.user="admin" \
    MongoDB.Effsms.pass="admin123"

create_secret "monitoring/$ENVIRONMENT/data/keys" \
    MongoDB.Effpush.host="localhost:27017" \
    MongoDB.Effpush.user="admin" \
    MongoDB.Effpush.pass="admin123"

create_secret "monitoring/$ENVIRONMENT/data/keys" \
    MongoDB.Effwhatsapp.host="localhost:27017" \
    MongoDB.Effwhatsapp.user="admin" \
    MongoDB.Effwhatsapp.pass="admin123"

create_secret "monitoring/$ENVIRONMENT/data/keys" \
    MongoDB.Persistence.host="localhost:27017" \
    MongoDB.Persistence.user="admin" \
    MongoDB.Persistence.pass="admin123" \
    MongoDB.Persistence.database="campaign_monitoring"

# Criar secrets para RabbitMQ
echo ""
echo "Configurando RabbitMQ..."
create_secret "monitoring/$ENVIRONMENT/data/keys" \
    RabbitMQ.host="localhost:5672" \
    RabbitMQ.user="admin" \
    RabbitMQ.pass="admin123" \
    RabbitMQ.virtualhost="/"

# Criar secrets para Consul
echo ""
echo "Configurando Consul..."
create_secret "monitoring/$ENVIRONMENT/data/keys" \
    Consul="http://localhost:8500" \
    Consul.token=""

echo ""
echo "=========================================="
echo "Vault configurado com sucesso!"
echo "=========================================="
echo ""
echo "Para usar o Vault, configure as variáveis de ambiente:"
echo "export VAULT_ADDR=$VAULT_ADDR"
echo "export VAULT_TOKEN=$VAULT_TOKEN"
echo ""
echo "Para listar os secrets criados:"
echo "vault kv list monitoring/$ENVIRONMENT/data"
echo ""
echo "Para ver um secret específico:"
echo "vault kv get monitoring/$ENVIRONMENT/data/keys"