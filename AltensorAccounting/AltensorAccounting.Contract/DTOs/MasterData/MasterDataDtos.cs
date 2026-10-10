using System;
using System.Collections.Generic;

namespace AltensorAccounting.Contract.DTOs.MasterData;

public class MasterDataSummaryDto
{
    public int CustomersCount { get; set; }
    public int SuppliersCount { get; set; }
    public int WarehousesCount { get; set; }
    public int ItemsCount { get; set; }
    public int FixedAssetsCount { get; set; }
    public int BankAccountsCount { get; set; }
    public int TaxCodesCount { get; set; }
    public int AccountsCount { get; set; }
}

public class MasterDataTabItemDto
{
    public string Key { get; set; } = default!;
    public string TitleAz { get; set; } = default!;
    public string TitleEn { get; set; } = default!;
    public int Count { get; set; }
    public string ApiEndpoint { get; set; } = default!;
}
