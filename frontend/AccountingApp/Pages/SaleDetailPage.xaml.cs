using AccountingApp.ViewModels;

namespace AccountingApp.Pages;

public partial class SaleDetailPage : ContentPage
{
    private readonly SaleDetailViewModel _viewModel;

    public SaleDetailPage(SaleDetailViewModel viewModel)
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