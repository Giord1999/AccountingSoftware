using AccountingApp.ViewModels;

namespace AccountingApp.Pages;

public partial class SalesPage : ContentPage
{
    private readonly SalesViewModel _viewModel;

    public SalesPage(SalesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadSalesCommand.ExecuteAsync(null);
    }
}