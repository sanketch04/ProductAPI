
using Product.DAL.Entities;

namespace Product.BLL.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(ApplicationUserEntity user);
}
