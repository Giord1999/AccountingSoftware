namespace AccountingApp.Services.Core;

public interface INavigationService
{
    Task NavigateToAsync(string route);
    Task NavigateBackAsync();
    Task NavigateToLoginAsync();
    Task NavigateToDashboardAsync();
}