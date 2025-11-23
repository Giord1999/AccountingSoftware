using AccountingApp.ViewModels;

namespace AccountingApp.Pages;

public partial class PurchasesPage : ContentPage
{
    private readonly PurchasesViewModel _viewModel;

    public PurchasesPage(PurchasesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadPurchasesCommand.ExecuteAsync(null);
    }
}