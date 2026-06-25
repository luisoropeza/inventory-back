namespace Inventory.Application.DataTransferObjects.WarehouseDto
{
    public class WarehouseSearchParams
    {
        public string? Name { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
