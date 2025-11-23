using AccountingApp.ViewModels;

namespace AccountingApp.Pages;

public partial class CustomersPage : ContentPage
{
    private readonly CustomersViewModel _viewModel;

    public CustomersPage(CustomersViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCustomersCommand.ExecuteAsync(null);
    }
}