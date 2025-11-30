using AccountingApp.ViewModels;

namespace AccountingApp.Pages;

public partial class InvoiceDetailPage : ContentPage
{
    private readonly InvoiceDetailViewModel _viewModel;

    public InvoiceDetailPage(InvoiceDetailViewModel viewModel)
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