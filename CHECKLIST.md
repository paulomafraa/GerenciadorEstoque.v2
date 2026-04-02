# ✅ Checklist Final - Deploy Google Cloud Run

## 📋 Arquivos Criados/Modificados

- ✅ **Dockerfile** (usa .NET 8.0 - RECOMENDADO)
- ✅ **Dockerfile.net9** (usa .NET 9.0 - se necessário)
- ✅ **.dockerignore** (otimiza build)
- ✅ **Program.cs** (detecta Cloud Run automaticamente)
- ✅ **deploy-cloudrun.ps1** (script automatizado)
- ✅ **DEPLOY_GUIDE.md** (guia completo)
- ✅ **README_DEPLOY.md** (guia rápido)

---

## ⚙️ Configurações Importantes

### ✅ **1. Detecção Automática de Ambiente**
```csharp
var isCloudRun = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("K_SERVICE"));
```
- Detecta automaticamente se está no Cloud Run
- Local: usa `LocalApplicationData`
- Cloud Run: usa `/tmp` (volátil)

### ✅ **2. Porta Configurada Corretamente**
```csharp
var port = Environment.GetEnvironmentVariable("PORT") ?? "5199";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
```
- Cloud Run injeta a variável `PORT`
- Localmente usa porta 5199

### ✅ **3. Banco de Dados**
- ⚠️ **SQLite em `/tmp` é VOLÁTIL**
- Dados são perdidos ao reiniciar
- Veja `DEPLOY_GUIDE.md` para soluções persistentes

---

## 🚀 Próximos Passos

### **1. Testar Localmente (Opcional)**
```powershell
cd "Controle Estoque V2\GerenciadorEstoque"
docker build -t gerenciador-estoque .
docker run -p 8080:8080 gerenciador-estoque
# Acesse: http://localhost:8080
```

### **2. Configurar Script de Deploy**
Edite `deploy-cloudrun.ps1`:
```powershell
$PROJECT_ID = "seu-projeto-gcp"      # ← Mude aqui
$SERVICE_NAME = "gerenciador-estoque"
$REGION = "us-central1"
$DOCKERFILE = "Dockerfile"           # ← Use "Dockerfile.net9" se precisar .NET 9
```

### **3. Instalar Google Cloud SDK**
https://cloud.google.com/sdk/docs/install

### **4. Fazer Deploy**
```powershell
cd "Controle Estoque V2\GerenciadorEstoque"
.\deploy-cloudrun.ps1
```

---

## ⚠️ AVISOS CRÍTICOS

### ❌ **Problema: Dados Voláteis**
```
[Cloud Run] Usando pasta temporária: /tmp/GerenciadorEstoque
⚠️ ATENÇÃO: Dados serão perdidos ao reiniciar o container!
💡 Configure Google Cloud Storage ou Cloud SQL para persistência.
```

**Isso é normal!** É para testes iniciais.

### ✅ **Solução: Migrar para Banco Persistente**

Escolha uma opção:

1. **Cloud SQL** (PostgreSQL/MySQL)
   - Custo: ~$7-10/mês
   - Melhor para produção
   
2. **Firestore** (NoSQL)
   - Free tier: 1 GB
   - Simples de usar
   
3. **Supabase** (PostgreSQL)
   - Free tier generoso
   - Fácil integração

Veja instruções completas em: **`DEPLOY_GUIDE.md`**

---

## 🐛 Problemas Comuns

### ❌ Erro: "Permission denied"
```bash
gcloud auth login
gcloud auth application-default login
```

### ❌ Erro: "Cannot find .NET SDK"
- Certifique-se que o Dockerfile correto está sendo usado
- Use `Dockerfile` (NET 8) ao invés de `Dockerfile.net9`

### ❌ Container não inicia
```bash
# Ver logs
gcloud run services logs read gerenciador-estoque --limit=50
```

---

## 📊 Estimativa de Custos

| Item | Free Tier | Após Free Tier |
|------|-----------|----------------|
| Cloud Run | 2M requisições/mês | $0.40/milhão |
| Tráfego | 2 GB/mês | $0.12/GB |
| **Total Estimado** | **$0-3/mês** | **$3-10/mês** |

---

## ✅ Está Tudo Pronto!

Você pode fazer o deploy agora. Basta:
1. Editar `deploy-cloudrun.ps1`
2. Executar o script
3. Aguardar 3-5 minutos

**Boa sorte! 🚀**
