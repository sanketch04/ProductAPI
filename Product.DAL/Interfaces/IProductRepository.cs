
using Product.DAL.Entities;

namespace Product.DAL.Interfaces
{
    public interface IProductRepository
    {
        Task<List<ProductEntity>> GetAllAsync();

        Task<ProductEntity?> GetByIdAsync(int id);

        Task<ProductEntity> AddAsync(ProductEntity product);

        Task<bool> UpdateAsync(ProductEntity product);

        Task<bool> DeleteAsync(int id);
    }
}
