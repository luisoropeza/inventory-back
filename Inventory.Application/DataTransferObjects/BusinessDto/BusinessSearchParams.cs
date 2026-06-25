namespace Inventory.Application.DataTransferObjects.BusinessDto
{
    public class BusinessSearchParams
    {
        public string? Name { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
