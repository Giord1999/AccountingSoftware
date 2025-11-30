using AccountingApp.ViewModels;

namespace AccountingApp.Pages;

public partial class LeadDetailPage : ContentPage
{
    private readonly LeadDetailViewModel _viewModel;

    public LeadDetailPage(LeadDetailViewModel viewModel)
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