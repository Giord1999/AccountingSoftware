using System.ComponentModel.DataAnnotations;

namespace AccountingApp.Models;

// ========== AUTHENTICATION ==========
public record LoginRequest(
    [Required] string Email,
    [Required] string Password
);

public record LoginResponse(
    string Token,
    string UserId,
    string DisplayName,
    string Email,
    List<string> Roles,
    Guid? CompanyId
);

// ========== ACCOUNT ==========
public class Account
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public AccountCategory Category { get; set; }
    public Guid CompanyId { get; set; }
    public string Currency { get; set; } = "EUR";
    public Guid? ParentAccountId { get; set; }
    public bool IsPostedRestricted { get; set; }
    public DateTime CreatedAt { get; set; }
    public required string CreatedBy { get; set; }
}

public enum AccountCategory
{
    Asset = 0,
    Liability = 1,
    Equity = 2,
    Revenue = 3,
    Expense = 4
}

// ========== JOURNAL ENTRY ==========
public class JournalEntry
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid PeriodId { get; set; }
    public required string Description { get; set; }
    public DateTime Date { get; set; }
    public JournalStatus Status { get; set; }
    public string Currency { get; set; } = "EUR";
    public decimal ExchangeRate { get; set; } = 1.0m;
    public string? Reference { get; set; }
    public List<JournalLine> Lines { get; set; } = new();
}

public class JournalLine
{
    public Guid Id { get; set; }
    public Guid JournalEntryId { get; set; }
    public Guid AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public required string Narrative { get; set; }
}

public enum JournalStatus
{
    Draft = 0,
    Posted = 1,
    Reversed = 2
}

// ========== SALES ==========
public record CreateSaleRequest(
    Guid CompanyId,
    Guid PeriodId,
    Guid InventoryId,
    Guid VatRateId,
    decimal Quantity,
    decimal UnitPrice,
    string CustomerName,
    string? CustomerVatNumber,
    Guid? ClientiAccountId = null,
    Guid? VenditeAccountId = null,
    Guid? IvaDebitoAccountId = null
);

public class Sale
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid PeriodId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid? JournalEntryId { get; set; }
    public Guid CustomerId { get; set; }
    public required string CustomerName { get; set; }
    public string? CustomerVatNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TotalVat { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTime SaleDate { get; set; }
    public SaleStatus Status { get; set; }
    public string? Notes { get; set; }
}

public enum SaleStatus
{
    Draft = 0,
    Confirmed = 1,
    Invoiced = 2,
    Posted = 3,
    Cancelled = 4
}

// ========== PURCHASE ==========
public record CreatePurchaseRequest(
    Guid CompanyId,
    Guid PeriodId,
    Guid InventoryId,
    Guid VatRateId,
    decimal Quantity,
    decimal UnitPrice,
    string SupplierName,
    string? SupplierVatNumber,
    Guid? FornitoriAccountId = null,
    Guid? AcquistiAccountId = null,
    Guid? IvaCreditoAccountId = null
);

public class Purchase
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid PeriodId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid? JournalEntryId { get; set; }
    public required string SupplierName { get; set; }
    public string? SupplierVatNumber { get; set; }
    public Guid? SupplierId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TotalVat { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTime PurchaseDate { get; set; }
    public PurchaseStatus Status { get; set; }
    public string? Notes { get; set; }
}

public enum PurchaseStatus
{
    Draft = 0,
    Confirmed = 1,
    Invoiced = 2,
    Posted = 3,
    Cancelled = 4
}

// ========== INVENTORY ==========
public class Inventory
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string ItemCode { get; set; }
    public required string ItemName { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string UnitOfMeasure { get; set; } = "PZ";
    public decimal QuantityOnHand { get; set; }
    public decimal? ReorderLevel { get; set; }
    public decimal UnitCost { get; set; }
    public decimal? SalePrice { get; set; }
    public string Currency { get; set; } = "EUR";
    public bool IsActive { get; set; } = true;
}

// ========== CUSTOMER ==========
public class Customer
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? VatNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = "IT";
    public CustomerRating Rating { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum CustomerRating
{
    Unrated = 0,
    Bronze = 1,
    Silver = 2,
    Gold = 3,
    Platinum = 4
}

// ========== SUPPLIER ==========
public class Supplier
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? VatNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = "IT";
    public SupplierRating Rating { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum SupplierRating
{
    Unrated = 0,
    Poor = 1,
    Fair = 2,
    Good = 3,
    Excellent = 4
}

// ========== INVOICE ==========
public class Invoice
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string InvoiceNumber { get; set; }
    public InvoiceType Type { get; set; }
    public InvoiceStatus Status { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? DueDate { get; set; }
    public required string CustomerName { get; set; }
    public string? CustomerVatNumber { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TotalVat { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
}

public enum InvoiceType
{
    Sales = 0,
    Purchase = 1
}

public enum InvoiceStatus
{
    Draft = 0,
    Issued = 1,
    Posted = 2,
    Paid = 3,
    PartiallyPaid = 4,
    Overdue = 5,
    Cancelled = 6
}

// ========== LEAD ==========
public class Lead
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public LeadSource Source { get; set; }
    public LeadStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum LeadSource
{
    Website = 0,
    Referral = 1,
    Campaign = 2,
    Event = 3,
    Other = 4
}

public enum LeadStatus
{
    New = 0,
    Contacted = 1,
    Qualified = 2,
    Converted = 3,
    Lost = 4
}

// ========== BI DASHBOARD ==========
public class BIDashboardResult
{
    public BIKPIs KPIs { get; set; } = new();
    public List<ChartData> RevenueChart { get; set; } = new();
    public List<TrendData> RevenueTrend { get; set; } = new();
}

public class BIKPIs
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public decimal ProfitMargin { get; set; }
    public decimal ROI { get; set; }
    public decimal CashFlowRatio { get; set; }
    public decimal AverageMonthlyRevenue { get; set; }
    public decimal AverageMonthlyExpenses { get; set; }
    public int TransactionCount { get; set; }
    public decimal RevenueGrowthPercentage { get; set; }
}

public class ChartData
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class TrendData
{
    public DateTime Period { get; set; }
    public decimal Value { get; set; }
}