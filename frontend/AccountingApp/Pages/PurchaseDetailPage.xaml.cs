using AccountingApp.ViewModels;

namespace AccountingApp.Pages;

public partial class PurchaseDetailPage : ContentPage
{
    private readonly PurchaseDetailViewModel _viewModel;

    public PurchaseDetailPage(PurchaseDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}