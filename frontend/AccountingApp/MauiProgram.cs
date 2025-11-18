using AccountingApp.Services;
using AccountingApp.ViewModels;
using AccountingApp.Views;
using Android.Net;
using Microsoft.Extensions.Logging;

namespace AccountingApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // ========== SERVICES ==========
        builder.Services.AddSingleton<ApiService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ApiService>>();
            return new ApiService("https://localhost:7001", logger);
        });

        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<AccountService>();
        builder.Services.AddSingleton<SalesService>();
        builder.Services.AddSingleton<PurchaseService>();
        builder.Services.AddSingleton<InvoiceService>();
        builder.Services.AddSingleton<InventoryService>();
        builder.Services.AddSingleton<CustomerService>();
        builder.Services.AddSingleton<SupplierService>();
        builder.Services.AddSingleton<VatRateService>();

        // ========== VIEWMODELS ==========
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<SalesViewModel>();
        builder.Services.AddTransient<PurchasesViewModel>();
        builder.Services.AddTransient<InvoicesViewModel>();
        builder.Services.AddTransient<InventoryViewModel>();
        builder.Services.AddTransient<CustomersViewModel>();
        builder.Services.AddTransient<SuppliersViewModel>();

        // ========== VIEWS ==========
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<SalesPage>();
        builder.Services.AddTransient<PurchasesPage>();
        builder.Services.AddTransient<InvoicesPage>();
        builder.Services.AddTransient<InventoryPage>();
        builder.Services.AddTransient<CustomersPage>();
        builder.Services.AddTransient<SuppliersPage>();

        return builder.Build();
    }
}