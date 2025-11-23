using AccountingApp.ViewModels;

namespace AccountingApp.Pages;

public partial class JournalEntryPage : ContentPage
{
    private readonly JournalEntryViewModel _viewModel;

    public JournalEntryPage(JournalEntryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAccountsCommand.ExecuteAsync(null);
        
        // Add first line automatically
        if (_viewModel.Lines.Count == 0)
        {
            _viewModel.AddLineCommand.Execute(null);
            _viewModel.AddLineCommand.Execute(null);
        }
    }
}