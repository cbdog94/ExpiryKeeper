namespace MedicineExpiration.Api.Services;

// TODO: Replace with real drug database API implementation
public class StubDrugDatabaseService : IDrugDatabaseService
{
    public Task<DrugInfo?> LookupByBarcodeAsync(string barcode, CancellationToken ct = default)
        => Task.FromResult<DrugInfo?>(null);
}
