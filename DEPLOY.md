# CardLedger — Deploy no ZimaOS via Docker

**Data:** Abril 2026  
**Projeto:** CardLedger Web API (.NET 10 + SQLite)  
**Repositório:** https://github.com/AndreRicarti/CardLedger

---

## 1. Visão Geral da Arquitetura

```
ZimaOS ~/Documents/
├── invoice-api/          ← API .NET 10 (clonada do GitHub)
│   └── CardLedger/
│       ├── Dockerfile
│       ├── CardLedger.csproj
│       └── ...
├── invoice-dashboard/    ← Frontend React (já existia)
└── docker-compose.yml    ← Orquestra os dois containers
```

**Portas expostas no ZimaOS:**
| Serviço  | Porta Externa | Porta Interna |
|----------|--------------|---------------|
| API .NET | 7086         | 8080          |
| Frontend | 3001         | 80            |

---

## 2. Arquivos Criados/Modificados

### `CardLedger/Dockerfile`
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CardLedger.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

RUN mkdir -p /app/data

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "CardLedger.dll"]
```

---

### `docker-compose.yml` (raiz do repositório)
```yaml
version: "3"
services:
  api:
    build: ./invoice-api/CardLedger
    ports:
      - "7086:8080"
    volumes:
      - api-data:/app/data
    restart: unless-stopped

  frontend:
    build: ./invoice-dashboard
    ports:
      - "3001:80"
    depends_on:
      - api
    restart: unless-stopped

volumes:
  api-data:
```

---

### `Program.cs` — Ajustes para Docker

| Mudança | Motivo |
|---------|--------|
| SQLite → `data/invoices.db` | Aponta para o volume Docker montado em `/app/data` |
| `UseHttpsRedirection` removido | Container usa HTTP puro, TLS fica no Nginx |
| CORS removido | Nginx proxy reverso garante mesma origem |
| Swagger em todos os ambientes | Facilita debug no ZimaOS |

Trecho relevante do `Program.cs`:
```csharp
// SQLite aponta para o volume montado em /app/data
var dbPath = Path.Combine("data", "invoices.db");
builder.Services.AddDbContext<InvoiceDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));
```

---

## 3. Comandos Executados no ZimaOS

### Instalação do docker-compose
```sh
# Baixar binário para /tmp (único diretório com escrita)
curl -L "https://github.com/docker/compose/releases/download/v2.24.6/docker-compose-$(uname -s)-$(uname -m)" -o /tmp/docker-compose
chmod +x /tmp/docker-compose
/tmp/docker-compose --version
```

### Clonar o repositório
```sh
cd ~/Documents
git clone https://github.com/AndreRicarti/CardLedger invoice-api
```

### Subir os containers
```sh
cd ~/Documents
sudo DOCKER_CONFIG=/tmp /tmp/docker-compose up -d
```
> `DOCKER_CONFIG=/tmp` necessário pois `/root/.docker` é read-only no ZimaOS.

### Atualizar após mudanças no GitHub
```sh
cd ~/Documents
git -C invoice-api pull
curl -o docker-compose.yml https://raw.githubusercontent.com/AndreRicarti/CardLedger/master/docker-compose.yml
sudo DOCKER_CONFIG=/tmp /tmp/docker-compose up -d --build
```

---

## 4. Limitações do ZimaOS Encontradas

| Problema | Causa | Solução |
|----------|-------|---------|
| `apt-get` não encontrado | ZimaOS não usa Debian/Ubuntu | Instalação manual via `curl` |
| `/usr/local/bin` não existe | Sistema de arquivos customizado | Usar `/tmp` |
| `/usr/bin` read-only | Sistema de arquivos protegido | Usar `/tmp` |
| `~` aponta para `/DATA` sem permissão | Diretório home restrito | Usar `/tmp` |
| `/root/.docker` read-only | Sistema de arquivos protegido | `DOCKER_CONFIG=/tmp` |
| Porta `3000` já ocupada | Outro serviço do ZimaOS | Trocar para porta `3001` |

---

## 5. Acesso aos Serviços

Descubra o IP do ZimaOS:
```sh
ip addr | grep "inet " | grep -v 127.0.0.1
```

| Serviço | URL |
|---------|-----|
| Swagger (API) | `http://IP-DO-ZIMA:7086` |
| Frontend | `http://IP-DO-ZIMA:3001` |

---

## 6. Banco de Dados SQLite

O banco fica persistido no volume Docker `documents_api-data`, mapeado para `/app/data/invoices.db` dentro do container. Os dados **sobrevivem** a restarts e rebuilds do container.

Para inspecionar o banco diretamente:
```sh
sudo docker exec -it documents-api-1 sh
ls /app/data/
```
