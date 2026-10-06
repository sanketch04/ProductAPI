
using Microsoft.AspNetCore.Mvc;
using Product.BLL.Interfaces;
using Product.Common.DTOs;

namespace ProductAPI.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IProductImageService _imageService;
        private readonly IQrService _qrService;

        public ProductsController(
            IProductService productService,
            IProductImageService imageService,
            IQrService qrService)
        {
            _productService = productService;
            _imageService = imageService;
            _qrService = qrService;
        }

        // GET: api/products
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            Console.WriteLine("1. GetAll API started");

            var products = await _productService.GetAllAsync();

            Console.WriteLine("2. Product service completed");
            Console.WriteLine($"3. Products found: {products.Count}");

            return Ok(products);
        }

        // GET: api/products/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest("Product ID must be greater than zero.");

            var product = await _productService.GetByIdAsync(id);

            if (product == null)
                return NotFound("Product not found.");

            return Ok(product);
        }

        // POST: api/products
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] ProductCreateDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var product = await _productService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = product.Id },
                product);
        }

        // PUT: api/products/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] ProductUpdateDto dto)
        {
            if (id <= 0)
                return BadRequest("Product ID must be greater than zero.");

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var updated = await _productService.UpdateAsync(id, dto);

            if (!updated)
                return NotFound("Product not found.");

            return NoContent();
        }

        // DELETE: api/products/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest("Product ID must be greater than zero.");

            var deleted = await _productService.DeleteAsync(id);

            if (!deleted)
                return NotFound("Product not found.");

            return NoContent();
        }

        // POST: api/products/1/image
        [HttpPost("{id:int}/image")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadImage(
            int id,
            [FromForm] ProductImageUploadDto request)
        {
            if (id <= 0)
                return BadRequest("Product ID must be greater than zero.");

            if (request?.File == null || request.File.Length == 0)
                return BadRequest("Please select an image.");

            try
            {
                var uploaded = await _imageService.UploadAsync(
                    id, request.File);

                if (!uploaded)
                    return NotFound("Product not found.");

                var product = await _productService.GetByIdAsync(id);

                return Ok(new
                {
                    message = "Image uploaded successfully.",
                    imagePath = product?.ImagePath
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/products/1/image
        [HttpGet("{id:int}/image")]
        public async Task<IActionResult> DownloadImage(int id)
        {
            if (id <= 0)
                return BadRequest("Product ID must be greater than zero.");

            var image = await _imageService.DownloadAsync(id);

            if (image == null)
                return NotFound("Product or image not found.");

            return File(
                image.Value.Content,
                image.Value.ContentType,
                image.Value.FileName);
        }

        // GET: api/products/1/qr
        [HttpGet("{id:int}/qr")]
        public async Task<IActionResult> GenerateQr(int id)
        {
            if (id <= 0)
                return BadRequest("Product ID must be greater than zero.");

            var qrBytes = await _qrService.GenerateProductQrAsync(id);

            if (qrBytes == null)
                return NotFound("Product not found.");

            return File(
                qrBytes,
                "image/png",
                $"product-{id}-qr.png");
        }
    }
}
