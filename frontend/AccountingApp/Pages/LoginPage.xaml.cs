using AccountingApp.ViewModels;

namespace AccountingApp.Pages;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;

    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Auto-fill for development (remove in production)
#if DEBUG
        _viewModel.Email = "admin@test.com";
        _viewModel.Password = "password";
#endif
    }
}