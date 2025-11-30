using AccountingApp.ViewModels;

namespace AccountingApp.Pages;

public partial class CreateSalePage : ContentPage
{
    private readonly CreateSaleViewModel _viewModel;

    public CreateSalePage(CreateSaleViewModel viewModel)
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