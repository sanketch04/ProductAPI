using Product.Common.DTOs;

namespace Product.BLL.Interfaces.ProductServices
{
    public interface IProductService
    {
        Task<List<ProductResponseDto>> GetAllAsync(int pageNumber,int pageSize);

        Task<ProductResponseDto?> GetByIdAsync(int id);

        Task<ProductResponseDto> CreateAsync(ProductCreateDto dto);

        Task<bool> UpdateAsync(int id, ProductUpdateDto dto);

        Task<bool> DeleteAsync(int id);
    }
}
