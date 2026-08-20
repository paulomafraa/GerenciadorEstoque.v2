# GerenciadorEstoque

Sistema web de gestão de estoque para uso interno de loja.  
Portfólio com Blazor WebAssembly, ASP.NET Core e deploy no Google Cloud.

![Status](https://img.shields.io/badge/status-portfolio-0f3d36?style=flat-square)
![.NET](https://img.shields.io/badge/.NET-9-512BD4?style=flat-square)
![Blazor](https://img.shields.io/badge/Blazor-WASM-512BD4?style=flat-square)
![MySQL](https://img.shields.io/badge/MySQL-8-4479A1?style=flat-square)

> Aplicação de uso interno, sem acesso público. O repositório existe para portfólio e demonstração de código.

## Sobre

App full-stack para cadastro de produtos, controle de estoque, vendas, relatórios e usuários.  
Roda em container no Cloud Run, com MySQL no Cloud SQL e segredos no Secret Manager.

Versão web do fluxo que começou no desktop [ControleEstoque](https://github.com/paulomafraa/ControleEstoque).

## Stack

| Camada | Tecnologia |
| --- | --- |
| Frontend | Blazor WebAssembly (.NET 9) + MudBlazor |
| Backend | ASP.NET Core 9 Web API |
| Banco | MySQL 8 (Pomelo EF Core) |
| Deploy | Google Cloud Run + Docker |
| Banco cloud | Google Cloud SQL |
| Segredos | Google Secret Manager |
| Auth | Cookie Authentication |
| Build | Google Cloud Build |

## Funcionalidades

- Login com CAPTCHA matemático
- Cadastro de produtos com imagem (BLOB no MySQL)
- Controle de entradas e saídas de estoque
- Registro e acompanhamento de vendas
- Relatórios gerais do negócio
- Gerenciamento de usuários
- Backup automático em JSON ao iniciar
- Modo escuro

## Arquitetura

```
GerenciadorEstoque/                 Projeto servidor (ASP.NET Core)
├── Controllers/                    APIs REST ([Authorize])
├── Data/AppDbContext.cs            EF Core + MySQL
├── Components/App.razor            Entry point Blazor
└── Program.cs                      Middleware, auth, migrations

GerenciadorEstoque.Client/          Cliente Blazor WASM
├── Pages/                          Páginas do SPA
├── Layout/MainLayout.razor         Layout com verificação de auth
└── Layout/LoginLayout.razor        Layout da tela de login
```

## Como rodar localmente

Requisitos: .NET 9 SDK e MySQL local (ou cloud-sql-proxy).

```powershell
cd "Controle Estoque V2/GerenciadorEstoque/GerenciadorEstoque"

$env:DB_CONNECTION = "server=127.0.0.1;port=3307;user=SEU_USER;database=estoque"
$env:DB_PASSWORD   = "SUA_SENHA"
$env:APP_USER      = "admin"
$env:APP_PASSWORD  = "suasenha"

dotnet run
# http://localhost:5199
```

## Deploy no Cloud Run

1. Conta no GCP, Cloud SDK, instância Cloud SQL MySQL e Secret Manager
2. Edite `deploy-cloudrun.ps1` com `PROJECT_ID`, instância e usuário do banco
3. Defina credenciais de login:

```powershell
gcloud run services update gerenciador-estoque `
    --region us-central1 `
    --update-env-vars "APP_USER=SEU_USUARIO,APP_PASSWORD=SUA_SENHA"
```

4. Execute o script de deploy

### Variáveis de ambiente

| Variável | Descrição | Tipo |
| --- | --- | --- |
| `DB_CONNECTION` | Connection string MySQL (sem senha) | Env var |
| `DB_PASSWORD` | Senha do banco | Secret Manager |
| `APP_USER` | Usuário do sistema | Env var |
| `APP_PASSWORD` | Senha do sistema | Env var |

## Segurança

- Sem credenciais no código (tudo por env vars)
- Senhas no Secret Manager
- Cookies HttpOnly com SlidingExpiration de 12h
- Middleware redireciona para `/login` antes do prerender
- `[Authorize]` nos controllers de dados

## Autor

[Paulo Mafra Watanabe](https://github.com/paulomafraa) · [LinkedIn](https://www.linkedin.com/in/paulo-watanabe/)
