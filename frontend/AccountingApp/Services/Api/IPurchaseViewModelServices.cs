namespace AccountingApp.Services.Api;

public record PurchaseViewModelServices(
    IPurchaseApiService PurchaseService,
    IInventoryApiService InventoryService,
    ISupplierApiService SupplierService,
    IAccountingPeriodApiService PeriodService,
    IVatRateApiService VatRateService);