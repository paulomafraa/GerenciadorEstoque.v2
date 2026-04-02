# 🚀 Deploy para Google Cloud Run - Guia Completo

## 📋 Pré-requisitos

1. **Instalar o Google Cloud SDK**
   - Baixe em: https://cloud.google.com/sdk/docs/install
   - Após instalar, reinicie o terminal

2. **Criar uma conta GCP**
   - Acesse: https://console.cloud.google.com
   - Crie um novo projeto

3. **Docker Desktop** (opcional, para testar localmente)
   - Baixe em: https://www.docker.com/products/docker-desktop

---

## 🎯 Passo a Passo

### **1. Configurar o script de deploy**

Abra o arquivo `deploy-cloudrun.ps1` e altere:

```powershell
$PROJECT_ID = "seu-projeto-gcp"      # ← ID do seu projeto GCP
$SERVICE_NAME = "gerenciador-estoque" # ← Nome do serviço
$REGION = "us-central1"               # ← Região (ou "southamerica-east1" para São Paulo)
```

### **2. Executar o deploy**

No PowerShell, navegue até a pasta do projeto e execute:

```powershell
cd "Controle Estoque V2\GerenciadorEstoque"
.\deploy-cloudrun.ps1
```

### **3. Aguardar o build**

O processo leva de 3 a 5 minutos na primeira vez. Você verá:
- ✅ Build da imagem Docker
- ✅ Upload para Google Artifact Registry
- ✅ Deploy no Cloud Run

---

## ⚠️ **AVISO IMPORTANTE: Persistência de Dados**

### ❌ **Problema Atual**
Seu banco SQLite está em `/tmp` (memória volátil). Isso significa:
- ✅ Funciona para testes iniciais
- ❌ **Dados são perdidos quando o container reinicia**
- ❌ Não é adequado para produção

### ✅ **Soluções Recomendadas**

#### **Opção 1: Google Cloud SQL (PostgreSQL/MySQL)** 
**Melhor para:** Aplicações sérias com muitos dados

```bash
# Criar instância Cloud SQL
gcloud sql instances create gerenciador-db \
    --database-version=POSTGRES_15 \
    --tier=db-f1-micro \
    --region=us-central1

# Conectar no Cloud Run
gcloud run services update gerenciador-estoque \
    --add-cloudsql-instances=PROJECT_ID:us-central1:gerenciador-db
```

**Custo:** ~$7-10/mês (instância pequena)

---

#### **Opção 2: Firestore (NoSQL)**
**Melhor para:** Aplicações pequenas/médias com dados simples

- Free tier: 1 GB de armazenamento
- Fácil de integrar
- Escalável automaticamente

---

#### **Opção 3: Supabase (PostgreSQL gerenciado)**
**Melhor para:** Desenvolvimento rápido

- Free tier generoso
- Suporte a SQLite-like (PostgreSQL)
- API REST automática

Acesse: https://supabase.com

---

#### **Opção 4: Cloud Storage + SQLite**
**Melhor para:** Poucos usuários, baixo tráfego

Monte um volume persistente:

```bash
gcloud run services update gerenciador-estoque \
    --execution-environment=gen2 \
    --add-volume=name=sqlite-data,type=cloud-storage,bucket=gerenciador-estoque-bucket \
    --add-volume-mount=volume=sqlite-data,mount-path=/data
```

**Limitação:** SQLite não funciona bem com múltiplas instâncias simultâneas.

---

## 🌐 Testar Localmente com Docker (Opcional)

### Build local:
```powershell
docker build -t gerenciador-estoque .
```

### Rodar local:
```powershell
docker run -p 8080:8080 gerenciador-estoque
```

Acesse: http://localhost:8080

---

## 🔧 Comandos Úteis

### Ver logs:
```bash
gcloud run services logs read gerenciador-estoque --region=us-central1
```

### Atualizar configurações:
```bash
gcloud run services update gerenciador-estoque \
    --memory=1Gi \
    --region=us-central1
```

### Deletar serviço:
```bash
gcloud run services delete gerenciador-estoque --region=us-central1
```

---

## 💰 Custos Estimados

| Recurso | Free Tier | Custo após Free Tier |
|---------|-----------|---------------------|
| **Cloud Run** | 2 milhões de requisições/mês | ~$0.40 por milhão |
| **Cloud SQL** | ❌ Não tem | ~$7-10/mês |
| **Firestore** | 1 GB storage | ~$0.18/GB/mês |
| **Cloud Storage** | 5 GB | ~$0.02/GB/mês |

**Estimativa para app pequeno (Cloud Run + Firestore):** **$0-5/mês**

---

## 📞 Suporte

Problemas comuns:

### Erro: "Permission denied"
```bash
gcloud auth login
gcloud auth application-default login
```

### Erro: "Service not found"
Verifique se o nome do serviço está correto e se a região está ativa.

### Container não inicia
```bash
# Ver logs detalhados
gcloud run services logs read gerenciador-estoque --limit=50
```

---

## 🎉 Pronto!

Depois do deploy, você terá:
- ✅ URL pública (ex: `https://gerenciador-estoque-xxxxx.run.app`)
- ✅ SSL/HTTPS automático
- ✅ Escalamento automático
- ⚠️ Dados voláteis (lembre-se de configurar banco persistente!)
