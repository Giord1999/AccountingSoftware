using AccountingApp.Services.Core;

namespace AccountingApp.Services.Core;

public class NavigationService : INavigationService
{
    public async Task NavigateToAsync(string route)
    {
        await Shell.Current.GoToAsync(route);
    }

    public async Task NavigateBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    public async Task NavigateToLoginAsync()
    {
        await Shell.Current.GoToAsync("//login");
    }

    public async Task NavigateToDashboardAsync()
    {
        await Shell.Current.GoToAsync("//dashboard");
    }
}