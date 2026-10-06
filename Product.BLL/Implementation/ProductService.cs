
using Product.BLL.Interfaces;
using Product.Common.DTOs;
using Product.DAL.Entities;
using Product.DAL.Interfaces;

namespace Product.BLL.Implementation
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<List<ProductResponseDto>> GetAllAsync()
        {
            var products = await _productRepository.GetAllAsync();

            return products.Select(MapToResponse).ToList();
        }

        public async Task<ProductResponseDto?> GetByIdAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);

            return product == null ? null : MapToResponse(product);
        }

        public async Task<ProductResponseDto> CreateAsync(ProductCreateDto dto)
        {
            var product = new ProductEntity
            {
                Name = dto.Name.Trim(),
                Description = dto.Description,
                Price = dto.Price,
                Stock = dto.Stock
            };

            var createdProduct =
                await _productRepository.AddAsync(product);

            return MapToResponse(createdProduct);
        }

        public async Task<bool> UpdateAsync(int id, ProductUpdateDto dto)
        {
            var product = await _productRepository.GetByIdAsync(id);

            if (product == null)
            {
                return false;
            }

            product.Name = dto.Name.Trim();
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.Stock = dto.Stock;

            return await _productRepository.UpdateAsync(product);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _productRepository.DeleteAsync(id);
        }

        private static ProductResponseDto MapToResponse(
            ProductEntity product)
        {
            return new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                ImagePath = product.ImagePath,
                CreatedAt = product.CreatedAt
            };
        }
    }
}
