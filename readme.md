# 🚀 TMB Orders – Sistema de Gerenciamento de Pedidos

> **Sistema completo de pedidos com Frontend React, API .NET, PostgreSQL, Azure Service Bus e IA (Groq).**


<img width="1678" height="890" alt="image" src="https://github.com/user-attachments/assets/a009b64e-b054-4cbb-8a6a-8dfc92342163" />


---

## 📌 Sumário
- [Visão Geral](#-visão-geral)
- [Arquitetura Geral](#-arquitetura-geral)
- [Tecnologias](#-tecnologias)
- [Fluxo Completo](#-fluxo-completo)
- [Endpoints da API](#-endpoints-da-api)
- [Banco de Dados](#-banco-de-dados)
- [Mensageria (Service Bus)](#-mensageria-service-bus)
- [Worker](#-worker)
- [IA (Ask Orders)](#-ia-ask-orders)
- [Variáveis de Ambiente](#-variáveis-de-ambiente)
- [Como Rodar](#-como-rodar)
- [Roadmap](#-roadmap)

---

# 🎯 Visão Geral

O **TMB Orders** é um sistema moderno que integra:

- **Frontend React SPA**
- **API .NET 8**
- **Banco PostgreSQL**
- **Fila Azure Service Bus**
- **Worker para processamento assíncrono**
- **IA Groq para análise dos pedidos**

---

# 🏗 Arquitetura Geral

```
[Usuário]
    |
    v
[Frontend - React]
  • Formulário de novo pedido
  • Lista de pedidos
  • Caixa de perguntas IA
    |
    v
[API .NET - Tmb.Orders.Api]
  ├── OrdersController
  ├── AskOrdersController
  ├── OrderCreatedPublisher
    |
    v
[Azure Service Bus Queue]
    |
    v
[Worker - Tmb.Orders.Worker]
  • Processa mensagens
  • Atualiza pedidos
  • Idempotência
    |
    v
[PostgreSQL]
```

**DIAGRAMA DA ARQUITETURA**
<img width="1348" height="590" alt="image" src="https://github.com/user-attachments/assets/14cb5825-4c68-41d3-94a2-a1b358e991a8" />



---

# 🛠 Tecnologias

| Camada | Tecnologia |
|--------|------------|
| **Frontend** | React + Vite + TypeScript |
| **API** | .NET 8 WebAPI |
| **Banco** | PostgreSQL |
| **Mensageria** | Azure Service Bus |
| **Worker** | BackgroundService (.NET) |
| **IA** | Groq |
| **Infra** | Docker & Docker Compose |

---

# 🔄 Fluxo Completo

### 📌 1. Criar Pedido
1. Usuário preenche e envia o formulário.
2. API salva pedido como **Pending**.
3. API envia mensagem para a fila.
4. Worker recebe a mensagem, simula processamento e finaliza o pedido.
5. Frontend atualiza automaticamente até status = **Finalizado**.

### 🤖 2. Perguntas de IA
1. Usuário envia pergunta.
2. API lê métricas do banco.
3. API envia JSON com métricas para a IA Groq.
4. IA responde em linguagem natural.

---

# 🔧 Endpoints da API

## 📍 OrdersController

### `GET /api/orders`
Lista todos os pedidos.

### `GET /api/orders/{id}`
Retorna um pedido específico.

### `POST /api/orders`
Cria pedido e envia mensagem na fila.

---

## 📍 AskOrdersController

### `POST /api/askorders`
Envia métricas para IA e retorna resposta.

---

# 🗄 Banco de Dados (PostgreSQL)

## Tabela: **Orders**
| Campo | Tipo | Descrição |
|-------|-------|-----------|
| Id | guid | Identificador |
| Cliente | text | Nome do cliente |
| Produto | text | Nome do produto |
| Valor | numeric | Valor do pedido |
| Status | int | Pending / Processing / Finalized |
| DataCriacao | timestamp | Data de criação |
| LastProcessedMessageId | text | Idempotência |

## Tabela: **OrderStatusHistories**
| Campo | Tipo |
|--------|------|
| Id | guid |
| OrderId | guid |
| Status | int |
| ChangedAt | timestamp |

---

# 📬 Mensageria (Azure Service Bus)

Cada pedido novo gera uma mensagem com:

```
{
  "OrderId": "GUID"
}
```

### Propriedades:

| Propriedade | Valor |
|-------------|-------|
| **CorrelationId** | OrderId |
| **EventType** | "OrderCreated" |

---

# ⚙ Worker – Processamento Assíncrono

O Worker:

- lê mensagens da fila  
- verifica idempotência  
- atualiza pedido → Finalizado  
- adiciona histórico  
- completa mensagem  

Simula processamento:

```
await Task.Delay(TimeSpan.FromSeconds(5));
```

---

# 🤖 IA – Ask Orders

A API monta JSON com métricas e envia para a IA, que responde em português em linguagem natural.

---

# 🔐 Variáveis de Ambiente

### API
```
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__DefaultConnection=...
ServiceBus__ConnectionString=...
ServiceBus__QueueName=orders
GROQ__ApiKey=...
GROQ__Model=llama3-8b
FrontendUrl=http://localhost:3000
```

### Worker
```
ConnectionStrings__DefaultConnection=...
ServiceBus__ConnectionString=...
ServiceBus__QueueName=orders
```

### Frontend
```
VITE_API_URL=http://localhost:8080
```

---

# ▶ Como Rodar

### 🚀 Subir tudo com Docker
```
docker compose up --build
```

---

# 👨‍💻 Desenvolvido por
**Henry Rufino**

