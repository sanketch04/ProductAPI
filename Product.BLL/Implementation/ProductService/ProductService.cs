using Microsoft.Extensions.Logging;
using Product.BLL.Interfaces.ProductServices;
using Product.Common.DTOs;
using Product.DAL.Entities;
using Product.DAL.Interfaces.ProductInterfaces;

namespace Product.BLL.Implementation.ProductService
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<ProductService> _logger;

        public ProductService(
            IProductRepository productRepository,
            ILogger<ProductService> logger)
        {
            _productRepository = productRepository;
            _logger = logger;
        }

        public async Task<List<ProductResponseDto>> GetAllAsync(int pagenumber,int pageSize)
        {
            _logger.LogInformation("Fetching all products");

            try
            {
                var products = await _productRepository.GetAllAsync(pagenumber,pageSize);
                var result = products.Select(MapToResponse).ToList();

                _logger.LogInformation(
                    "Fetched {ProductCount} products", result.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch all products");
                throw;
            }
        }

        public async Task<ProductResponseDto?> GetByIdAsync(int id)
        {
            _logger.LogInformation("Fetching product {ProductId}", id);

            try
            {
                var product = await _productRepository.GetByIdAsync(id);

                if (product == null)
                {
                    _logger.LogWarning("Product {ProductId} not found", id);
                    return null;
                }

                return MapToResponse(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch product {ProductId}", id);
                throw;
            }
        }

        public async Task<ProductResponseDto> CreateAsync(ProductCreateDto dto)
        {
            _logger.LogInformation(
                "Creating product {ProductName} with price {Price} and stock {Stock}",
                dto.Name, dto.Price, dto.Stock);

            try
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

                _logger.LogInformation(
                    "Product created with {ProductId} and name {ProductName}",
                    createdProduct.Id, createdProduct.Name);

                return MapToResponse(createdProduct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to create product {ProductName}", dto.Name);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(int id, ProductUpdateDto dto)
        {
            _logger.LogInformation("Updating product {ProductId}", id);

            try
            {
                var product = await _productRepository.GetByIdAsync(id);

                if (product == null)
                {
                    _logger.LogWarning(
                        "Update failed: product {ProductId} not found", id);
                    return false;
                }

                product.Name = dto.Name.Trim();
                product.Description = dto.Description;
                product.Price = dto.Price;
                product.Stock = dto.Stock;

                var updated = await _productRepository.UpdateAsync(product);

                if (updated)
                {
                    _logger.LogInformation(
                        "Product {ProductId} updated", id);
                }
                else
                {
                    _logger.LogWarning(
                        "Repository reported no update for product {ProductId}", id);
                }

                return updated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update product {ProductId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogInformation("Deleting product {ProductId}", id);

            try
            {
                var deleted = await _productRepository.DeleteAsync(id);

                if (deleted)
                {
                    _logger.LogInformation("Product {ProductId} deleted", id);
                }
                else
                {
                    _logger.LogWarning(
                        "Delete failed: product {ProductId} not found", id);
                }

                return deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete product {ProductId}", id);
                throw;
            }
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