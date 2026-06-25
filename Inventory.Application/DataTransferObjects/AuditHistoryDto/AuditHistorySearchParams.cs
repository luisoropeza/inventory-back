using Inventory.Domain.Enum;

namespace Inventory.Application.DataTransferObjects.AuditHistoryDto
{
    public class AuditHistorySearchParams
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}