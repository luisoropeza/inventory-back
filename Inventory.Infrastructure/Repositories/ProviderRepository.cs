using Inventory.Application.Common.Abstracts;
using Inventory.Application.Common.Pagination;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Context;
using Inventory.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories
{
    public class ProviderRepository(InventoryDbContext context, IDateTimeProvider dateTimeProvider) : IProviderRepository
    {
        public async Task<PaginatedList<Provider>> GetProvidersAsync(Guid businessId, string? name, int page, int pageSize) =>
            await context.Providers
                .AsNoTracking()
                .Where(p => p.BusinessId == businessId)
                .OrderByDescending(b => b.CreatedAt)
                .FiltersProvider(name)
                .ToPaginatedListAsync(page, pageSize);

        public async Task<Provider?> GetProviderByIdAsync(Guid id, Guid businessId) =>
            await context.Providers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId);

        public async Task<Provider> CreateProviderAsync(Provider provider)
        {
            context.Providers.Add(provider);
            await context.SaveChangesAsync();
            return await context.Providers
                .AsNoTracking()
                .FirstAsync(c => c.Id == provider.Id);
        }

        public async Task UpdateProviderAsync(Provider provider)
        {
            provider.UpdatedAt = dateTimeProvider.UtcNow;
            context.Providers.Update(provider);
            await context.SaveChangesAsync();
        }

        public async Task DeleteProviderAsync(Provider provider)
        {
            provider.IsDeleted = true;
            context.Providers.Update(provider);
            await context.SaveChangesAsync();
        }
    }
}
