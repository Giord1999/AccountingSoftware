using AccountingApp.ViewModels;

namespace AccountingApp.Pages;

public partial class SupplierDetailPage : ContentPage
{
    private readonly SupplierDetailViewModel _viewModel;

    public SupplierDetailPage(SupplierDetailViewModel viewModel)
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