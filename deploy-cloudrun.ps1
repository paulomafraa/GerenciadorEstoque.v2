$PROJECT_ID = "SEU-PROJETO-GCP"
$SERVICE_NAME = "gerenciador-estoque"
$REGION = "us-central1"

$INSTANCE_CONNECTION = "SEU-PROJETO:REGIAO:NOME-DA-INSTANCIA"
$DB_USER = "USUARIO-DO-BANCO"
$DB_NAME = "estoque"

$DOCKERFILE = "Dockerfile"

if (-not (Get-Command gcloud -ErrorAction SilentlyContinue)) {
    Write-Host "ERRO: 'gcloud' nao foi encontrado no PATH do PowerShell." -ForegroundColor Red
    Write-Host "Solucoes:" -ForegroundColor Yellow
    Write-Host "  1) Feche e reabra o PowerShell apos instalar o Cloud SDK." -ForegroundColor Yellow
    Write-Host "  2) Verifique em cmd.exe: execute 'gcloud --version'." -ForegroundColor Yellow
    Write-Host "  3) Adicione manualmente ao PATH: `$env:Path += ';C:\Users\<seu-usuario>\AppData\Local\Google\Cloud SDK\google-cloud-sdk\bin'" -ForegroundColor Yellow
    exit 1
}

Write-Host "Iniciando deploy para Google Cloud Run..." -ForegroundColor Cyan
Write-Host "Usando: $DOCKERFILE" -ForegroundColor Yellow

Write-Host "Fazendo login no Google Cloud..." -ForegroundColor Yellow
gcloud auth login

Write-Host "Configurando projeto: $PROJECT_ID" -ForegroundColor Yellow
gcloud config set project $PROJECT_ID

Write-Host "Habilitando APIs necessarias..." -ForegroundColor Yellow
gcloud services enable run.googleapis.com
gcloud services enable cloudbuild.googleapis.com
gcloud services enable artifactregistry.googleapis.com
gcloud services enable sqladmin.googleapis.com

Write-Host "Fazendo build da imagem com Cloud Build..." -ForegroundColor Yellow
gcloud builds submit --tag gcr.io/$PROJECT_ID/$SERVICE_NAME:latest --project=$PROJECT_ID --dockerfile $DOCKERFILE .

Write-Host "Deployando para o Cloud Run..." -ForegroundColor Yellow
$DB_CONNECTION = "server=/cloudsql/$INSTANCE_CONNECTION;user=$DB_USER;database=$DB_NAME"
gcloud run deploy $SERVICE_NAME `
    --image gcr.io/$PROJECT_ID/${SERVICE_NAME}:latest `
    --platform managed `
    --region $REGION `
    --allow-unauthenticated `
    --project $PROJECT_ID `
    --add-cloudsql-instances $INSTANCE_CONNECTION `
    --set-env-vars "DB_CONNECTION=$DB_CONNECTION" `
    --set-secrets "DB_PASSWORD=db-password:latest"

Write-Host "Deploy concluido!" -ForegroundColor Green
Write-Host "URL da aplicacao:" -ForegroundColor Cyan
gcloud run services describe $SERVICE_NAME --region $REGION --format="value(status.url)"

Write-Host "BANCO DE DADOS:" -ForegroundColor Green
Write-Host "- Instancia: $INSTANCE_CONNECTION" -ForegroundColor Yellow
Write-Host "- Database: $DB_NAME" -ForegroundColor Yellow
