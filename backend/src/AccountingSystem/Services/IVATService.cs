using System;
using System.Collections.Generic;
using System.Linq;
using AccountingSystem.Models;

namespace AccountingSystem.Services
{
    public interface IVATService
    {
        decimal CalculateVAT(decimal amount, decimal vatRate);
        decimal CalculateAmountWithoutVAT(decimal amountWithVAT, decimal vatRate);
        decimal CalculateAmountWithVAT(decimal amountWithoutVAT, decimal vatRate);
        IEnumerable<VATRate> GetAvailableVATRates();
        VATRate GetVATRateByRate(decimal rate);
    }

    public class VATService : IVATService
    {
        private readonly List<VATRate> _vatRates;

        public VATService()
        {
            // Aliquote IVA italiane standard
            _vatRates = new List<VATRate>
            {
                new VATRate { Id = Guid.NewGuid(), Name = "Aliquota ordinaria", Rate = 22m },
                new VATRate { Id = Guid.NewGuid(), Name = "Aliquota ridotta", Rate = 10m },
                new VATRate { Id = Guid.NewGuid(), Name = "Aliquota ridotta minima", Rate = 5m },
                new VATRate { Id = Guid.NewGuid(), Name = "Aliquota ridotta minima", Rate = 4m },
                new VATRate { Id = Guid.NewGuid(), Name = "Esente IVA", Rate = 0m }
            };
        }

        public decimal CalculateVAT(decimal amount, decimal vatRate)
        {
            if (amount < 0)
                throw new ArgumentException("L'importo non può essere negativo", nameof(amount));

            if (vatRate < 0 || vatRate > 100)
                throw new ArgumentException("L'aliquota IVA deve essere compresa tra 0 e 100", nameof(vatRate));

            return Math.Round(amount * vatRate / 100, 2);
        }

        public decimal CalculateAmountWithoutVAT(decimal amountWithVAT, decimal vatRate)
        {
            if (amountWithVAT < 0)
                throw new ArgumentException("L'importo non può essere negativo", nameof(amountWithVAT));

            if (vatRate < 0 || vatRate > 100)
                throw new ArgumentException("L'aliquota IVA deve essere compresa tra 0 e 100", nameof(vatRate));

            return Math.Round(amountWithVAT / (1 + vatRate / 100), 2);
        }

        public decimal CalculateAmountWithVAT(decimal amountWithoutVAT, decimal vatRate)
        {
            if (amountWithoutVAT < 0)
                throw new ArgumentException("L'importo non può essere negativo", nameof(amountWithoutVAT));

            if (vatRate < 0 || vatRate > 100)
                throw new ArgumentException("L'aliquota IVA deve essere compresa tra 0 e 100", nameof(vatRate));

            var vat = CalculateVAT(amountWithoutVAT, vatRate);
            return amountWithoutVAT + vat;
        }

        public IEnumerable<VATRate> GetAvailableVATRates()
        {
            return _vatRates.AsReadOnly();
        }

        public VATRate GetVATRateByRate(decimal rate)
        {
            if (rate < 0 || rate > 100)
                throw new ArgumentException("L'aliquota IVA deve essere compresa tra 0 e 100", nameof(rate));

            var vatRate = _vatRates.FirstOrDefault(r => r.Rate == rate);
            if (vatRate == null)
                throw new InvalidOperationException($"Aliquota IVA con valore '{rate}' non trovata");

            return vatRate;
        }
    }
}