namespace Inventory.Application.DataTransferObjects.CategoryDto
{
    public class CategorySearchParams
    {
        public string? Name { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
