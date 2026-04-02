# Dockerfile para .NET 9.0 (versão final/estável)

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Print dotnet info to logs to confirm runtime during Cloud Build
RUN dotnet --info

# Copy csproj files and restore
COPY ["GerenciadorEstoque/GerenciadorEstoque.csproj", "GerenciadorEstoque/"]
COPY ["GerenciadorEstoque.Client/GerenciadorEstoque.Client.csproj", "GerenciadorEstoque.Client/"]
RUN dotnet restore "GerenciadorEstoque/GerenciadorEstoque.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/GerenciadorEstoque"
RUN dotnet build "GerenciadorEstoque.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "GerenciadorEstoque.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Cloud Run injects PORT environment variable
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "GerenciadorEstoque.dll"]
