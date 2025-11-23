using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace AccountingApp.Services.Core;

public class AlertService : IAlertService
{
    public async Task ShowAlertAsync(string title, string message, string cancel = "OK")
    {
        var page = Application.Current?.Windows[0]?.Page;
        if (page != null)
        {
            await page.DisplayAlert(title, message, cancel);
        }
    }

    public async Task<bool> ShowConfirmAsync(string title, string message, string accept = "Sì", string cancel = "No")
    {
        var page = Application.Current?.Windows[0]?.Page;
        if (page != null)
        {
            return await page.DisplayAlert(title, message, accept, cancel);
        }
        return false;
    }

    public async Task ShowToastAsync(string message)
    {
        var toast = Toast.Make(message, ToastDuration.Short);
        await toast.Show();
    }
}