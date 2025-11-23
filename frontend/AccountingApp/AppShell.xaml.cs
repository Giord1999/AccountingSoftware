using AccountingApp.Pages;

namespace AccountingApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes for navigation
        Routing.RegisterRoute("accountdetail", typeof(AccountDetailPage));
        Routing.RegisterRoute("createsale", typeof(CreateSalePage));
        Routing.RegisterRoute("saledetail", typeof(SaleDetailPage));
        Routing.RegisterRoute("createpurchase", typeof(CreatePurchasePage));
        Routing.RegisterRoute("purchasedetail", typeof(PurchaseDetailPage));
        Routing.RegisterRoute("inventorydetail", typeof(InventoryDetailPage));
        Routing.RegisterRoute("customerdetail", typeof(CustomerDetailPage));
        Routing.RegisterRoute("supplierdetail", typeof(SupplierDetailPage));
        Routing.RegisterRoute("leaddetail", typeof(LeadDetailPage));
        Routing.RegisterRoute("invoicedetail", typeof(InvoiceDetailPage));
    }
}