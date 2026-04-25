# ◆ ShopX — Production E-Commerce Platform

> A full-stack, production-grade e-commerce platform built with **Angular 19** frontend and **.NET 8 microservices** backend. Modelled after real-world platforms like Amazon and Flipkart.

---

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Services](#services)
- [Getting Started](#getting-started)
- [Environment Variables](#environment-variables)
- [Database Setup](#database-setup)
- [Running with Docker](#running-with-docker)
- [Running Locally](#running-locally)
- [Service URLs](#service-urls)
- [Postman Testing](#postman-testing)
- [Default Credentials](#default-credentials)
- [Frontend Routes](#frontend-routes)
- [Message Flow](#message-flow)
- [Roadmap](#roadmap)
- [Security Notes](#security-notes)

---

## Overview

ShopX is a microservices-based e-commerce platform with:

- **Role-based access** — Customer, Seller, Admin
- **Product catalog** with full-text search via Elasticsearch
- **Shopping cart** backed by Redis
- **Order management** with full lifecycle tracking
- **Stripe payment** processing with webhook support
- **JWT authentication** with refresh token rotation
- **Event-driven messaging** via RabbitMQ + MassTransit
- **API Gateway** via YARP with rate limiting

---

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Angular 19 Frontend                   │
│         Signals · Standalone Components · SSR            │
└──────────────────────────┬──────────────────────────────┘
                           │ HTTPS
┌──────────────────────────▼──────────────────────────────┐
│              API Gateway (YARP) :5000                    │
│        Rate Limiting · JWT Validation · Routing          │
└──┬──────────┬──────────┬──────────┬─────────────────────┘
   │          │          │          │          │
┌──▼──┐  ┌───▼──┐  ┌────▼─┐  ┌────▼─┐  ┌────▼──────┐
│Auth │  │Prod  │  │Order │  │Cart  │  │Payment    │
│:5001│  │:5002 │  │:5003 │  │:5004 │  │:5005      │
│     │  │      │  │      │  │      │  │           │
│SQL  │  │SQL + │  │SQL   │  │Redis │  │SQL +      │
│Svr  │  │Elstc │  │Server│  │      │  │Stripe     │
└─────┘  └──────┘  └──┬───┘  └──────┘  └─────┬─────┘
                       │                       │
         ┌─────────────▼───────────────────────▼──────┐
         │           RabbitMQ Message Bus              │
         │  OrderPlaced · PaymentProcessed ·           │
         │  OrderCancelled · StockUpdated              │
         └─────────────────────────────────────────────┘
```

---

## Tech Stack

| Layer | Technology |
|-------|------------|
| Frontend | Angular 19 (Standalone, Signals, SSR) |
| API Gateway | YARP (Yet Another Reverse Proxy) |
| Backend | ASP.NET Core 8 Web API |
| ORM | Entity Framework Core 8 |
| Message Bus | RabbitMQ + MassTransit |
| Search | Elasticsearch 8 |
| Cache | Redis 7 |
| Auth | JWT + Refresh Token Rotation |
| Payment | Stripe |
| Logging | Serilog |
| Containerization | Docker + Docker Compose |
| Database | SQL Server 2022 |

---

## Project Structure

```
ShopX/
├── docker-compose.yml
├── README.md
│
├── backend/
│   ├── ECommerce.sln
│   │
│   ├── gateway/
│   │   └── ECommerce.ApiGateway/
│   │       ├── Dockerfile
│   │       ├── Program.cs
│   │       └── appsettings.json
│   │
│   └── services/
│       ├── ECommerce.IdentityService/
│       │   ├── Dockerfile
│       │   ├── Controllers/
│       │   │   └── AuthController.cs
│       │   ├── Domain/Entities/
│       │   │   ├── AppUser.cs
│       │   │   └── RefreshToken.cs
│       │   ├── Application/
│       │   │   ├── DTOs/
│       │   │   ├── Services/
│       │   │   │   ├── AuthService.cs
│       │   │   │   └── TokenService.cs
│       │   │   └── Validators/
│       │   ├── Infrastructure/Persistence/
│       │   │   └── AppDbContext.cs
│       │   └── Program.cs
│       │
│       ├── ECommerce.ProductService/
│       │   ├── Dockerfile
│       │   ├── Controllers/
│       │   │   └── ProductsController.cs
│       │   ├── Domain/Entities/
│       │   │   ├── Product.cs
│       │   │   ├── ProductVariant.cs
│       │   │   ├── ProductImage.cs
│       │   │   └── Category.cs
│       │   ├── Application/
│       │   │   ├── DTOs/
│       │   │   └── Services/ProductService.cs
│       │   ├── Infrastructure/
│       │   │   ├── Persistence/ProductDbContext.cs
│       │   │   └── Search/
│       │   │       ├── ProductDocument.cs
│       │   │       └── ElasticsearchService.cs
│       │   └── Program.cs
│       │
│       ├── ECommerce.CartService/
│       │   ├── Dockerfile
│       │   ├── Controllers/CartController.cs
│       │   ├── Domain/Cart.cs
│       │   ├── Application/
│       │   │   ├── DTOs/
│       │   │   └── Services/
│       │   │       ├── CartService.cs
│       │   │       └── ProductPriceClient.cs
│       │   ├── Infrastructure/Cache/
│       │   │   └── CartRepository.cs
│       │   └── Program.cs
│       │
│       ├── ECommerce.OrderService/
│       │   ├── Dockerfile
│       │   ├── Controllers/OrdersController.cs
│       │   ├── Domain/
│       │   │   ├── Entities/
│       │   │   │   ├── Order.cs
│       │   │   │   ├── OrderItem.cs
│       │   │   │   ├── ShippingAddress.cs
│       │   │   │   └── OrderStatusHistory.cs
│       │   │   └── Enums/OrderStatus.cs
│       │   ├── Application/
│       │   │   ├── DTOs/
│       │   │   └── Services/OrderService.cs
│       │   ├── Infrastructure/
│       │   │   ├── Persistence/OrderDbContext.cs
│       │   │   └── Messaging/
│       │   │       ├── Events.cs
│       │   │       └── PaymentProcessedConsumer.cs
│       │   └── Program.cs
│       │
│       └── ECommerce.PaymentService/
│           ├── Dockerfile
│           ├── Controllers/PaymentController.cs
│           ├── Domain/Entities/
│           │   └── PaymentTransaction.cs
│           ├── Application/
│           │   ├── DTOs/
│           │   └── Services/PaymentService.cs
│           ├── Infrastructure/
│           │   ├── Persistence/PaymentDbContext.cs
│           │   └── Messaging/Events.cs
│           └── Program.cs
│
└── frontend/
    └── ecommerce-ui/
        ├── src/
        │   ├── app/
        │   │   ├── core/auth/
        │   │   │   ├── models/auth.models.ts
        │   │   │   ├── guards/auth.guard.ts
        │   │   │   ├── interceptors/token.interceptor.ts
        │   │   │   └── auth.service.ts
        │   │   ├── features/
        │   │   │   ├── auth/
        │   │   │   │   ├── login/
        │   │   │   │   ├── register/
        │   │   │   │   ├── seller-login/
        │   │   │   │   └── seller-register/
        │   │   │   ├── home/
        │   │   │   ├── products/
        │   │   │   ├── cart/
        │   │   │   ├── orders/
        │   │   │   └── admin/
        │   │   └── shared/components/
        │   ├── environments/
        │   └── styles/
        └── angular.json
```

---

## Services

### Identity Service — `:5001`

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| POST | `/api/auth/register` | Public | Register as Customer |
| POST | `/api/auth/register/seller` | Public | Register as Seller |
| POST | `/api/auth/login` | Public | Login (any role) |
| POST | `/api/auth/refresh` | Public | Refresh access token |
| POST | `/api/auth/logout` | Auth | Logout + revoke token |
| GET | `/api/auth/me` | Auth | Get current user info |
| PATCH | `/api/auth/users/{id}/role` | Admin | Update user role |

---

### Product Service — `:5002`

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/products/search` | Public | Full-text search with filters |
| GET | `/api/products/categories` | Public | All active categories |
| GET | `/api/products/slug/{slug}` | Public | Product by slug |
| GET | `/api/products/id/{id}` | Public | Product by ID |
| GET | `/api/products/variants/{id}` | Internal | Variant info for cart |
| POST | `/api/products` | Seller | Create product |
| PUT | `/api/products/{id}` | Seller | Update product |
| DELETE | `/api/products/{id}` | Seller | Deactivate product |
| GET | `/api/products/seller/my-products` | Seller | Seller's own products |
| PATCH | `/api/products/variants/stock` | Seller | Update stock |
| POST | `/api/products/{id}/images` | Seller | Add image |
| DELETE | `/api/products/{id}/images/{imgId}` | Seller | Remove image |
| GET | `/api/products` | Admin | All products |
| PATCH | `/api/products/{id}/approve` | Admin | Approve + index to ES |
| PATCH | `/api/products/{id}/activate` | Admin | Reactivate product |

---

### Cart Service — `:5004`

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/cart` | Auth | Get full cart |
| GET | `/api/cart/summary` | Auth | Total + count only |
| GET | `/api/cart/count` | Auth | Navbar badge count |
| POST | `/api/cart/items` | Auth | Add item |
| PATCH | `/api/cart/items/{variantId}` | Auth | Update quantity |
| DELETE | `/api/cart/items/{variantId}` | Auth | Remove single item |
| DELETE | `/api/cart/items` | Auth | Remove multiple items |
| DELETE | `/api/cart` | Auth | Clear entire cart |
| POST | `/api/cart/validate` | Auth | Pre-checkout validation |
| POST | `/api/cart/merge/{guestId}` | Auth | Merge guest cart on login |

---

### Order Service — `:5003`

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| POST | `/api/orders` | Auth | Place order |
| GET | `/api/orders` | Auth | My orders |
| GET | `/api/orders/{id}` | Auth | Order by ID |
| GET | `/api/orders/number/{number}` | Auth | Order by number |
| POST | `/api/orders/{id}/cancel` | Auth | Cancel order |
| GET | `/api/orders/admin/all` | Admin | All orders with filters |
| PATCH | `/api/orders/{id}/status` | Admin/Seller | Update order status |

**Order Status Flow:**
```
Pending → Confirmed → Processing → Shipped → OutForDelivery → Delivered
                                                           ↓
                                                       Returned → Refunded
Pending / Confirmed / Processing → Cancelled
```

---

### Payment Service — `:5005`

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| POST | `/api/payments/create-intent` | Auth | Create Stripe PaymentIntent |
| GET | `/api/payments/order/{orderId}` | Auth | Get transaction by order |
| POST | `/api/payments/webhook` | Stripe | Stripe webhook (no auth) |
| POST | `/api/payments/refund` | Admin | Process refund |

---

## Getting Started

### Prerequisites

```
- .NET 8 SDK
- Node.js 20+
- Angular CLI 19
- Docker Desktop (16GB RAM recommended)
- Visual Studio 2022 / VS Code
- SQL Server 2022 (or via Docker)
```

### Install Tools

```bash
# Angular CLI
npm install -g @angular/cli@21

# EF Core tools
dotnet tool install --global dotnet-ef

# Verify
ng version
dotnet ef --version
docker --version
```

---

## Environment Variables

> **Never hardcode secrets. Use environment variables in production.**

### Shared across all services
```
Jwt__Secret       = <min 32 char random string — SAME across all services>
Jwt__Issuer       = ECommerce.IdentityService
Jwt__Audience     = ECommerce.Client
```

### Identity Service
```
ConnectionStrings__DefaultConnection = Server=...;Database=ECommerce_Identity;...
AllowedOrigins = http://localhost:4200
```

### Product Service
```
ConnectionStrings__DefaultConnection = Server=...;Database=ECommerce_Products;...
Elasticsearch__Url = http://localhost:9200
AllowedOrigins = http://localhost:5000
```

### Cart Service
```
ConnectionStrings__Redis = localhost:6379
Services__ProductServiceUrl = http://localhost:5002
```

### Order Service
```
ConnectionStrings__DefaultConnection = Server=...;Database=ECommerce_Orders;...
RabbitMQ__Host     = localhost
RabbitMQ__Username = guest
RabbitMQ__Password = guest
```

### Payment Service
```
ConnectionStrings__DefaultConnection = Server=...;Database=ECommerce_Payments;...
Stripe__SecretKey     = sk_test_...
Stripe__WebhookSecret = whsec_...
RabbitMQ__Host        = localhost
```

### Generate a Strong JWT Secret (PowerShell)
```powershell
[System.Convert]::ToBase64String(
  [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
)
```

---

## Database Setup

Run in Visual Studio **Package Manager Console**.
Change **Default Project** before each run.

```powershell
# 1. Identity Service
# Default Project → ECommerce.IdentityService
Add-Migration InitialCreate
Update-Database

# 2. Product Service
# Default Project → ECommerce.ProductService
Add-Migration InitialCreate
Update-Database

# 3. Order Service
# Default Project → ECommerce.OrderService
Add-Migration InitialCreate
Update-Database

# 4. Payment Service
# Default Project → ECommerce.PaymentService
Add-Migration InitialCreate
Update-Database
```

### Seed Data

After migrations, run the SQL seed scripts in SSMS against `ECommerce_Products`:

```
1. Update Categories (3 existing + 5 new)
2. Insert 20 Products
3. Insert 60+ Product Variants
4. Insert 40+ Product Images
```

Admin user is auto-seeded by Identity Service on first startup.

---

## Running with Docker

### Recommended — Infrastructure Only

```bash
# Start only infrastructure
docker-compose up -d sqlserver redis elasticsearch rabbitmq

# Run services from Visual Studio (easier to debug)
```

### Full Stack via Docker

```bash
# Build and start everything
docker-compose up -d --build

# Check status
docker-compose ps

# View logs
docker-compose logs -f identity-service
docker-compose logs -f product-service
docker-compose logs -f order-service

# Stop
docker-compose down

# Stop and wipe all data
docker-compose down -v
```

---

## Running Locally

### Step 1 — Start Infrastructure
```bash
docker-compose up -d sqlserver redis elasticsearch rabbitmq
```

### Step 2 — Run Backend

In Visual Studio → right-click Solution → **Set Startup Projects** → Multiple:

```
ECommerce.ApiGateway      ✓ Start
ECommerce.IdentityService ✓ Start
ECommerce.ProductService  ✓ Start
ECommerce.CartService     ✓ Start
ECommerce.OrderService    ✓ Start
ECommerce.PaymentService  ✓ Start
```

Press **F5**.

### Step 3 — Run Frontend

```bash
cd frontend/ecommerce-ui
npm install
ng serve -o
# Opens http://localhost:4200
```

---

## Service URLs

| Service | Local URL |
|---------|-----------|
| Angular App | http://localhost:4200 |
| API Gateway | http://localhost:5000 |
| Identity Service | http://localhost:5001 |
| Product Service | http://localhost:5002 |
| Order Service | http://localhost:5003 |
| Cart Service | http://localhost:5004 |
| Payment Service | http://localhost:5005 |
| Elasticsearch | http://localhost:9200 |
| RabbitMQ UI | http://localhost:15672 (guest/guest) |
| SQL Server | localhost:1433 |
| Redis | localhost:6379 |

---

## Postman Testing

### Setup Environment

Create environment `ShopX Local`:

| Variable | Value |
|----------|-------|
| `gatewayUrl` | http://localhost:5000 |
| `accessToken` | (empty) |
| `refreshToken` | (empty) |

### Auto-save Token After Login

In Login request **Tests** tab:
```javascript
const res = pm.response.json();
pm.environment.set("accessToken",  res.accessToken);
pm.environment.set("refreshToken", res.refreshToken);
```

### Sample Requests

**Register Customer**
```
POST {{gatewayUrl}}/api/auth/register
Content-Type: application/json

{
  "email":     "customer@test.com",
  "password":  "Test@1234",
  "firstName": "John",
  "lastName":  "Doe"
}
```

**Login**
```
POST {{gatewayUrl}}/api/auth/login
Content-Type: application/json

{
  "email":    "admin@shopx.com",
  "password": "Admin@123456"
}
```

**Search Products**
```
GET {{gatewayUrl}}/api/products/search?searchTerm=sony&sortBy=price_asc
```

**Add to Cart**
```
POST {{gatewayUrl}}/api/cart/items
Authorization: Bearer {{accessToken}}
Content-Type: application/json

{
  "productId": "AAAA0001-0000-0000-0000-000000000001",
  "variantId": "<variant-id>",
  "quantity":  1
}
```

**Place Order**
```
POST {{gatewayUrl}}/api/orders
Authorization: Bearer {{accessToken}}
Content-Type: application/json

{
  "items": [{
    "productId":   "AAAA0001-0000-0000-0000-000000000001",
    "variantId":   "<variant-id>",
    "productName": "Sony WH-1000XM5",
    "sku":         "SONY-XM5-BLK",
    "unitPrice":   24990,
    "quantity":    1
  }],
  "shippingAddress": {
    "fullName":   "John Doe",
    "phone":      "9876543210",
    "line1":      "123 MG Road",
    "city":       "Bengaluru",
    "state":      "Karnataka",
    "postalCode": "560001"
  }
}
```

---

## Default Credentials

> Change these immediately in production.

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@shopx.com | Admin@123456 |

---

## Frontend Routes

| Route | Access | Description |
|-------|--------|-------------|
| `/home` | Public | Home page with hero, categories, products |
| `/auth/login` | Public | Customer login |
| `/auth/register` | Public | Customer register |
| `/auth/seller/login` | Public | Seller portal login |
| `/auth/seller/register` | Public | Seller registration (2-step) |
| `/products` | Public | Product listing + search |
| `/products/:slug` | Public | Product detail |
| `/cart` | Auth | Shopping cart |
| `/checkout` | Auth | Checkout + Stripe payment |
| `/orders` | Auth | My orders |
| `/orders/:id` | Auth | Order detail + tracking |
| `/seller/dashboard` | Seller | Seller dashboard |
| `/admin` | Admin | Admin panel |
| `/unauthorized` | Public | 403 error page |

---

## Message Flow

```
1. Customer places order
   POST /api/orders
        │
        ▼
2. OrderService creates Order (Pending)
   Publishes → OrderPlacedEvent to RabbitMQ
        │
        ▼
3. Frontend calls PaymentService
   POST /api/payments/create-intent
   Returns clientSecret
        │
        ▼
4. Customer pays via Stripe.js (frontend)
        │
        ▼
5. Stripe sends webhook
   POST /api/payments/webhook
   PaymentService verifies signature
        │
        ├── Success → MarkSucceeded()
        │             Publishes → PaymentProcessedEvent (IsSuccess: true)
        │
        └── Failure → MarkFailed()
                      Publishes → PaymentProcessedEvent (IsSuccess: false)
                          │
                          ▼
6. OrderService consumes PaymentProcessedEvent
   Success → Order.ConfirmPayment() → Status: Confirmed
   Failure → Order.FailPayment()   → Status: Cancelled
```

---

## Roadmap

### ✅ Phase 1 — Auth Foundation
- JWT + refresh token rotation
- Role-based access (Customer, Seller, Admin)
- API Gateway (YARP) with rate limiting

### ✅ Phase 2 — Catalog & Cart
- Product service with Elasticsearch full-text search
- Redis cart with server-side price validation
- Category management + seed data

### ✅ Phase 3 — Orders & Payments
- Full order lifecycle with state machine
- Stripe PaymentIntent + webhook handling
- RabbitMQ event-driven architecture

### 🔲 Phase 4 — Notifications
- Email (SendGrid)
- SMS (Twilio)
- Push notifications (Firebase)

### 🔲 Phase 5 — Production Hardening
- Health checks (`/health` on every service)
- Distributed tracing (OpenTelemetry + Jaeger)
- Metrics dashboards (Prometheus + Grafana)
- Load testing (k6 / NBomber)
- CI/CD pipeline (GitHub Actions)
- Kubernetes deployment (Azure AKS)

---

## Security Notes

```
✅ JWT access tokens expire in 15 minutes
✅ Refresh token rotation — old token revoked on each use
✅ BCrypt password hashing (work factor 12)
✅ Stripe webhook signature verification
✅ Idempotency keys prevent double payment charges
✅ Server-side price validation — client prices never trusted
✅ Role enforcement at Gateway AND service level
✅ SQL injection prevention via EF Core parameterized queries
✅ CORS locked to known origins only

⚠️  Move all secrets to Azure Key Vault in production
⚠️  Enable HTTPS everywhere in production
⚠️  Restrict CORS to production domain only
⚠️  Never expose /api/auth/users role endpoint without Admin auth
⚠️  Rotate JWT secret periodically
```

---

## License

MIT License — free to use for personal and commercial projects.

---

<div align="center">
  Built with ❤️ using Angular 19 + ASP.NET Core 8 Microservices
</div>
