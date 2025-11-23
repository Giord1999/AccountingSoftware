using AccountingApp.Services;
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

        // ========== SERVIZI HTTP ==========
        builder.Services.AddSingleton(sp => new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7001/api/"),
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Core Services
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<IAlertService, AlertService>();

        // API Services
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

        // ViewModels
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

        // Pages
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

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
