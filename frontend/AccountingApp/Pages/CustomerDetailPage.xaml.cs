using AccountingApp.ViewModels;

namespace AccountingApp.Pages;

public partial class CustomerDetailPage : ContentPage
{
    private readonly CustomerDetailViewModel _viewModel;

    public CustomerDetailPage(CustomerDetailViewModel viewModel)
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