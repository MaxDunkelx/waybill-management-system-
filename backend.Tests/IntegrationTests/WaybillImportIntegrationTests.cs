using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WaybillManagementSystem.Data;
using WaybillManagementSystem.DTOs;
using WaybillManagementSystem.Services;
using Xunit;

namespace WaybillManagementSystem.Tests.IntegrationTests;

/// <summary>
/// Integration tests for waybill CSV import functionality.
/// 
/// PURPOSE:
/// These tests verify that CSV import works correctly, including:
/// - CSV parsing with Hebrew text
/// - Data validation
/// - Tenant ID validation
/// - Database operations (upsert logic)
/// </summary>
public class WaybillImportIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IWaybillImportService _importService;
    private readonly IWaybillValidationService _validationService;
    private readonly ITenantService _tenantService;
    private const string TestTenantId = "TEST_TENANT_IMPORT";

    public WaybillImportIntegrationTests()
    {
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_Import_{Guid.NewGuid()}")
            .Options;

        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();

        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var httpContextAccessor = new Microsoft.AspNetCore.Http.HttpContextAccessor();
        var tenantService = new TenantService(httpContextAccessor, loggerFactory.CreateLogger<TenantService>());

        _dbContext = new ApplicationDbContext(options, tenantService);
        _tenantService = tenantService;
        _validationService = new WaybillValidationService(loggerFactory.CreateLogger<WaybillValidationService>());

        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "RabbitMQ:HostName", "localhost" },
                { "RabbitMQ:Port", "5672" },
                { "RabbitMQ:UserName", "guest" },
                { "RabbitMQ:Password", "guest" },
                { "RabbitMQ:VirtualHost", "/" }
            }!)
            .Build();

        var messagePublisher = new MessagePublisher(
            configuration,
            loggerFactory.CreateLogger<MessagePublisher>());

        var cacheService = new MemoryCacheService(
            new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()),
            loggerFactory.CreateLogger<MemoryCacheService>());

        _importService = new WaybillImportService(
            loggerFactory.CreateLogger<WaybillImportService>(),
            _validationService,
            _dbContext,
            tenantService,
            messagePublisher,
            cacheService);

        // Create test tenant
        _dbContext.Tenants.Add(new Models.Tenant
        {
            Id = TestTenantId,
            Name = "Test Tenant",
            CreatedAt = DateTime.UtcNow
        });
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task ImportCsv_ValidData_CreatesWaybills()
    {
        // Arrange
        var csvContent = "waybill_id,waybill_date,delivery_date,project_id,project_name,supplier_id,supplier_name,product_code,product_name,quantity,unit,unit_price,total_amount,currency,status,vehicle_number,driver_name,delivery_address,notes,created_at,tenant_id\n" +
                         "WB-TEST-001,2024-09-01,2024-09-01,PRJ-TEST,Test Project,SUP-TEST,Test Supplier,B30,בטון ב-30,10.0,מ\"ק,450.0,4500.0,ILS,DELIVERED,123-45-67,יוסי כהן,Test Address,Test Notes,2024-09-01T08:00:00Z," + TestTenantId;
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _importService.ImportFromCsvAsync(stream, TestTenantId);

        // Assert
        result.SuccessCount.Should().Be(1);
        result.ErrorCount.Should().Be(0);
        result.ParsedWaybills.Should().HaveCount(1);

        // Verify waybill was created in database
        var waybill = await _dbContext.Waybills
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.Id == "WB-TEST-001");
        waybill.Should().NotBeNull();
        waybill!.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public async Task ImportCsv_TenantIdMismatch_ReturnsError()
    {
        // Arrange
        var csvContent = "waybill_id,waybill_date,delivery_date,project_id,project_name,supplier_id,supplier_name,product_code,product_name,quantity,unit,unit_price,total_amount,currency,status,vehicle_number,driver_name,delivery_address,notes,created_at,tenant_id\n" +
                         "WB-TEST-002,2024-09-01,2024-09-01,PRJ-TEST,Test Project,SUP-TEST,Test Supplier,B30,בטון ב-30,10.0,מ\"ק,450.0,4500.0,ILS,DELIVERED,123-45-67,יוסי כהן,Test Address,Test Notes,2024-09-01T08:00:00Z,DIFFERENT_TENANT";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _importService.ImportFromCsvAsync(stream, TestTenantId);

        // Assert
        result.ErrorCount.Should().BeGreaterThan(0);
        result.Errors.Should().Contain(e => e.Message.Contains("Tenant ID mismatch"));
    }

    [Fact]
    public async Task ImportCsv_InvalidQuantity_ReturnsError()
    {
        // Arrange
        var csvContent = "waybill_id,waybill_date,delivery_date,project_id,project_name,supplier_id,supplier_name,product_code,product_name,quantity,unit,unit_price,total_amount,currency,status,vehicle_number,driver_name,delivery_address,notes,created_at,tenant_id\n" +
                         "WB-TEST-003,2024-09-01,2024-09-01,PRJ-TEST,Test Project,SUP-TEST,Test Supplier,B30,בטון ב-30,0.3,מ\"ק,450.0,135.0,ILS,DELIVERED,123-45-67,יוסי כהן,Test Address,Test Notes,2024-09-01T08:00:00Z," + TestTenantId;
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _importService.ImportFromCsvAsync(stream, TestTenantId);

        // Assert
        result.ErrorCount.Should().BeGreaterThan(0);
        result.Errors.Should().Contain(e => e.Message.Contains("quantity") || e.Message.Contains("0.5"));
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}
