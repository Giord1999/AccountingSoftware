using AccountingApp.ViewModels;

namespace AccountingApp.Pages;

public partial class LeadsPage : ContentPage
{
    private readonly LeadsViewModel _viewModel;

    public LeadsPage(LeadsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadLeadsCommand.ExecuteAsync(null);
    }
}