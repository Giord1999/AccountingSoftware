using AccountingApp.ViewModels;

namespace AccountingApp.Pages;

public partial class AccountDetailPage : ContentPage
{
    private readonly AccountDetailViewModel _viewModel;

    public AccountDetailPage(AccountDetailViewModel viewModel)
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