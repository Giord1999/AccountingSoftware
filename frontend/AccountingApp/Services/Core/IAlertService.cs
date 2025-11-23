namespace AccountingApp.Services.Core;

public interface IAlertService
{
    Task ShowAlertAsync(string title, string message, string cancel = "OK");
    Task<bool> ShowConfirmAsync(string title, string message, string accept = "Sì", string cancel = "No");
    Task ShowToastAsync(string message);
}