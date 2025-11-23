using AccountingApp.ViewModels;

namespace AccountingApp.Pages;

public partial class SuppliersPage : ContentPage
{
    private readonly SuppliersViewModel _viewModel;

    public SuppliersPage(SuppliersViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadSuppliersCommand.ExecuteAsync(null);
    }
}