# Waybill Management System - Architecture Diagrams

This document contains Mermaid diagrams visualizing the system architecture, data flows, and component interactions.

## 1. Request Flow Diagram

This diagram shows the complete lifecycle of an HTTP request through the system, including tenant resolution at each layer.

```mermaid
sequenceDiagram
    participant Client
    participant TenantMiddleware
    participant Controller
    participant Service
    participant TenantService
    participant DbContext
    participant Database

    Client->>TenantMiddleware: HTTP Request<br/>(X-Tenant-ID: TENANT001)
    TenantMiddleware->>TenantMiddleware: Extract & Validate Tenant ID
    TenantMiddleware->>TenantMiddleware: Store in HttpContext.Items
    TenantMiddleware->>Controller: Forward Request
    
    Controller->>TenantService: GetCurrentTenantId()
    TenantService->>TenantService: Read from HttpContext.Items
    TenantService-->>Controller: Return Tenant ID
    
    Controller->>Service: ProcessRequest(tenantId, ...)
    Service->>DbContext: Query Data
    DbContext->>TenantService: GetCurrentTenantId()
    TenantService-->>DbContext: Return Tenant ID
    DbContext->>DbContext: Apply Global Query Filter<br/>(WHERE TenantId = @tenantId)
    DbContext->>Database: Execute Filtered Query
    Database-->>DbContext: Return Tenant-Scoped Results
    DbContext-->>Service: Return Filtered Data
    Service-->>Controller: Return Results
    Controller-->>Client: HTTP 200 OK + Data
```

## 2. CSV Import Flow Diagram

This diagram shows the complete CSV import pipeline from file upload to database persistence and event publishing.

```mermaid
flowchart TD
    Start([Client Uploads CSV File]) --> Middleware[TenantMiddleware<br/>Extract X-Tenant-ID]
    Middleware --> ValidateTenant{Valid<br/>Tenant ID?}
    ValidateTenant -->|No| Reject[Return 400 Bad Request]
    ValidateTenant -->|Yes| Controller[WaybillImportController<br/>Receive File]
    
    Controller --> ImportService[WaybillImportService<br/>ImportFromCsvAsync]
    ImportService --> ParseCSV[CsvHelper Parse CSV<br/>UTF-8 Encoding]
    ParseCSV --> ValidateRow[WaybillValidationService<br/>Validate Each Row]
    
    ValidateRow --> ValidRow{Row<br/>Valid?}
    ValidRow -->|No| CollectError[Collect Error<br/>Continue Processing]
    ValidRow -->|Yes| CheckTenant[Validate CSV Tenant ID<br/>Matches Header]
    
    CheckTenant --> TenantMatch{Tenant IDs<br/>Match?}
    TenantMatch -->|No| CollectError
    TenantMatch -->|Yes| EnsureEntities[Ensure Project/Supplier<br/>Exist in Database]
    
    EnsureEntities --> UpsertWaybill[Upsert Waybill<br/>Create or Update]
    UpsertWaybill --> NextRow{More<br/>Rows?}
    NextRow -->|Yes| ValidateRow
    NextRow -->|No| Transaction[Commit Transaction]
    
    CollectError --> NextRow
    Transaction --> PublishEvent[Publish WaybillsImportedEvent<br/>to RabbitMQ]
    PublishEvent --> ReturnResults[Return ImportResultDto<br/>Success/Error Counts]
    ReturnResults --> End([Import Complete])
    
    style ValidateTenant fill:#ffcccc
    style TenantMatch fill:#ffcccc
    style ValidRow fill:#ffffcc
    style Transaction fill:#ccffcc
    style PublishEvent fill:#ccccff
```

## 3. Multi-Tenant Isolation Flow

This diagram demonstrates how tenant isolation is enforced at multiple layers (defense-in-depth).

```mermaid
flowchart LR
    subgraph RequestLayer[Request Layer]
        HTTP[HTTP Request<br/>X-Tenant-ID Header]
        Middleware[TenantMiddleware<br/>Extract & Validate]
        Context[HttpContext.Items<br/>Store Tenant ID]
    end
    
    subgraph ServiceLayer[Service Layer]
        Controller[Controller<br/>Get Tenant ID]
        Service[Service<br/>Verify Tenant ID]
        TenantService[TenantService<br/>Provide Context]
    end
    
    subgraph DatabaseLayer[Database Layer]
        DbContext[ApplicationDbContext<br/>Global Query Filter]
        Query[SQL Query<br/>WHERE TenantId = @tenantId]
        Results[Filtered Results<br/>Tenant-Scoped Only]
    end
    
    HTTP --> Middleware
    Middleware -->|Valid| Context
    Middleware -->|Invalid| Reject[400 Bad Request]
    
    Context --> Controller
    Controller --> TenantService
    TenantService --> Service
    Service -->|Verify Match| DbContext
    
    DbContext --> Query
    Query --> Results
    
    style Middleware fill:#ffcccc
    style Service fill:#ffffcc
    style DbContext fill:#ccffcc
    style Query fill:#ccccff
```

## 4. Event-Driven Architecture Flow

This diagram shows how events flow through RabbitMQ for asynchronous processing.

```mermaid
sequenceDiagram
    participant ImportService
    participant MessagePublisher
    participant RabbitMQ
    participant MessageConsumer
    participant BackgroundService
    participant Database

    ImportService->>ImportService: CSV Import Complete
    ImportService->>MessagePublisher: PublishWaybillsImportedEvent()
    MessagePublisher->>RabbitMQ: Publish Event to Exchange
    RabbitMQ-->>MessagePublisher: Acknowledge
    
    Note over RabbitMQ: Event Queued<br/>WaybillsImportedEvent
    
    RabbitMQ->>MessageConsumer: Deliver Event
    MessageConsumer->>BackgroundService: ProcessWaybillsImportedEvent()
    BackgroundService->>BackgroundService: Log Import Statistics
    BackgroundService->>Database: Update Audit Log
    BackgroundService->>BackgroundService: Trigger Notifications
    BackgroundService-->>MessageConsumer: Processing Complete
    MessageConsumer-->>RabbitMQ: Acknowledge
    
    Note over BackgroundService: Asynchronous Processing<br/>Decoupled from Import
```

## 5. Concurrency Handling Flow

This diagram shows how optimistic locking and distributed locking prevent race conditions.

```mermaid
sequenceDiagram
    participant UserA
    participant UserB
    participant API
    participant LockService
    participant Database

    Note over UserA,UserB: Concurrent Update Scenario
    
    UserA->>API: GET /api/waybills/WB-001<br/>(Version: 0x1234)
    API->>Database: Load Waybill
    Database-->>UserA: Waybill + Version
    
    UserB->>API: GET /api/waybills/WB-001<br/>(Version: 0x1234)
    API->>Database: Load Waybill
    Database-->>UserB: Waybill + Version
    
    UserB->>API: PUT /api/waybills/WB-001<br/>(Version: 0x1234)
    API->>Database: Update Waybill<br/>(Version Check: 0x1234)
    Database->>Database: Update + Increment Version
    Database-->>API: Success (Version: 0x5678)
    API-->>UserB: 200 OK
    
    UserA->>API: PUT /api/waybills/WB-001<br/>(Version: 0x1234 - OLD!)
    API->>Database: Update Waybill<br/>(Version Check: 0x1234)
    Database->>Database: Version Mismatch!<br/>(Current: 0x5678)
    Database-->>API: ConcurrencyException
    API-->>UserA: 409 Conflict<br/>"Document was modified"
    
    Note over UserA,UserB: Optimistic Locking Prevents Lost Updates
```

```mermaid
sequenceDiagram
    participant UserA
    participant UserB
    participant API
    participant LockService
    participant ReportService

    Note over UserA,UserB: Single-User Execution Scenario
    
    UserA->>API: POST /api/Reports/generate-monthly-report
    API->>LockService: AcquireLock("monthly-report")
    LockService-->>API: Lock Acquired
    API->>ReportService: Generate Report (Long Operation)
    
    UserB->>API: POST /api/Reports/generate-monthly-report
    API->>LockService: AcquireLock("monthly-report")
    LockService-->>API: Lock Already Held
    API-->>UserB: 409 Conflict<br/>"Report generation in progress"
    
    ReportService-->>API: Report Complete
    API->>LockService: ReleaseLock("monthly-report")
    LockService-->>API: Lock Released
    API-->>UserA: 200 OK + Report
    
    Note over UserA,UserB: Distributed Locking Prevents Concurrent Execution
```

## 6. Database Schema Relationships

This diagram shows the entity relationships and tenant isolation strategy.

```mermaid
erDiagram
    TENANT ||--o{ PROJECT : "has"
    TENANT ||--o{ SUPPLIER : "has"
    TENANT ||--o{ WAYBILL : "has"
    
    PROJECT ||--o{ WAYBILL : "contains"
    SUPPLIER ||--o{ WAYBILL : "delivers"
    
    TENANT {
        string Id PK
        string Name
        DateTime CreatedAt
    }
    
    PROJECT {
        string Id PK
        string TenantId FK
        string Name
        DateTime CreatedAt
    }
    
    SUPPLIER {
        string Id PK
        string TenantId PK
        string Name
        DateTime CreatedAt
    }
    
    WAYBILL {
        string Id PK
        string TenantId FK
        string ProjectId FK
        string SupplierId FK
        date WaybillDate
        date DeliveryDate
        decimal Quantity
        decimal TotalAmount
        int Status
        byte[] Version
        DateTime CreatedAt
        DateTime UpdatedAt
    }
    
    Note right of SUPPLIER: Composite Primary Key<br/>(TenantId, Id)<br/>Allows same supplier ID<br/>across tenants
```

## 7. Component Interaction Diagram

This diagram shows how all major components interact in the system.

```mermaid
graph TB
    subgraph ClientLayer[Client Layer]
        WebApp[Web Application]
        API[API Clients]
    end
    
    subgraph APILayer[API Layer]
        Middleware[TenantMiddleware]
        Controllers[Controllers<br/>Waybills, Import, Suppliers, Reports]
    end
    
    subgraph ServiceLayer[Service Layer]
        ImportService[WaybillImportService]
        ValidationService[WaybillValidationService]
        WaybillService[WaybillService]
        TenantService[TenantService]
        LockService[DistributedLockService]
    end
    
    subgraph DataLayer[Data Layer]
        DbContext[ApplicationDbContext<br/>Global Query Filters]
        Database[(SQL Server)]
    end
    
    subgraph EventLayer[Event Layer]
        Publisher[MessagePublisher]
        Consumer[MessageConsumer]
        RabbitMQ[(RabbitMQ)]
    end
    
    WebApp --> Middleware
    API --> Middleware
    Middleware --> Controllers
    Controllers --> ImportService
    Controllers --> WaybillService
    Controllers --> LockService
    
    ImportService --> ValidationService
    ImportService --> DbContext
    ImportService --> Publisher
    
    WaybillService --> DbContext
    WaybillService --> TenantService
    
    Controllers --> TenantService
    DbContext --> TenantService
    
    DbContext --> Database
    
    Publisher --> RabbitMQ
    RabbitMQ --> Consumer
    Consumer --> Database
    
    style Middleware fill:#ffcccc
    style DbContext fill:#ccffcc
    style RabbitMQ fill:#ccccff
```

## 8. CSV Processing Pipeline Detail

Detailed view of CSV import processing steps.

```mermaid
flowchart TD
    Start([CSV File Upload]) --> ReadStream[Read File Stream<br/>UTF-8 Encoding]
    ReadStream --> CsvHelper[CsvHelper Configuration<br/>Auto-detect Delimiter<br/>Hebrew Text Support]
    
    CsvHelper --> ParseHeader[Parse Header Row<br/>Map Column Names]
    ParseHeader --> ParseRow[Parse Data Rows<br/>Create ImportWaybillDto]
    
    ParseRow --> ValidateRequired[Validate Required Fields<br/>waybill_id, dates, project_id, etc.]
    ValidateRequired --> ValidateTypes[Validate Data Types<br/>Dates, Decimals, Enum]
    ValidateTypes --> ValidateBusiness[Validate Business Rules<br/>Quantity 0.5-50, Price Calc, Dates]
    ValidateBusiness --> ValidateTenant[Validate Tenant ID Match<br/>CSV tenant_id = Header tenant_id]
    
    ValidateTenant --> Valid{All<br/>Valid?}
    Valid -->|No| AddError[Add to Error List<br/>Continue Processing]
    Valid -->|Yes| EnsureTenant[Ensure Tenant Exists<br/>Create if Missing]
    
    EnsureTenant --> EnsureProject[Ensure Project Exists<br/>Create if Missing]
    EnsureProject --> EnsureSupplier[Ensure Supplier Exists<br/>Create if Missing]
    
    EnsureSupplier --> Upsert[Upsert Waybill<br/>Match: Id + SupplierId + DeliveryDate]
    Upsert --> NextRow{More<br/>Rows?}
    
    AddError --> NextRow
    NextRow -->|Yes| ParseRow
    NextRow -->|No| Commit[Commit Transaction<br/>All or Nothing]
    
    Commit --> PublishEvent[Publish Event<br/>WaybillsImportedEvent]
    PublishEvent --> Return[Return Results<br/>Success Count, Errors, Warnings]
    Return --> End([Complete])
    
    style ValidateTenant fill:#ffcccc
    style Valid fill:#ffffcc
    style Commit fill:#ccffcc
    style PublishEvent fill:#ccccff
```

## Key Architectural Decisions

### 1. Global Query Filters
**Decision**: Use EF Core global query filters for tenant isolation
**Rationale**: Automatic filtering at database level prevents accidental cross-tenant queries
**Trade-off**: Slightly more complex DbContext setup, but provides defense-in-depth security

### 2. Composite Primary Key for Suppliers
**Decision**: Use (TenantId, Id) composite key for Supplier entity
**Rationale**: Allows different tenants to have suppliers with same ID (e.g., shared suppliers)
**Trade-off**: More complex foreign key relationships, but enables realistic multi-tenant scenarios

### 3. Event-Driven Architecture
**Decision**: Use RabbitMQ for asynchronous event processing
**Rationale**: Decouples import processing from downstream actions (notifications, statistics)
**Trade-off**: Additional infrastructure, but enables scalability and flexibility

### 4. In-Memory Distributed Locking
**Decision**: Start with in-memory locking, upgradeable to Redis
**Rationale**: Simple for single-instance deployment, can upgrade to Redis for multi-instance
**Trade-off**: Doesn't work across multiple API instances, but sufficient for assignment scope

### 5. String Enum Serialization
**Decision**: Use JsonStringEnumConverter for API enum serialization
**Rationale**: More readable API responses, easier frontend integration
**Trade-off**: Slightly larger JSON payloads, but better developer experience
