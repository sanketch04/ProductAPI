
using Product.DAL.Entities;

namespace Product.BLL.Models;

public class RefreshTokenRotationResult
{
    public string Token { get; set; } = string.Empty;

    public ApplicationUserEntity User { get; set; } = null!;
}
