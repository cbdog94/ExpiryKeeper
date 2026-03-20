namespace MedicineExpiration.Api.Services;

public record DrugInfo(string Name, string? Manufacturer, string? Specification);

public interface IDrugDatabaseService
{
    Task<DrugInfo?> LookupByBarcodeAsync(string barcode, CancellationToken ct = default);
}
