using AccountingApp.Services.Core;
using AccountingApp.Services.Api;
using AccountingApp.ViewModels;
using AccountingApp.Pages;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace AccountingApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ========== BLAZOR WEBVIEW ==========
        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // ========== SERVIZI HTTP ==========
        builder.Services.AddSingleton(sp => new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7001/api/"),
            Timeout = TimeSpan.FromSeconds(30)
        });

        // ========== CORE SERVICES ==========
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<IAlertService, AlertService>();

        // ========== API SERVICES ==========
        builder.Services.AddTransient<IAccountService, AccountService>();
        builder.Services.AddTransient<IAccountingService, AccountingService>();
        builder.Services.AddTransient<ISalesApiService, SalesApiService>();
        builder.Services.AddTransient<IPurchaseApiService, PurchaseApiService>();
        builder.Services.AddTransient<IInventoryApiService, InventoryApiService>();
        builder.Services.AddTransient<ICustomerApiService, CustomerApiService>();
        builder.Services.AddTransient<ISupplierApiService, SupplierApiService>();
        builder.Services.AddTransient<IInvoiceApiService, InvoiceApiService>();
        builder.Services.AddTransient<ILeadApiService, LeadApiService>();
        builder.Services.AddTransient<IBIApiService, BIApiService>();
        // Servizi mancanti
        builder.Services.AddTransient<IVatRateApiService, VatRateApiService>();
        builder.Services.AddTransient<IAccountingPeriodApiService, AccountingPeriodApiService>();
        builder.Services.AddTransient<IAccountingPeriodService, AccountingPeriodService>();

        // ========== SERVICE AGGREGATORS ==========
        // PurchaseViewModelServices - aggrega servizi per CreatePurchaseViewModel
        builder.Services.AddTransient(sp => new PurchaseViewModelServices
        {
            PurchaseService = sp.GetRequiredService<IPurchaseApiService>(),
            InventoryService = sp.GetRequiredService<IInventoryApiService>(),
            SupplierService = sp.GetRequiredService<ISupplierApiService>(),
            PeriodService = sp.GetRequiredService<IAccountingPeriodApiService>(),
            VatRateService = sp.GetRequiredService<IVatRateApiService>()
        });

        // SaleViewModelServices - aggrega servizi per CreateSaleViewModel
        builder.Services.AddTransient(sp => new SaleViewModelServices
        {
            SalesService = sp.GetRequiredService<ISalesApiService>(),
            InventoryService = sp.GetRequiredService<IInventoryApiService>(),
            CustomerService = sp.GetRequiredService<ICustomerApiService>(),
            PeriodService = sp.GetRequiredService<IAccountingPeriodApiService>(),
            VatRateService = sp.GetRequiredService<IVatRateApiService>(),
            AuthService = sp.GetRequiredService<IAuthService>(),
            AlertService = sp.GetRequiredService<IAlertService>(),
            NavigationService = sp.GetRequiredService<INavigationService>()
        });

        // ========== VIEWMODELS ==========
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<AccountsViewModel>();
        builder.Services.AddTransient<JournalEntryViewModel>();
        builder.Services.AddTransient<SalesViewModel>();
        builder.Services.AddTransient<PurchasesViewModel>();
        builder.Services.AddTransient<InventoryViewModel>();
        builder.Services.AddTransient<CustomersViewModel>();
        builder.Services.AddTransient<SuppliersViewModel>();
        builder.Services.AddTransient<InvoicesViewModel>();
        builder.Services.AddTransient<LeadsViewModel>();
        builder.Services.AddTransient<BIReportsViewModel>();
        // ViewModels mancanti
        builder.Services.AddTransient<AccountDetailViewModel>();
        builder.Services.AddTransient<CreatePurchaseViewModel>();
        builder.Services.AddTransient<CreateSaleViewModel>();
        builder.Services.AddTransient<PurchaseDetailViewModel>();
        builder.Services.AddTransient<SaleDetailViewModel>();

        // ========== PAGES ==========
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<AccountsPage>();
        builder.Services.AddTransient<JournalEntryPage>();
        builder.Services.AddTransient<SalesPage>();
        builder.Services.AddTransient<PurchasesPage>();
        builder.Services.AddTransient<InventoryPage>();
        builder.Services.AddTransient<CustomersPage>();
        builder.Services.AddTransient<SuppliersPage>();
        builder.Services.AddTransient<InvoicesPage>();
        builder.Services.AddTransient<LeadsPage>();
        builder.Services.AddTransient<BIReportsPage>();
        // Pages mancanti
        builder.Services.AddTransient<AccountDetailPage>();
        builder.Services.AddTransient<CreatePurchasePage>();
        builder.Services.AddTransient<CreateSalePage>();

        return builder.Build();
    }
}

/// <summary>
/// Raggruppa i servizi API necessari per la creazione di un acquisto.
/// </summary>
public class PurchaseViewModelServices
{
    public required IPurchaseApiService PurchaseService { get; init; }
    public required IInventoryApiService InventoryService { get; init; }
    public required ISupplierApiService SupplierService { get; init; }
    public required IAccountingPeriodApiService PeriodService { get; init; }
    public required IVatRateApiService VatRateService { get; init; }
}