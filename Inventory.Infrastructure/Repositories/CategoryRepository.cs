using Inventory.Application.Common.Abstracts;
using Inventory.Application.Common.Pagination;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Context;
using Inventory.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories
{
    public class CategoryRepository(InventoryDbContext context, IDateTimeProvider dateTimeProvider) : ICategoryRepository
    {
        public async Task<PaginatedList<Category>> GetCategoriesAsync(Guid businessId, string? name, int page, int pageSize) =>
            await context.Categories
                .AsNoTracking()
                .Where(c => c.BusinessId == businessId)
                .OrderByDescending(b => b.CreatedAt)
                .FiltersCategory(name)
                .ToPaginatedListAsync(page, pageSize);

        public async Task<Category?> GetCategoryByIdAsync(int id, Guid businessId) =>
            await context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId);

        public async Task<Category> CreateCategoryAsync(Category category)
        {
            context.Categories.Add(category);
            await context.SaveChangesAsync();
            return await context.Categories
                .AsNoTracking()
                .FirstAsync(c => c.Id == category.Id);
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            category.UpdatedAt = dateTimeProvider.UtcNow;
            context.Categories.Update(category);
            await context.SaveChangesAsync();
        }

        public async Task DeleteCategoryAsync(Category category)
        {
            category.IsDeleted = true;
            context.Categories.Update(category);
            await context.SaveChangesAsync();
        }
    }
}
