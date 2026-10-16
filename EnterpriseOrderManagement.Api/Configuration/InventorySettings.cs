namespace EnterpriseOrderManagement.Api.Configuration;

public sealed class InventorySettings
{
    public const string SectionName = "InventorySettings";

    public int LowStockThreshold { get; set; } = 5;
}