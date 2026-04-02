# ⚡ Deploy Rápido - 3 Passos

## 1️⃣ Configurar
Abra `deploy-cloudrun.ps1` e altere:
```powershell
$PROJECT_ID = "seu-projeto-gcp"  # ← Seu projeto do Google Cloud
```

## 2️⃣ Instalar Google Cloud SDK
https://cloud.google.com/sdk/docs/install

## 3️⃣ Executar
```powershell
cd "Controle Estoque V2\GerenciadorEstoque"
.\deploy-cloudrun.ps1
```

## ⚠️ IMPORTANTE
- O banco SQLite atual é **VOLÁTIL** (perde dados ao reiniciar)
- Veja `DEPLOY_GUIDE.md` para configurar banco persistente
- Ideal: migrar para Cloud SQL, Firestore ou Supabase

## 📖 Documentação Completa
Leia `DEPLOY_GUIDE.md` para instruções detalhadas.
