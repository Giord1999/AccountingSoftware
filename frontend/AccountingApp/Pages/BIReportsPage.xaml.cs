using AccountingApp.ViewModels;

namespace AccountingApp.Pages;

public partial class BIReportsPage : ContentPage
{
    private readonly BIReportsViewModel _viewModel;

    public BIReportsPage(BIReportsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDashboardCommand.ExecuteAsync(null);
    }
}