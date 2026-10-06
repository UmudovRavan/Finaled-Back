using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AltensorAccounting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAccounting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountingDimensions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingDimensions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ParentAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsLeaf = table.Column<bool>(type: "boolean", nullable: false),
                    IsControlAccount = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accounts_Accounts_ParentAccountId",
                        column: x => x.ParentAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    TaxNumber = table.Column<string>(type: "text", nullable: false),
                    BaseCurrency = table.Column<string>(type: "text", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultReceivableAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultPayableAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultStockAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultGRNIAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultCOGSAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultRetainedEarningsAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultInputVatAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultOutputVatAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultRevenueAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultFXGainLossAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CostLayers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    InitialQty = table.Column<decimal>(type: "numeric", nullable: false),
                    RemainingQty = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric", nullable: false),
                    SourceDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsExhausted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostLayers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    TaxNumber = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    ReceivableAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreditLimit = table.Column<decimal>(type: "numeric", nullable: false),
                    PaymentTermsDays = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FiscalYears",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalYears", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    BaseUOM = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ValuationMethod = table.Column<int>(type: "integer", nullable: false),
                    StandardBuyingPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    StandardSellingPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    CurrentValuationRate = table.Column<decimal>(type: "numeric", nullable: false),
                    InventoryAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    COGSAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevenueAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManualJournals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    JournalNumber = table.Column<string>(type: "text", nullable: false),
                    PostingDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    IsReversal = table.Column<bool>(type: "boolean", nullable: false),
                    ReversalOfJournalId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReversalReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManualJournals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PostingBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNumber = table.Column<string>(type: "text", nullable: false),
                    PostingDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    SourceDocumentType = table.Column<int>(type: "integer", nullable: false),
                    SourceDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceDocumentNumber = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    TotalDebitBase = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalCreditBase = table.Column<decimal>(type: "numeric", nullable: false),
                    IsReversed = table.Column<bool>(type: "boolean", nullable: false),
                    ReversalBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostingBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseRequisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionNumber = table.Column<string>(type: "text", nullable: false),
                    RequiredDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Department = table.Column<string>(type: "text", nullable: true),
                    Purpose = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseRequisitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    TaxNumber = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    BankAccountDetails = table.Column<string>(type: "text", nullable: true),
                    PayableAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentTermsDays = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: true),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankName = table.Column<string>(type: "text", nullable: false),
                    AccountNumber = table.Column<string>(type: "text", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    SwiftCode = table.Column<string>(type: "text", nullable: true),
                    GLAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankAccounts_Accounts_GLAccountId",
                        column: x => x.GLAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CashDesks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    GLAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashDesks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashDesks_Accounts_GLAccountId",
                        column: x => x.GLAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentNumber = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PostingDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: true),
                    PartyType = table.Column<string>(type: "text", nullable: true),
                    BankOrCashAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    UnallocatedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Accounts_BankOrCashAccountId",
                        column: x => x.BankOrCashAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    RatePercent = table.Column<decimal>(type: "numeric", nullable: false),
                    IsInclusive = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TaxAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxCodes_Accounts_TaxAccountId",
                        column: x => x.TaxAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: true),
                    DefaultInventoryAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Warehouses_Accounts_DefaultInventoryAccountId",
                        column: x => x.DefaultInventoryAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CustomerInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "text", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PostingDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DocumentStatus = table.Column<int>(type: "integer", nullable: false),
                    SettlementStatus = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    OutstandingAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerInvoices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountingPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FiscalYearId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PeriodNumber = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsAdjustmentPeriod = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingPeriods_FiscalYears_FiscalYearId",
                        column: x => x.FiscalYearId,
                        principalTable: "FiscalYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemUomConversions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromUOM = table.Column<string>(type: "text", nullable: false),
                    ToUOM = table.Column<string>(type: "text", nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemUomConversions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemUomConversions_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManualJournalLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManualJournalId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Debit = table.Column<decimal>(type: "numeric", nullable: false),
                    Credit = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: true),
                    PartyType = table.Column<string>(type: "text", nullable: true),
                    CostCenterId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManualJournalLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManualJournalLines_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ManualJournalLines_ManualJournals_ManualJournalId",
                        column: x => x.ManualJournalId,
                        principalTable: "ManualJournals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LedgerEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostingBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostingDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    DebitBase = table.Column<decimal>(type: "numeric", nullable: false),
                    CreditBase = table.Column<decimal>(type: "numeric", nullable: false),
                    TransactionCurrency = table.Column<string>(type: "text", nullable: false),
                    TransactionAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: true),
                    PartyType = table.Column<string>(type: "text", nullable: true),
                    SourceDocumentType = table.Column<int>(type: "integer", nullable: false),
                    SourceDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    CostCenterId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    LineDescription = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LedgerEntries_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LedgerEntries_PostingBatches_PostingBatchId",
                        column: x => x.PostingBatchId,
                        principalTable: "PostingBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseRequisitionLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    RequestedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    EstimatedUnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseRequisitionLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitionLines_PurchaseRequisitions_PurchaseRequi~",
                        column: x => x.PurchaseRequisitionId,
                        principalTable: "PurchaseRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "text", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    TermsAndConditions = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BankStatements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    StatementNumber = table.Column<string>(type: "text", nullable: false),
                    StatementDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    ClosingBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalDeposits = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalWithdrawals = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankStatements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankStatements_BankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunNumber = table.Column<string>(type: "text", nullable: false),
                    RunDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BankAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalProposalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRuns_BankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetDocumentType = table.Column<int>(type: "integer", nullable: false),
                    TargetDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetDocumentNumber = table.Column<string>(type: "text", nullable: true),
                    AllocatedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    AllocationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostingDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TransactionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    QtyIn = table.Column<decimal>(type: "numeric", nullable: false),
                    QtyOut = table.Column<decimal>(type: "numeric", nullable: false),
                    ValuationRate = table.Column<decimal>(type: "numeric", nullable: false),
                    IncomingCost = table.Column<decimal>(type: "numeric", nullable: false),
                    BalanceQty = table.Column<decimal>(type: "numeric", nullable: false),
                    BalanceValue = table.Column<decimal>(type: "numeric", nullable: false),
                    SourceDocumentType = table.Column<int>(type: "integer", nullable: false),
                    SourceDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceDocumentNumber = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockLedgerEntries_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockLedgerEntries_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockReconciliations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReconciliationNumber = table.Column<string>(type: "text", nullable: false),
                    ReconciliationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PostingDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalAdjustmentValue = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReconciliations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockReconciliations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionNumber = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PostingDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    SourceWarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceDocumentType = table.Column<int>(type: "integer", nullable: false),
                    SourceDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalValue = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransactions_Warehouses_SourceWarehouseId",
                        column: x => x.SourceWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockTransactions_Warehouses_TargetWarehouseId",
                        column: x => x.TargetWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CustomerCreditNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditNoteNumber = table.Column<string>(type: "text", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditNoteDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PostingDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalCredit = table.Column<decimal>(type: "numeric", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerCreditNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerCreditNotes_CustomerInvoices_OriginalInvoiceId",
                        column: x => x.OriginalInvoiceId,
                        principalTable: "CustomerInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerCreditNotes_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerInvoiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    LineSubTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxCodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    TaxAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    RevenueAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CostCenterId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerInvoiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerInvoiceLines_Accounts_RevenueAccountId",
                        column: x => x.RevenueAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CustomerInvoiceLines_CustomerInvoices_CustomerInvoiceId",
                        column: x => x.CustomerInvoiceId,
                        principalTable: "CustomerInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerInvoiceLines_TaxCodes_TaxCodeId",
                        column: x => x.TaxCodeId,
                        principalTable: "TaxCodes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "text", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceiptDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PostingDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    WaybillNumber = table.Column<string>(type: "text", nullable: true),
                    TotalValue = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    OrderedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    InvoicedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    LineSubTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    CostCenterId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BankStatementLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankStatementId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: true),
                    CounterpartyName = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MatchedPaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankStatementLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankStatementLines_BankStatements_BankStatementId",
                        column: x => x.BankStatementId,
                        principalTable: "BankStatements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockReconciliationLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockReconciliationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemQty = table.Column<decimal>(type: "numeric", nullable: false),
                    PhysicalQty = table.Column<decimal>(type: "numeric", nullable: false),
                    DifferenceQty = table.Column<decimal>(type: "numeric", nullable: false),
                    ValuationRate = table.Column<decimal>(type: "numeric", nullable: false),
                    DifferenceValue = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReconciliationLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockReconciliationLines_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockReconciliationLines_StockReconciliations_StockReconcil~",
                        column: x => x.StockReconciliationId,
                        principalTable: "StockReconciliations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockTransactionLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    CostCenterId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransactionLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransactionLines_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockTransactionLines_StockTransactions_StockTransactionId",
                        column: x => x.StockTransactionId,
                        principalTable: "StockTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceiptLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceiptLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLines_GoodsReceipts_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalTable: "GoodsReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LandedCostVouchers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoucherNumber = table.Column<string>(type: "text", nullable: false),
                    PostingDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalFreightAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalCustomsAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalInsuranceAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalAdditionalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LandedCostVouchers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LandedCostVouchers_GoodsReceipts_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalTable: "GoodsReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "text", nullable: false),
                    SupplierInvoiceNumber = table.Column<string>(type: "text", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    GoodsReceiptId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PostingDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DocumentStatus = table.Column<int>(type: "integer", nullable: false),
                    SettlementStatus = table.Column<int>(type: "integer", nullable: false),
                    ThreeWayMatchStatus = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    OutstandingAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    IsGRNIBased = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierInvoices_GoodsReceipts_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalTable: "GoodsReceipts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierInvoices_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierInvoices_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LandedCostItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LandedCostVoucherId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    AllocatedAdditionalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    NewTotalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LandedCostItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LandedCostItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LandedCostItems_LandedCostVouchers_LandedCostVoucherId",
                        column: x => x.LandedCostVoucherId,
                        principalTable: "LandedCostVouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentRunItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceOutstandingAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ProposedPaymentAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    IsSelected = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRunItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRunItems_PaymentRuns_PaymentRunId",
                        column: x => x.PaymentRunId,
                        principalTable: "PaymentRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentRunItems_SupplierInvoices_SupplierInvoiceId",
                        column: x => x.SupplierInvoiceId,
                        principalTable: "SupplierInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentRunItems_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierDebitNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DebitNoteNumber = table.Column<string>(type: "text", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DebitNoteDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PostingDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalDebit = table.Column<decimal>(type: "numeric", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierDebitNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierDebitNotes_SupplierInvoices_OriginalInvoiceId",
                        column: x => x.OriginalInvoiceId,
                        principalTable: "SupplierInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierDebitNotes_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierInvoiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    LineSubTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxCodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    TaxAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    ExpenseOrAssetAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CostCenterId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierInvoiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierInvoiceLines_Accounts_ExpenseOrAssetAccountId",
                        column: x => x.ExpenseOrAssetAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierInvoiceLines_SupplierInvoices_SupplierInvoiceId",
                        column: x => x.SupplierInvoiceId,
                        principalTable: "SupplierInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierInvoiceLines_TaxCodes_TaxCodeId",
                        column: x => x.TaxCodeId,
                        principalTable: "TaxCodes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPeriods_FiscalYearId",
                table: "AccountingPeriods",
                column: "FiscalYearId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_ParentAccountId",
                table: "Accounts",
                column: "ParentAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_GLAccountId",
                table: "BankAccounts",
                column: "GLAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementLines_BankStatementId",
                table: "BankStatementLines",
                column: "BankStatementId");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatements_BankAccountId",
                table: "BankStatements",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CashDesks_GLAccountId",
                table: "CashDesks",
                column: "GLAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCreditNotes_CustomerId",
                table: "CustomerCreditNotes",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCreditNotes_OriginalInvoiceId",
                table: "CustomerCreditNotes",
                column: "OriginalInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerInvoiceLines_CustomerInvoiceId",
                table: "CustomerInvoiceLines",
                column: "CustomerInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerInvoiceLines_RevenueAccountId",
                table: "CustomerInvoiceLines",
                column: "RevenueAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerInvoiceLines_TaxCodeId",
                table: "CustomerInvoiceLines",
                column: "TaxCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerInvoices_CustomerId",
                table: "CustomerInvoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLines_GoodsReceiptId",
                table: "GoodsReceiptLines",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_PurchaseOrderId",
                table: "GoodsReceipts",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_SupplierId",
                table: "GoodsReceipts",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemUomConversions_ItemId",
                table: "ItemUomConversions",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LandedCostItems_ItemId",
                table: "LandedCostItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LandedCostItems_LandedCostVoucherId",
                table: "LandedCostItems",
                column: "LandedCostVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_LandedCostVouchers_GoodsReceiptId",
                table: "LandedCostVouchers",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_AccountId",
                table: "LedgerEntries",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_PostingBatchId",
                table: "LedgerEntries",
                column: "PostingBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_TenantId_PostingDate_AccountId",
                table: "LedgerEntries",
                columns: new[] { "TenantId", "PostingDate", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_ManualJournalLines_AccountId",
                table: "ManualJournalLines",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ManualJournalLines_ManualJournalId",
                table: "ManualJournalLines",
                column: "ManualJournalId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_PaymentId",
                table: "PaymentAllocations",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRunItems_PaymentRunId",
                table: "PaymentRunItems",
                column: "PaymentRunId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRunItems_SupplierId",
                table: "PaymentRunItems",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRunItems_SupplierInvoiceId",
                table: "PaymentRunItems",
                column: "SupplierInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRuns_BankAccountId",
                table: "PaymentRuns",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BankOrCashAccountId",
                table: "Payments",
                column: "BankOrCashAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_PurchaseOrderId",
                table: "PurchaseOrderLines",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitionLines_PurchaseRequisitionId",
                table: "PurchaseRequisitionLines",
                column: "PurchaseRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLedgerEntries_ItemId",
                table: "StockLedgerEntries",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLedgerEntries_TenantId_ItemId_WarehouseId_TransactionT~",
                table: "StockLedgerEntries",
                columns: new[] { "TenantId", "ItemId", "WarehouseId", "TransactionTime" });

            migrationBuilder.CreateIndex(
                name: "IX_StockLedgerEntries_WarehouseId",
                table: "StockLedgerEntries",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReconciliationLines_ItemId",
                table: "StockReconciliationLines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReconciliationLines_StockReconciliationId",
                table: "StockReconciliationLines",
                column: "StockReconciliationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReconciliations_WarehouseId",
                table: "StockReconciliations",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactionLines_ItemId",
                table: "StockTransactionLines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactionLines_StockTransactionId",
                table: "StockTransactionLines",
                column: "StockTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_SourceWarehouseId",
                table: "StockTransactions",
                column: "SourceWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_TargetWarehouseId",
                table: "StockTransactions",
                column: "TargetWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierDebitNotes_OriginalInvoiceId",
                table: "SupplierDebitNotes",
                column: "OriginalInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierDebitNotes_SupplierId",
                table: "SupplierDebitNotes",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoiceLines_ExpenseOrAssetAccountId",
                table: "SupplierInvoiceLines",
                column: "ExpenseOrAssetAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoiceLines_SupplierInvoiceId",
                table: "SupplierInvoiceLines",
                column: "SupplierInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoiceLines_TaxCodeId",
                table: "SupplierInvoiceLines",
                column: "TaxCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_GoodsReceiptId",
                table: "SupplierInvoices",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_PurchaseOrderId",
                table: "SupplierInvoices",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_SupplierId",
                table: "SupplierInvoices",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxCodes_TaxAccountId",
                table: "TaxCodes",
                column: "TaxAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_DefaultInventoryAccountId",
                table: "Warehouses",
                column: "DefaultInventoryAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountingDimensions");

            migrationBuilder.DropTable(
                name: "AccountingPeriods");

            migrationBuilder.DropTable(
                name: "BankStatementLines");

            migrationBuilder.DropTable(
                name: "CashDesks");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "CostLayers");

            migrationBuilder.DropTable(
                name: "CustomerCreditNotes");

            migrationBuilder.DropTable(
                name: "CustomerInvoiceLines");

            migrationBuilder.DropTable(
                name: "GoodsReceiptLines");

            migrationBuilder.DropTable(
                name: "ItemUomConversions");

            migrationBuilder.DropTable(
                name: "LandedCostItems");

            migrationBuilder.DropTable(
                name: "LedgerEntries");

            migrationBuilder.DropTable(
                name: "ManualJournalLines");

            migrationBuilder.DropTable(
                name: "PaymentAllocations");

            migrationBuilder.DropTable(
                name: "PaymentRunItems");

            migrationBuilder.DropTable(
                name: "PurchaseOrderLines");

            migrationBuilder.DropTable(
                name: "PurchaseRequisitionLines");

            migrationBuilder.DropTable(
                name: "StockLedgerEntries");

            migrationBuilder.DropTable(
                name: "StockReconciliationLines");

            migrationBuilder.DropTable(
                name: "StockTransactionLines");

            migrationBuilder.DropTable(
                name: "SupplierDebitNotes");

            migrationBuilder.DropTable(
                name: "SupplierInvoiceLines");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "FiscalYears");

            migrationBuilder.DropTable(
                name: "BankStatements");

            migrationBuilder.DropTable(
                name: "CustomerInvoices");

            migrationBuilder.DropTable(
                name: "LandedCostVouchers");

            migrationBuilder.DropTable(
                name: "PostingBatches");

            migrationBuilder.DropTable(
                name: "ManualJournals");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "PaymentRuns");

            migrationBuilder.DropTable(
                name: "PurchaseRequisitions");

            migrationBuilder.DropTable(
                name: "StockReconciliations");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "StockTransactions");

            migrationBuilder.DropTable(
                name: "SupplierInvoices");

            migrationBuilder.DropTable(
                name: "TaxCodes");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "BankAccounts");

            migrationBuilder.DropTable(
                name: "Warehouses");

            migrationBuilder.DropTable(
                name: "GoodsReceipts");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "Suppliers");
        }
    }
}
