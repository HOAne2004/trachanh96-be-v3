# 🍹 TraChanh96 - Beverage Management System

> **A modern, scalable microservices-based beverage e-commerce platform built with .NET 10 and Clean Architecture**

**Status**: 🚀 Active Development | **Version**: 1.0.0 | **Last Updated**: March 2026

---

## 📋 Table of Contents

- [Overview](#-overview)
- [Key Features](#-key-features)
- [Architecture](#-architecture)
- [Tech Stack](#-tech-stack)
- [Project Structure](#-project-structure)
- [Modules](#-modules)
- [Getting Started](#-getting-started)
- [API Documentation](#-api-documentation)
- [Database Setup](#-database-setup)
- [Configuration](#-configuration)
- [Development](#-development)
- [Security](#-security-considerations)
- [Deployment](#-deployment)
- [Contributing](#-contributing)

---

## 🎯 Overview

**TraChanh96** is a comprehensive beverage e-commerce and management system designed for modern retail operations. It provides a complete solution for multi-store beverage businesses with advanced features like AI-powered support, payment processing, and real-time order management.

### What is TraChanh96?
- 🛍️ **E-Commerce Platform**: Complete product catalog with multi-store support
- 🤖 **AI-Powered Support**: Google Gemini chatbot integration for customer assistance
- 💳 **Payment Processing**: VNPay gateway integration for Vietnamese payments
- 📦 **Order Management**: Full order lifecycle with Dine-In, Takeout, and Delivery support
- 🏪 **Multi-Store System**: Independent store management with separate inventories
- 🔐 **Enterprise Security**: JWT authentication, RBAC, rate limiting, and encryption
- 🗄️ **PostgreSQL Backend**: Neon cloud database with full audit trail

---

## ✨ Key Features

### 🛍️ E-Commerce & Catalog
| Feature | Description |
|---------|-------------|
| **Product Management** | Create, edit, and manage beverage products with detailed attributes |
| **Multi-Store Support** | Different prices, inventory, and availability per store |
| **Advanced Search** | Search, filter by category, sort by price/rating, with pagination |
| **Product Ratings** | Customer review system with star ratings and comments |
| **Stock Management** | Real-time inventory tracking and availability management |
| **SEO-Friendly URLs** | Auto-generated slugs for product discovery |

### 👤 User Management & Authentication
| Feature | Description |
|---------|-------------|
| **Secure Registration** | Email verification with confirmation tokens |
| **JWT Authentication** | Stateless, secure token-based authentication |
| **Multi-Device Sessions** | Track and manage active sessions across devices |
| **Password Security** | Secure reset flows with email confirmation |
| **RBAC System** | Role-based access control with 50+ permissions |
| **User Profiles** | Flexible profile management for customers and staff |

### 🏪 Store Management
| Feature | Description |
|---------|-------------|
| **Store Profiles** | Complete store information and branding |
| **Operating Hours** | Configure availability per day of week |
| **Location Tracking** | GPS coordinates with address verification |
| **Delivery Radius** | Configure delivery zones per store |
| **Store Settings** | Customizable business rules per location |

### 📦 Order Management
| Feature | Description |
|---------|-------------|
| **Order Types** | Support Dine-In, Takeout, and Delivery orders |
| **Status Tracking** | Complete lifecycle from Pending → Processing → Delivered |
| **Order History** | Customer order history with filtering and search |
| **Table Management** | Dine-in table assignment and management |
| **Voucher System** | Discount codes and promotional campaigns |
| **Concurrency Control** | Prevents race conditions during order processing |

### 💳 Payment Processing
| Feature | Description |
|---------|-------------|
| **VNPay Integration** | Secure Vietnamese payment gateway |
| **Payment Status** | Real-time payment status monitoring |
| **Transaction History** | Complete audit trail for compliance |
| **Multi-Currency** | Support for multiple currency types |
| **Webhook Handling** | Secure callback processing for confirmations |

### 🤖 AI-Powered Support
| Feature | Description |
|---------|-------------|
| **Chatbot** | Google Gemini integration for intelligent responses |
| **Context Awareness** | AI understands product catalog and business rules |
| **Session Management** | Persistent conversation history per user |
| **Smart Recommendations** | Product suggestions based on conversation context |

### 🔐 Security Features
| Feature | Description |
|---------|-------------|
| **Rate Limiting** | Sliding window rate limiter (10 req/min for auth endpoints) |
| **JWT Validation** | Secure token verification with expiration |
| **Permission-Based Access** | Fine-grained authorization per endpoint |
| **Soft Delete** | Recoverable deletion for GDPR compliance |
| **Encrypted Passwords** | BCrypt hashing with random salts |
| **Concurrency Control** | Row versioning to prevent race conditions |

---

## 🏗️ Architecture

### Clean Architecture Model
The project follows **Clean Architecture** with separation of concerns:

```
┌─────────────────────────────────┐
│   Presentation (Controllers)    │ ← API Layer
└────────────┬────────────────────┘
             ▼
┌─────────────────────────────────┐
│  Application (CQRS Handlers)    │ ← Business Logic
└────────────┬────────────────────┘
             ▼
┌─────────────────────────────────┐
│  Domain (Entities & Rules)      │ ← Business Rules
└────────────┬────────────────────┘
             ▼
┌─────────────────────────────────┐
│  Infrastructure (Data & APIs)   │ ← External Services
└─────────────────────────────────┘
```

### Design Patterns
- **CQRS**: Separate read and write models via MediatR
- **MediatR Pipeline**: Request/response handler chains
- **Repository Pattern**: Abstract data access
- **Unit of Work**: Atomic transaction management
- **Domain Events**: Event-driven inter-module communication
- **Value Objects**: Type-safe business concepts
- **Soft Delete**: Logical deletion with recovery
- **Optimistic Locking**: Row version concurrency control

---

## 💻 Tech Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| **Runtime** | .NET | 10.0 |
| **Language** | C# | 13.0 |
| **Web Framework** | ASP.NET Core | 10.0 |
| **ORM** | Entity Framework Core | 10.0.3 |
| **Database** | PostgreSQL (Neon) | Latest |
| **CQRS** | MediatR | 14.1.0 |
| **Validation** | FluentValidation | 12.1.1 |
| **Auth** | System.IdentityModel.Tokens.Jwt | 8.17.0 |
| **API Docs** | Swashbuckle/Swagger | 6.6.2 |
| **AI** | Google Gemini API | Latest |
| **Payments** | VNPay Gateway | Latest |
| **Storage** | Supabase PostgreSQL | Latest |

---

## 📁 Project Structure

```
DrinkingBeV3/
├── src/
│   ├── API/
│   │   └── BeverageSystem.Api/          # API Gateway & Startup
│   │       ├── Controllers/              # (Routed via modules)
│   │       ├── Middlewares/
│   │       ├── Extensions/
│   │       ├── Program.cs                # Dependency Injection Setup
│   │       └── appsettings.json
│   │
│   ├── Modules/
│   │   ├── Identity/                    # Authentication & User Management
│   │   │   ├── Identity.Domain/         # User, Role, Permission entities
│   │   │   ├── Identity.Application/    # Login, Register commands
│   │   │   ├── Identity.Infrastructure/ # DB context, JWT provider
│   │   │   └── Identity.Presentation/   # AuthController, UsersController
│   │   │
│   │   ├── Catalog/                     # Products & Categories
│   │   │   ├── Catalog.Domain/          # Product, Category entities
│   │   │   ├── Catalog.Application/     # CRUD operations
│   │   │   ├── Catalog.Infrastructure/  # DB access
│   │   │   └── Catalog.Presentation/    # ProductsController
│   │   │
│   │   ├── Stores/                      # Store & Inventory Management
│   │   │   ├── Stores.Domain/           # Store, OperatingHour entities
│   │   │   ├── Stores.Application/
│   │   │   ├── Stores.Infrastructure/
│   │   │   └── Stores.Presentation/
│   │   │
│   │   ├── Orders/                      # Order Processing
│   │   │   ├── Orders.Domain/           # Order, OrderItem entities
│   │   │   ├── Orders.Application/      # Create, Update commands
│   │   │   ├── Orders.Infrastructure/   # DB access
│   │   │   └── Orders.Presentation/     # OrdersController
│   │   │
│   │   ├── Payments/                    # Payment Processing
│   │   │   ├── Payments.Domain/         # Payment entity
│   │   │   ├── Payments.Application/
│   │   │   ├── Payments.Infrastructure/ # VNPay integration
│   │   │   └── Payments.Presentation/   # PaymentsController
│   │   │
│   │   └── AI/                          # AI Chatbot
│   │       ├── AI.Domain/               # Session, Message entities
│   │       ├── AI.Application/          # Chat commands
│   │       ├── AI.Infrastructure/       # Gemini integration
│   │       └── AI.Presentation/         # ChatController
│   │
│   └── Shared/
│       ├── Shared.Domain/               # Common base classes
│       ├── Shared.Application/          # Common behaviors & DTOs
│       ├── Shared.Infrastructure/       # DB context, Email service
│       └── Shared.Presentation/         # BaseApiController
│
└── README.md                             # This file
```

---

## 🔌 Modules Overview

### 🔐 Identity Module
**Responsibility**: User authentication, authorization, and account management

**Key Classes**:
- `User` - User entity with roles and sessions
- `Role` - Role definition with permissions
- `Permission` - Fine-grained access control
- `RefreshToken` - Token lifecycle management

**Key Operations** (28 Commands + 9 Queries):
```
Commands: Register, Login, RefreshToken, Logout, ChangePassword, UpdateProfile,
          CreateUserByAdmin, UpdateUserByAdmin, AssignRoles, LockUser, UnlockUser,
          DeleteUser, RestoreUser, RequestChangeEmail, ConfirmChangeEmail...

Queries: GetProfile, GetMyAddresses, GetMyActiveSessions, GetUsers, GetRoles...
```

**API Base**: `/api/identity/`

---

### 🛍️ Catalog Module
**Responsibility**: Product and category management

**Key Classes**:
- `Product` - Beverage product definition
- `Category` - Product classification
- `StoreProduct` - Store-specific product configurations
- `ProductRating` - Customer reviews

**Key Features**:
- Product CRUD with admin controls
- Multi-store pricing and availability
- Search and advanced filtering
- Customer product catalog with ratings
- Slug generation for SEO

**API Base**: `/api/catalog/`

---

### 🏪 Stores Module
**Responsibility**: Multi-store management

**Key Classes**:
- `Store` - Store entity with full configuration
- `OperatingHour` - Store operating hours per day
- `StoreProduct` - Product override per store

**Key Features**:
- Store creation and configuration
- Operating hours per day of week
- Location management (GPS)
- Delivery radius configuration
- Store performance metrics

**API Base**: `/api/stores/`

---

### 📦 Orders Module
**Responsibility**: Order processing and fulfillment

**Key Classes**:
- `Order` - Main order entity
- `OrderItem` - Individual items in order
- `OrderStatusHistory` - Status change audit trail
- `Table` - Dine-in table reference

**Key Features**:
- Support for Dine-In, Takeout, Delivery
- Complete order lifecycle management
- Status tracking with history
- Voucher and discount application
- Delivery details management

**API Base**: `/api/orders/`

---

### 💳 Payments Module
**Responsibility**: Payment gateway integration

**Key Classes**:
- `Payment` - Payment transaction record
- `PaymentTransaction` - Transaction details

**Key Features**:
- VNPay payment gateway integration
- Payment status tracking
- Transaction history and audit
- Webhook/callback handling
- Payment reconciliation

**API Base**: `/api/payments/`

---

### 🤖 AI Module
**Responsibility**: AI-powered customer support

**Key Classes**:
- `AISession` - Chat session management
- `AIMessage` - Message storage

**Key Features**:
- Google Gemini API integration
- Conversational chat interface
- Context-aware responses
- Session history persistence
- Product knowledge integration

**API Base**: `/api/ai/`

---

## 🚀 Getting Started

### Prerequisites
- **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download)
- **Visual Studio 2026** (Community or Pro)
- **PostgreSQL** client (pgAdmin or psql)
- **Git**

### Quick Start (5 minutes)

1. **Clone Repository**
   ```bash
   git clone https://github.com/HOAne2004/trachanh96-be-v3.git
   cd DrinkingBeV3
   ```

2. **Install Dependencies**
   ```bash
   dotnet restore
   ```

3. **Configure Database** (edit `src/API/BeverageSystem.Api/appsettings.json`)
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=trachanh96;Username=postgres;Password=your-password;"
     }
   }
   ```

4. **Run Migrations**
   ```bash
   dotnet ef database update --project src/API/BeverageSystem.Api
   ```

5. **Start API**
   ```bash
   cd src/API/BeverageSystem.Api
   dotnet run
   ```

6. **Open Swagger UI**
   - Navigate to: `https://localhost:7001/swagger`
   - API runs on: `https://localhost:7001`

---

## 📡 API Documentation

### Authentication Endpoints
```
POST   /api/identity/auth/register              # Register user
POST   /api/identity/auth/login                 # Login user
POST   /api/identity/auth/refresh-token         # Refresh JWT
POST   /api/identity/auth/logout                # Logout
POST   /api/identity/auth/forgot-password       # Reset password request
POST   /api/identity/auth/reset-password        # Reset password
POST   /api/identity/auth/verify-email          # Verify email
```

### User Profile Endpoints
```
GET    /api/identity/users/me                   # Get profile
PUT    /api/identity/users/me                   # Update profile
PUT    /api/identity/users/me/password          # Change password
PUT    /api/identity/users/me/email             # Request email change
DELETE /api/identity/users/me/email/pending     # Cancel email change
```

### Sessions & Addresses
```
GET    /api/identity/users/me/sessions          # Active sessions
DELETE /api/identity/users/me/sessions          # Logout all devices

GET    /api/identity/users/me/addresses         # User addresses
POST   /api/identity/users/me/addresses         # Add address
PUT    /api/identity/users/me/addresses/{id}    # Update address
PATCH  /api/identity/users/me/addresses/{id}/default # Set default
```

### Admin - Users & Roles
```
GET    /api/identity/admin/users                # List users (paginated)
GET    /api/identity/admin/users/{id}           # Get user
POST   /api/identity/admin/users                # Create user
PUT    /api/identity/admin/users/{id}           # Update user
PUT    /api/identity/admin/users/{id}/lock      # Lock account
PUT    /api/identity/admin/users/{id}/unlock    # Unlock account
DELETE /api/identity/admin/users/{id}           # Delete (soft)
PUT    /api/identity/admin/users/{id}/restore   # Restore

GET    /api/identity/admin/roles                # List roles
POST   /api/identity/admin/roles                # Create role
PUT    /api/identity/admin/roles/{id}           # Update role
DELETE /api/identity/admin/roles/{id}           # Delete role
PUT    /api/identity/admin/roles/{id}/permissions # Assign permissions
```

### Products & Catalog
```
GET    /api/catalog/products                    # Get products (search, filter)
GET    /api/catalog/products/{id}               # Get product details
GET    /api/catalog/categories                  # Get categories

POST   /api/catalog/admin/products              # Create product (admin)
PUT    /api/catalog/admin/products/{id}         # Update product (admin)
DELETE /api/catalog/admin/products/{id}         # Delete product (admin)
```

### Orders & Payments
```
POST   /api/orders                              # Create order
GET    /api/orders/{id}                         # Get order
GET    /api/orders/history                      # Order history
PUT    /api/orders/{id}/status                  # Update status
DELETE /api/orders/{id}                         # Cancel order

POST   /api/payments/vnpay-checkout             # Initiate payment
GET    /api/payments/{id}                       # Get payment status
POST   /api/payments/vnpay-return               # VNPay callback
```

### AI Chat
```
POST   /api/ai/chat/send                        # Send message
GET    /api/ai/chat/history/{sessionId}         # Chat history
DELETE /api/ai/chat/sessions/{sessionId}        # Delete session
```

> **Full API documentation available at**: `https://localhost:7001/swagger` (Swagger UI)

---

## 🗄️ Database Setup

### Database Architecture

The system uses **PostgreSQL** (hosted on Neon) with schema separation:

| Schema | Purpose |
|--------|---------|
| `identity` | Users, roles, permissions, sessions |
| `catalog` | Products, categories, ratings |
| `stores` | Store data, operating hours |
| `orders` | Orders, items, status history |
| `payments` | Payment records and transactions |
| `ai` | Chat sessions and messages |

### Create Database

```bash
# Using psql
createdb trachanh96

# Using connection string in appsettings.json
dotnet ef database update
```

### Entity Framework Migrations

```bash
# List pending migrations
dotnet ef migrations list

# Apply all migrations
dotnet ef database update

# Create new migration
dotnet ef migrations add AddNewFeature --project src/Modules/[Module]/[Module].Infrastructure

# Remove last migration (before applying)
dotnet ef migrations remove --project src/Modules/[Module]/[Module].Infrastructure

# Generate SQL script
dotnet ef migrations script --idempotent --output migrations.sql
```

---

## ⚙️ Configuration

### appsettings.json Template

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",

  "ConnectionStrings": {
    "DefaultConnection": "Host=your-host;Database=trachanh96;Username=user;Password=pass;SSL Mode=Require;"
  },

  "JwtSettings": {
    "Key": "your-super-secret-key-min-32-characters-long!!",
    "Issuer": "TraChanh96Issuer",
    "Audience": "TraChanh96Audience",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  },

  "EmailSettings": {
    "CompanyName": "TraChanh96",
    "DefaultFromEmail": "noreply@trachanh96.com",
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password"
  },

  "VnPayConfig": {
    "TmnCode": "your-merchant-code",
    "HashSecret": "your-hash-secret",
    "VnPayUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
    "ReturnUrl": "https://api.yourdomain.com/api/payments/vnpay-return"
  },

  "Supabase": {
    "Url": "https://your-project.supabase.co",
    "Key": "your-anon-public-key"
  },

  "AI": {
    "GeminiApiKey": "your-gemini-api-key"
  },

  "FrontendSettings": {
    "BaseUrl": "http://localhost:5173"
  }
}
```

### Environment Variables (Production)

```bash
export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__DefaultConnection="Host=prod-db;..."
export JwtSettings__Key="your-prod-secret-key"
export EmailSettings__Password="your-prod-smtp-password"
export VnPayConfig__HashSecret="your-prod-hash"
```

---

## 🔧 Development

### Build & Run

```bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Build release version
dotnet build --configuration Release

# Run API
dotnet run --project src/API/BeverageSystem.Api

# Run with hot reload
dotnet watch run --project src/API/BeverageSystem.Api
```

### Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test src/Modules/Identity/Identity.Application.Tests

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run with specific filter
dotnet test --filter "FullyQualifiedName~CreateUserCommand"
```

### Code Quality

```bash
# Check formatting
dotnet format --verify-no-changes

# Auto-format code
dotnet format

# Build with code analysis
dotnet build /p:EnforceCodeStyleInBuild=true
```

---

## 🔒 Security Considerations

### Authentication & Authorization
- ✅ JWT tokens with expiration
- ✅ Refresh token rotation
- ✅ BCrypt password hashing
- ✅ HTTPS/TLS encryption
- ✅ CORS configured for frontend only
- ✅ Rate limiting on auth endpoints

### Data Protection
- ✅ SQL injection prevention (EF Core parameterized queries)
- ✅ XSS prevention (JSON serialization)
- ✅ Soft delete for audit trail
- ✅ Row versioning for concurrency
- ✅ Encrypted password storage

### Production Security
1. Set strong JWT secret (min 32 chars, random)
2. Enable HTTPS with valid certificate
3. Configure `KnownNetworks` for proxy headers
4. Use environment variables for sensitive config
5. Enable database connection encryption
6. Set up request logging and monitoring
7. Regular security patches and updates
8. Run behind reverse proxy (nginx) with security headers

---

## 🚀 Deployment

### Docker Deployment

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 as build
WORKDIR /app
COPY . .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .
EXPOSE 7001
ENTRYPOINT ["dotnet", "BeverageSystem.Api.dll"]
```

### Deploy Command
```bash
docker build -t trachanh96-api .
docker run -p 7001:7001 \
  -e ConnectionStrings__DefaultConnection="..." \
  -e JwtSettings__Key="..." \
  trachanh96-api
```

### Azure Deployment
```bash
az webapp up --name trachanh96-api \
  --resource-group myResourceGroup \
  --runtime "DOTNETCORE|10.0"
```

---

## 🤝 Contributing

1. Fork the repository
2. Create feature branch: `git checkout -b feature/amazing-feature`
3. Follow code style and architecture patterns
4. Commit: `git commit -m 'feat: add amazing feature'`
5. Push: `git push origin feature/amazing-feature`
6. Create Pull Request

### Code Style
- **Architecture**: Clean Architecture + DDD
- **Naming**: PascalCase for public, camelCase for private
- **Documentation**: XML comments for public APIs
- **Testing**: Unit tests for business logic

---

## 📊 Project Statistics

| Metric | Count |
|--------|-------|
| **Modules** | 6 (Identity, Catalog, Stores, Orders, Payments, AI) |
| **Commands** | ~50+ |
| **Queries** | ~20+ |
| **Controllers** | 10+ |
| **Database Tables** | 20+ |
| **API Endpoints** | 50+ |
| **Permission Types** | 50+ |
| **Lines of Code** | 15,000+ |

---

## 📝 Common Commands Reference

```bash
# Database
dotnet ef database update
dotnet ef migrations add MigrationName --project src/Modules/Identity/Identity.Infrastructure
dotnet ef database drop --force

# Build & Run
dotnet build
dotnet run --project src/API/BeverageSystem.Api
dotnet watch run

# Testing
dotnet test
dotnet test --filter "Class=MyTests"

# Code Quality
dotnet format
dotnet build /p:EnforceCodeStyleInBuild=true
```

---

## 🔗 Resources

- **Repository**: https://github.com/HOAne2004/trachanh96-be-v3
- **.NET 10 Docs**: https://docs.microsoft.com/dotnet/
- **EF Core Guide**: https://docs.microsoft.com/ef/core/
- **Clean Architecture**: https://www.oreilly.com/library/view/clean-architecture/
- **CQRS Pattern**: https://martinfowler.com/bliki/CQRS.html
- **DDD**: https://www.domainlanguage.com/ddd/

---

## 📧 Support

- **Issues**: Report via [GitHub Issues](https://github.com/HOAne2004/trachanh96-be-v3/issues)
- **Discussions**: Use [GitHub Discussions](https://github.com/HOAne2004/trachanh96-be-v3/discussions)

---

## 🙏 Acknowledgments

Built with modern .NET technologies and clean architecture principles. Thanks to the .NET community and open-source contributors.

---

**Made with ❤️ by the TraChanh96 team**
