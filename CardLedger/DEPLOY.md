# CardLedger — CI/CD e Deploy no ZimaOS

**Data:** Abril 2026  
**Projeto:** CardLedger Web API (.NET 10 + SQLite)  
**Repositório:** https://github.com/AndreRicarti/CardLedger

---

## 1. Visão Geral da Arquitetura Atual

```text
GitHub (master)
  ├─ CI (build + testes)
  └─ CD (build/push imagem para GHCR)

ZimaOS ~/Documents/
├── docker-compose.yml                ← sobe API + Frontend + Watchtower
├── invoice-dashboard/                ← frontend local (outro projeto)
└── (API vem por imagem do GHCR)
```

**Portas expostas no ZimaOS:**

| Serviço | Porta Externa | Porta Interna |
|---|---:|---:|
| API .NET | 7086 | 8080 |
| Frontend | 3001 | 80 |

---

## 2. GitHub Actions (CI/CD)

### CI — `.github/workflows/ci.yml`
Executa em `push` e `pull_request` na `master`:
- `dotnet restore`
- `dotnet build -c Release`
- `dotnet test -c Release`

### CD — `.github/workflows/cd.yml`
Executa após CI com sucesso (`workflow_run`) na `master`:
- login no GHCR
- build da imagem via `CardLedger/Dockerfile`
- push para: `ghcr.io/andrericarti/cardledger-api:latest`

---

## 3. Arquivos Criados/Modificados

### `.github/workflows/ci.yml`
Pipeline de validação (build e testes).

### `.github/workflows/cd.yml`
Pipeline de publicação da imagem Docker no GHCR.

### `docker-compose.yml` (raiz do repositório)
```yaml
version: "3"
services:
  api:
    image: ghcr.io/andrericarti/cardledger-api:latest
    ports:
      - "7086:8080"
    volumes:
      - api-data:/app/data
    restart: unless-stopped

  frontend:
    build:
      context: ./invoice-dashboard
      dockerfile: Dockerfile
    ports:
      - "3001:80"
    depends_on:
      - api
    restart: unless-stopped

  watchtower:
    image: containrrr/watchtower
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
    environment:
      - WATCHTOWER_POLL_INTERVAL=300
      - WATCHTOWER_CLEANUP=true
    restart: unless-stopped

volumes:
  api-data:
```

### `CardLedger/Dockerfile`
Imagem multi-stage (.NET 10 SDK + ASP.NET runtime), expondo a API em `8080` e persistindo SQLite em `/app/data`.

### `CardLedger/Program.cs` — ajustes para container
| Mudança | Motivo |
|---|---|
| SQLite → `data/invoices.db` | Persistência em volume Docker (`/app/data`) |
| `UseHttpsRedirection` removido | Container roda em HTTP; TLS fica fora da API |
| Swagger em todos os ambientes | Facilita validação no ZimaOS |
| `RoutePrefix = string.Empty` | Swagger UI disponível na raiz (`/`) |

---

## 4. Comandos Usados

### 4.1 GitHub (primeiro setup)
No ambiente local de desenvolvimento:

```powershell
cd C:\Users\andre\source\repos\CardLedger
git add .github/workflows/ci.yml .github/workflows/cd.yml docker-compose.yml
git commit -m "ci: add GitHub Actions workflows and Watchtower"
git push origin master
```

Após a primeira execução de CD, no GitHub:
- abrir pacote `cardledger-api`
- ajustar visibilidade para **Public** (quando necessário para pull sem autenticação no ZimaOS)

### 4.2 ZimaOS (bootstrap)
```sh
cd ~/Documents
curl -o docker-compose.yml https://raw.githubusercontent.com/AndreRicarti/CardLedger/master/docker-compose.yml
sudo DOCKER_CONFIG=/tmp docker compose pull api
sudo DOCKER_CONFIG=/tmp docker compose up -d
```

### 4.3 ZimaOS (validação)
```sh
sudo docker ps
sudo docker logs --tail 100 documents-api-1
sudo docker logs --tail 100 documents-watchtower-1
```

### 4.4 ZimaOS (fallback, se `docker compose` não existir)
```sh
curl -L "https://github.com/docker/compose/releases/download/v2.24.6/docker-compose-$(uname -s)-$(uname -m)" -o /tmp/docker-compose
chmod +x /tmp/docker-compose
sudo DOCKER_CONFIG=/tmp /tmp/docker-compose pull api
sudo DOCKER_CONFIG=/tmp /tmp/docker-compose up -d
```

---

## 5. Limitações Observadas no ZimaOS

| Problema | Causa | Solução |
|---|---|---|
| `apt-get` não encontrado | SO não baseado em Debian/Ubuntu | instalação manual quando necessário |
| caminhos de sistema read-only | sistema protegido | usar `/tmp` quando necessário |
| `/root/.docker` read-only | escrita restrita | `DOCKER_CONFIG=/tmp` |
| porta `3000` ocupada | conflito com outro serviço | frontend em `3001` |

---

## 6. Acesso aos Serviços

Descobrir IP do ZimaOS:
```sh
ip addr | grep "inet " | grep -v 127.0.0.1
```

| Serviço | URL |
|---|---|
| Swagger UI (API) | `http://IP-DO-ZIMA:7086/` |
| OpenAPI JSON | `http://IP-DO-ZIMA:7086/swagger/v1/swagger.json` |
| Frontend | `http://IP-DO-ZIMA:3001` |

---

## 7. Como Funciona o Auto Deploy

1. `git push` na `master`
2. GitHub Actions roda CI
3. CD publica nova imagem `latest` no GHCR
4. Watchtower (poll a cada 300s) detecta nova imagem
5. Watchtower recria `documents-api-1` automaticamente

Logs para acompanhar:
```sh
sudo docker logs -f documents-watchtower-1
```

---

## 8. Banco de Dados SQLite

O banco fica no volume Docker `documents_api-data`, em `/app/data/invoices.db` dentro do container. Os dados sobrevivem a restart e rebuild.

Inspeção:
```sh
sudo docker exec -it documents-api-1 sh
ls /app/data/
```
