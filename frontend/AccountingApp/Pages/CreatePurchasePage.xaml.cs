using AccountingApp.ViewModels;

namespace AccountingApp.Pages;

public partial class CreatePurchasePage : ContentPage
{
    private readonly CreatePurchaseViewModel _viewModel;

    public CreatePurchasePage(CreatePurchaseViewModel viewModel)
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