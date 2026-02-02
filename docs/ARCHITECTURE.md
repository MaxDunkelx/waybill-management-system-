# Waybill Management System - Architecture Documentation

## Overview

The Waybill Management System is a multi-tenant .NET 10 Web API designed to manage construction waybill data with full support for Hebrew text encoding. The system implements strict tenant isolation at multiple layers to ensure complete data separation between construction companies.

## Architecture Principles

1. **Multi-Tenant Isolation**: Defense-in-depth approach with tenant filtering at middleware, service, and database layers
2. **Event-Driven Architecture**: Asynchronous event processing using RabbitMQ for decoupled operations
3. **RESTful API Design**: Standard HTTP methods and status codes with comprehensive error handling
4. **Hebrew Text Support**: UTF-8 encoding throughout the entire pipeline
5. **Concurrency Safety**: Optimistic locking and distributed locking for race condition prevention

## System Components

### Core Services

- **TenantMiddleware**: Extracts and validates tenant ID from HTTP headers
- **TenantService**: Provides tenant context to all services
- **WaybillImportService**: Handles CSV parsing, validation, and database operations
- **WaybillValidationService**: Implements business rule validation
- **WaybillService**: Provides querying, filtering, and aggregation capabilities
- **MessagePublisher**: Publishes events to RabbitMQ
- **MessageConsumer**: Consumes events from RabbitMQ
- **DistributedLockService**: Manages distributed locks for single-user execution

### Data Layer

- **ApplicationDbContext**: EF Core DbContext with global query filters for tenant isolation
- **SQL Server Database**: Stores waybill, project, supplier, and tenant data
- **Entity Models**: Waybill, Project, Supplier, Tenant with proper relationships

### External Services

- **RabbitMQ**: Message broker for event-driven architecture
- **Redis**: Available for caching and distributed locking (optional)

## Request Flow Architecture

See [ARCHITECTURE_DIAGRAMS.md](ARCHITECTURE_DIAGRAMS.md) for detailed flow diagrams.

## Tenant Isolation Strategy

The system implements a three-layer defense-in-depth approach:

1. **Middleware Layer**: TenantMiddleware validates and extracts tenant ID from `X-Tenant-ID` header
2. **Service Layer**: All services verify tenant ID matches current context
3. **Database Layer**: Global query filters automatically filter all queries by TenantId

This ensures that even if application code has bugs, the database layer prevents cross-tenant data access.

## Data Flow Patterns

### CSV Import Flow

1. File upload → TenantMiddleware validates tenant ID
2. WaybillImportController receives file
3. WaybillImportService parses CSV with CsvHelper (UTF-8)
4. WaybillValidationService validates each row
5. Database upsert operations (create/update waybills, projects, suppliers)
6. Publish WaybillsImportedEvent to RabbitMQ
7. Return import results with success/error counts

### Query Flow

1. HTTP request with X-Tenant-ID header
2. TenantMiddleware extracts tenant ID → stores in HttpContext.Items
3. Controller receives request → calls service
4. Service calls TenantService.GetCurrentTenantId()
5. ApplicationDbContext uses tenant ID in global query filter
6. Database query automatically includes WHERE TenantId = @tenantId
7. Results returned (already filtered by tenant)

### Event Processing Flow

1. WaybillImportService completes import
2. Publishes WaybillsImportedEvent to RabbitMQ exchange
3. WaybillEventConsumer (background service) receives event
4. Consumer processes event (logging, statistics, notifications)
5. Event processing is asynchronous and decoupled

## Security Considerations

- **Tenant ID Required**: All API requests must include X-Tenant-ID header
- **Automatic Filtering**: Global query filters prevent cross-tenant queries
- **Composite Keys**: Suppliers use (TenantId, Id) composite key for proper isolation
- **Input Validation**: Comprehensive validation at CSV import and API endpoints
- **Error Handling**: Errors never expose tenant data or system internals

## Scalability Considerations

- **Stateless API**: All state stored in database, supports horizontal scaling
- **Event-Driven**: RabbitMQ enables asynchronous processing and decoupling
- **Database Indexing**: Proper indexes on TenantId, Status, Dates for query performance
- **Caching Ready**: Redis available for caching frequently accessed data

## Technology Stack

- **Backend**: .NET 10 Web API
- **Database**: SQL Server with EF Core
- **Message Broker**: RabbitMQ
- **Cache**: Redis (optional)
- **Frontend**: React/TypeScript (optional, for testing)

## Deployment Architecture

- **Docker Compose**: Local development with SQL Server, RabbitMQ, Redis
- **Production Ready**: Stateless API can be deployed to multiple instances
- **Database**: Single SQL Server instance with tenant-scoped queries
- **Message Broker**: RabbitMQ cluster for high availability (production)
