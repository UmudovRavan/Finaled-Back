using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AltensorAccounting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAccountingFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DeliveryNoteId",
                table: "CustomerInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SalesOrderId",
                table: "CustomerInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AsanLoginId",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankCode",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChiefAccountantName",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrespondentAccount",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DirectorName",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FiscalYearEndMonth",
                table: "Companies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FiscalYearStartMonth",
                table: "Companies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Iban",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVatPayer",
                table: "Companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LegalForm",
                table: "Companies",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatisticalCode",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SwiftBic",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxRegime",
                table: "Companies",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "Companies",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Subcategory",
                table: "Accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InitialBalanceAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActNumber = table.Column<string>(type: "text", nullable: false),
                    ActDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AttachmentUrl = table.Column<string>(type: "text", nullable: true),
                    AuthorizedBy = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    TotalDebit = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalCredit = table.Column<decimal>(type: "numeric", nullable: false),
                    JournalBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    AccountCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InitialBalanceAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "text", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CustomerReference = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryNumber = table.Column<string>(type: "text", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PostingDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DriverName = table.Column<string>(type: "text", nullable: true),
                    VehicleNumber = table.Column<string>(type: "text", nullable: true),
                    TrackingNumber = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    TotalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    StockTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryNotes_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeliveryNotes_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeliveryNotes_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    DeliveredQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    InvoicedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxCodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    TaxRate = table.Column<decimal>(type: "numeric", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrderLines_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryNoteLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryNoteLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryNoteLines_DeliveryNotes_DeliveryNoteId",
                        column: x => x.DeliveryNoteId,
                        principalTable: "DeliveryNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeliveryNoteLines_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNoteLines_DeliveryNoteId",
                table: "DeliveryNoteLines",
                column: "DeliveryNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNoteLines_ItemId",
                table: "DeliveryNoteLines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNotes_CustomerId",
                table: "DeliveryNotes",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNotes_SalesOrderId",
                table: "DeliveryNotes",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNotes_WarehouseId",
                table: "DeliveryNotes",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderLines_SalesOrderId",
                table: "SalesOrderLines",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CustomerId",
                table: "SalesOrders",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryNoteLines");

            migrationBuilder.DropTable(
                name: "InitialBalanceAuditLogs");

            migrationBuilder.DropTable(
                name: "SalesOrderLines");

            migrationBuilder.DropTable(
                name: "DeliveryNotes");

            migrationBuilder.DropTable(
                name: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "DeliveryNoteId",
                table: "CustomerInvoices");

            migrationBuilder.DropColumn(
                name: "SalesOrderId",
                table: "CustomerInvoices");

            migrationBuilder.DropColumn(
                name: "AsanLoginId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "BankCode",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ChiefAccountantName",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CorrespondentAccount",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "DirectorName",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "FiscalYearEndMonth",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "FiscalYearStartMonth",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Iban",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "IsVatPayer",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "LegalForm",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "StatisticalCode",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "SwiftBic",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "TaxRegime",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Subcategory",
                table: "Accounts");
        }
    }
}
