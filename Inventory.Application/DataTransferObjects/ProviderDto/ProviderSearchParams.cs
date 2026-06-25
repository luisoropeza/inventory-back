namespace Inventory.Application.DataTransferObjects.ProviderDto
{
    public class ProviderSearchParams
    {
        public string? Name { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}