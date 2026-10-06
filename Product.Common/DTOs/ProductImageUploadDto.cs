
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Product.Common.DTOs
{
    public class ProductImageUploadDto
    {
        [Required]
        public IFormFile File { get; set; } = null!;
    }
}
