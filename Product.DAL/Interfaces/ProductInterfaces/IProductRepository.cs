using Product.DAL.Entities;

namespace Product.DAL.Interfaces.ProductInterfaces
{
    public interface IProductRepository
    {
        Task<List<ProductEntity>> GetAllAsync(int pageNumber = 1, int pageSize = 10);

        Task<ProductEntity?> GetByIdAsync(int id);

        Task<ProductEntity> AddAsync(ProductEntity product);

        Task<bool> UpdateAsync(ProductEntity product);

        Task<bool> DeleteAsync(int id);
    }
}
