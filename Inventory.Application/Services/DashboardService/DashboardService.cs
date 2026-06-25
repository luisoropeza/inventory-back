using Inventory.Application.Common.Abstracts;
using Inventory.Application.DataTransferObjects.DashboardDto;

namespace Inventory.Application.Services.DashboardService
{
    public class DashboardService(IDashboardRepository repository, IDateTimeProvider dateTimeProvider) : IDashboardService
    {
        public async Task<DashboardResponse> GetTodayStatsAsync(Guid businessId, Guid? branchId)
        {
            var start = dateTimeProvider.UtcNow.Date;
            var end = start.AddDays(1);

            var salesCountTask = repository.GetTodaySalesCountAsync(businessId, branchId, start, end);
            var salesTotalTask = repository.GetTodaySalesTotalAsync(businessId, branchId, start, end);
            var movementsCountTask = repository.GetTodayMovementsCountAsync(businessId, branchId, start, end);
            var lowStockCountTask = repository.GetLowStockProductsCountAsync(businessId, branchId);

            await Task.WhenAll(salesCountTask, salesTotalTask, movementsCountTask, lowStockCountTask);

            return new DashboardResponse
            {
                TodaySalesCount = salesCountTask.Result,
                TodaySalesTotal = salesTotalTask.Result,
                TodayMovementsCount = movementsCountTask.Result,
                LowStockProductsCount = lowStockCountTask.Result
            };
        }
    }
}
