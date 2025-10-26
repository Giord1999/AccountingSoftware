namespace AccountingSystem.Services;
public interface IFXService
{
    Task<decimal> ConvertAsync(string fromCurrency, string toCurrency, decimal amount, DateTime? date = null);
    Task RevaluateAccountsAsync(Guid companyId, DateTime asOfDate, string userId);
}
