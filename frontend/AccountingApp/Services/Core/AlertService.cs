using Android.Widget;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace AccountingApp.Services;

public class AlertService : IAlertService
{
    public async Task ShowAlertAsync(string title, string message, string cancel = "OK")
    {
        if (Application.Current?.MainPage != null)
        {
            await Application.Current.MainPage.DisplayAlert(title, message, cancel);
        }
    }

    public async Task<bool> ShowConfirmAsync(string title, string message, string accept = "Sì", string cancel = "No")
    {
        if (Application.Current?.MainPage != null)
        {
            return await Application.Current.MainPage.DisplayAlert(title, message, accept, cancel);
        }
        return false;
    }

    public async Task ShowToastAsync(string message)
    {
        var toast = Toast.Make(message, ToastDuration.Short);
        await toast.Show();
    }
}