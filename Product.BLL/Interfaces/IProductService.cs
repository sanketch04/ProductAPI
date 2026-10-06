
using Product.Common.DTOs;

namespace Product.BLL.Interfaces
{
    public interface IProductService
    {
        Task<List<ProductResponseDto>> GetAllAsync();

        Task<ProductResponseDto?> GetByIdAsync(int id);

        Task<ProductResponseDto> CreateAsync(ProductCreateDto dto);

        Task<bool> UpdateAsync(int id, ProductUpdateDto dto);

        Task<bool> DeleteAsync(int id);
    }
}
