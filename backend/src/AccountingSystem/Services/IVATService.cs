using System;
using System.Collections.Generic;
using System.Linq;
using AccountingSystem.Models;

namespace AccountingSystem.Services
{
    public interface IVatService
    {
        decimal CalculateVAT(decimal amount, decimal vatRate);
        decimal CalculateAmountWithoutVAT(decimal amountWithVAT, decimal vatRate);
        decimal CalculateAmountWithVAT(decimal amountWithoutVAT, decimal vatRate);
        IEnumerable<VatRate> GetAvailableVATRates();
        VatRate GetVATRateByRate(decimal rate);
        Task ApplyVatToJournalAsync(JournalEntry journalEntry, Guid companyId, string userId);
    }
}