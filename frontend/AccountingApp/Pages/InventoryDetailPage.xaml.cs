using AccountingApp.ViewModels;

namespace AccountingApp.Pages;

public partial class InventoryDetailPage : ContentPage
{
    private readonly InventoryDetailViewModel _viewModel;

    public InventoryDetailPage(InventoryDetailViewModel viewModel)
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