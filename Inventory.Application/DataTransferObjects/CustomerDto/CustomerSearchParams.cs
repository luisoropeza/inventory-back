namespace Inventory.Application.DataTransferObjects.CustomerDto
{
    public class CustomerSearchParams
    {
        public string? Name { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}