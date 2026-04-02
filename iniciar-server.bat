@echo off
title Gerenciador de Estoque - Servidor

echo ============================================
echo   Gerenciador de Estoque - Iniciando...
echo ============================================
echo.

:: Limpa cache de builds anteriores
echo [1/4] Limpando cache...
dotnet clean GerenciadorEstoque/GerenciadorEstoque.csproj -c Release --nologo -q 2>nul
rd /s /q publicado 2>nul

:: Publica o app
echo [2/4] Publicando aplicacao...
dotnet publish GerenciadorEstoque/GerenciadorEstoque.csproj -c Release -o ./publicado --nologo
echo.

:: Verifica se o exe foi gerado
if not exist "publicado\GerenciadorEstoque.exe" (
    echo ERRO: Arquivo GerenciadorEstoque.exe nao encontrado.
    pause
    exit /b 1
)

echo [3/4] Iniciando servidor na porta 5199...
cd publicado
start "" /B GerenciadorEstoque.exe --urls "http://0.0.0.0:5199"
cd ..

:: Aguarda o servidor iniciar
echo Aguardando servidor iniciar...
timeout /t 5 /nobreak >nul

echo [4/4] Iniciando ngrok...
echo.
echo ============================================
echo   Copie a URL "Forwarding" abaixo para
echo   acessar do celular de qualquer lugar.
echo.
echo   Pressione Ctrl+C para encerrar tudo.
echo ============================================
echo.

ngrok http 5199