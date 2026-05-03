# CardLedger — CI/CD e Deploy no ZimaOS

**Data:** Maio 2026
**Projeto:** CardLedger Web API (.NET 10 + SQLite)
**Repositório:** https://github.com/AndreRicarti/CardLedger

---

## 1. Visão Geral da Arquitetura

```text
GitHub (master)
  ├─ CI (build + testes)
  └─ CD (build/push imagem para GHCR)

GHCR (Container Registry)
  ├─ cardledger-api:latest
  └─ invoice-dashboard:latest

ZimaOS (/DATA/Backup/projects/invoice)
  ├── docker-compose.yml
  ├── API (container)
  ├── Frontend (container)
  └── Watchtower (auto update)
```

---

## 2. Tecnologias Utilizadas

* .NET 10 (API)
* SQLite (persistência)
* React + Vite (frontend)
* Docker + Docker Compose
* GitHub Actions (CI/CD)
* GHCR (container registry)
* Watchtower (auto deploy)
* Portainer (monitoramento visual)

---

## 3. GitHub Actions (CI/CD)

### CI — `.github/workflows/ci.yml`

Executa em `push` e `pull_request` na `master`:

* `dotnet restore`
* `dotnet build -c Release`
* `dotnet test -c Release`

---

### CD — `.github/workflows/cd.yml`

Executa após CI com sucesso:

* login no GHCR
* build da imagem Docker
* push para:

```text
ghcr.io/andrericarti/cardledger-api:latest
```

---

## 4. Estrutura no ZimaOS

Os arquivos ficam em:

```text
/DATA/Backup/projects/invoice
```

> ⚠️ Não usar `~/Documents` ou `AppData` (podem ser apagados em updates)

---

## 5. docker-compose.yml

```yaml
services:
  api:
    image: ghcr.io/andrericarti/cardledger-api:latest
    container_name: cardledger-api
    ports:
      - "7086:8080"
    volumes:
      - api-data:/app/data
    restart: unless-stopped

  frontend:
    image: ghcr.io/andrericarti/invoice-dashboard:latest
    container_name: invoice-dashboard
    ports:
      - "3000:80"
    depends_on:
      - api
    restart: unless-stopped

  watchtower:
    image: containrrr/watchtower
    container_name: watchtower
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
    environment:
      - WATCHTOWER_POLL_INTERVAL=300
      - WATCHTOWER_CLEANUP=true
    restart: unless-stopped

volumes:
  api-data:
```

---

## 6. Deploy no ZimaOS

### Subir containers

```sh
cd /DATA/Backup/projects/invoice

export DOCKER_CONFIG=/tmp
sudo -E docker compose up -d
```

---

### Atualização manual

```sh
export DOCKER_CONFIG=/tmp
sudo -E docker compose pull
sudo -E docker compose up -d
```

---

## 7. Acesso aos Serviços

| Serviço       | URL                                            |
| ------------- | ---------------------------------------------- |
| Frontend      | http://IP-DO-ZIMA:3000                         |
| API (Swagger) | http://IP-DO-ZIMA:7086                         |
| OpenAPI       | http://IP-DO-ZIMA:7086/swagger/v1/swagger.json |

---

## 8. Auto Deploy (Watchtower)

Fluxo:

1. `git push` na master
2. GitHub Actions executa CI/CD
3. Nova imagem publicada no GHCR
4. Watchtower detecta atualização
5. Container é recriado automaticamente

---

### Logs do Watchtower

```sh
sudo docker logs -f invoice-watchtower-1
```

Exemplo:

```text
Found new ghcr.io/andrericarti/invoice-dashboard:latest
Stopping container
Creating container
```

---

## 9. Monitoramento com Portainer

Acesso:

```text
http://IP-DO-ZIMA:9000
```

Permite:

* visualizar containers
* acompanhar logs em tempo real
* reiniciar containers
* monitorar deploys

---

## 10. Banco de Dados (SQLite)

Local:

```text
/app/data/invoices.db
```

Persistido via volume Docker:

```text
api-data
```

---

### Inspeção:

```sh
sudo docker exec -it invoice-api-1 sh
ls /app/data
```

---

## 11. Problemas Comuns

| Problema                  | Causa            | Solução                   |
| ------------------------- | ---------------- | ------------------------- |
| `/root/.docker` read-only | ZimaOS protegido | usar `DOCKER_CONFIG=/tmp` |
| 404 `/api` no frontend    | falta de proxy   | usar URL da API direta    |
| frontend não atualiza     | cache navegador  | CTRL + F5                 |
| containers não sobem      | permissão pasta  | usar `chown`              |
| imagem não atualiza       | cache GHCR       | forçar pull               |

---

## 12. Observações Importantes

* Não é necessário código local no ZimaOS
* Deploy é feito via imagens Docker (GHCR)
* Estrutura é resiliente a perda de arquivos locais
* Watchtower garante atualização automática