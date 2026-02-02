using FluentAssertions;
using Microsoft.Extensions.Logging;
using WaybillManagementSystem.DTOs;
using WaybillManagementSystem.Services;
using Xunit;

namespace WaybillManagementSystem.Tests.UnitTests;

/// <summary>
/// Unit tests for WaybillValidationService.
/// 
/// PURPOSE:
/// These tests verify that all business rule validations work correctly,
/// including required fields, data types, quantity validation, price calculation, date validation, and duplicate detection.
/// </summary>
public class WaybillValidationServiceTests
{
    private readonly IWaybillValidationService _validationService;
    private readonly ILogger<WaybillValidationService> _logger;

    public WaybillValidationServiceTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<WaybillValidationService>();
        _validationService = new WaybillValidationService(_logger);
    }

    [Fact]
    public void ValidateWaybill_ValidData_ReturnsNoErrors()
    {
        // Arrange
        var dto = new ImportWaybillDto
        {
            WaybillId = "WB-001",
            WaybillDate = "2024-09-01",
            DeliveryDate = "2024-09-01",
            ProjectId = "PRJ001",
            SupplierId = "SUP001",
            ProductCode = "B30",
            ProductName = "בטון ב-30",
            Quantity = "12.5",
            Unit = "מ\"ק",
            UnitPrice = "450.0",
            TotalAmount = "5625.0",
            Currency = "ILS",
            Status = "DELIVERED",
            DeliveryAddress = "Test Address"
        };
        var existingWaybillIds = new HashSet<string>();

        // Act
        var errors = _validationService.ValidateWaybill(dto, 1, "test row", existingWaybillIds);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateWaybill_QuantityBelowMinimum_ReturnsError()
    {
        // Arrange
        var dto = new ImportWaybillDto
        {
            WaybillId = "WB-001",
            WaybillDate = "2024-09-01",
            DeliveryDate = "2024-09-01",
            ProjectId = "PRJ001",
            SupplierId = "SUP001",
            ProductCode = "B30",
            ProductName = "בטון ב-30",
            Quantity = "0.3", // Below minimum
            Unit = "מ\"ק",
            UnitPrice = "450.0",
            TotalAmount = "135.0",
            Currency = "ILS",
            Status = "DELIVERED",
            DeliveryAddress = "Test Address"
        };
        var existingWaybillIds = new HashSet<string>();

        // Act
        var errors = _validationService.ValidateWaybill(dto, 1, "test row", existingWaybillIds);

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.Message.Contains("0.5") || e.Message.Contains("quantity"));
    }

    [Fact]
    public void ValidateWaybill_QuantityAboveMaximum_ReturnsError()
    {
        // Arrange
        var dto = new ImportWaybillDto
        {
            WaybillId = "WB-001",
            WaybillDate = "2024-09-01",
            DeliveryDate = "2024-09-01",
            ProjectId = "PRJ001",
            SupplierId = "SUP001",
            ProductCode = "B30",
            ProductName = "בטון ב-30",
            Quantity = "55.0", // Above maximum
            Unit = "מ\"ק",
            UnitPrice = "450.0",
            TotalAmount = "24750.0",
            Currency = "ILS",
            Status = "DELIVERED",
            DeliveryAddress = "Test Address"
        };
        var existingWaybillIds = new HashSet<string>();

        // Act
        var errors = _validationService.ValidateWaybill(dto, 1, "test row", existingWaybillIds);

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.Message.Contains("50") || e.Message.Contains("quantity"));
    }

    [Fact]
    public void ValidateWaybill_PriceCalculationIncorrect_ReturnsError()
    {
        // Arrange
        var dto = new ImportWaybillDto
        {
            WaybillId = "WB-001",
            WaybillDate = "2024-09-01",
            DeliveryDate = "2024-09-01",
            ProjectId = "PRJ001",
            SupplierId = "SUP001",
            ProductCode = "B30",
            ProductName = "בטון ב-30",
            Quantity = "10.0",
            Unit = "מ\"ק",
            UnitPrice = "450.0",
            TotalAmount = "5000.0", // Should be 4500.0
            Currency = "ILS",
            Status = "DELIVERED",
            DeliveryAddress = "Test Address"
        };
        var existingWaybillIds = new HashSet<string>();

        // Act
        var errors = _validationService.ValidateWaybill(dto, 1, "test row", existingWaybillIds);

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.Message.Contains("price calculation") || e.Message.Contains("total_amount"));
    }

    [Fact]
    public void ValidateWaybill_DeliveryBeforeWaybill_ReturnsError()
    {
        // Arrange
        var dto = new ImportWaybillDto
        {
            WaybillId = "WB-001",
            WaybillDate = "2024-09-02",
            DeliveryDate = "2024-09-01", // Before waybill date
            ProjectId = "PRJ001",
            SupplierId = "SUP001",
            ProductCode = "B30",
            ProductName = "בטון ב-30",
            Quantity = "10.0",
            Unit = "מ\"ק",
            UnitPrice = "450.0",
            TotalAmount = "4500.0",
            Currency = "ILS",
            Status = "DELIVERED",
            DeliveryAddress = "Test Address"
        };
        var existingWaybillIds = new HashSet<string>();

        // Act
        var errors = _validationService.ValidateWaybill(dto, 1, "test row", existingWaybillIds);

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.Message.Contains("delivery_date cannot be before waybill_date"));
    }

    [Fact]
    public void ValidateTenantIdMatch_Matching_ReturnsNoErrors()
    {
        // Arrange
        var csvTenantId = "TENANT001";
        var expectedTenantId = "TENANT001";

        // Act
        var errors = _validationService.ValidateTenantIdMatch(csvTenantId, expectedTenantId, 1, "test row");

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateTenantIdMatch_Mismatch_ReturnsError()
    {
        // Arrange
        var csvTenantId = "TENANT002";
        var expectedTenantId = "TENANT001";

        // Act
        var errors = _validationService.ValidateTenantIdMatch(csvTenantId, expectedTenantId, 1, "test row");

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.Message.Contains("Tenant ID mismatch"));
    }
}
