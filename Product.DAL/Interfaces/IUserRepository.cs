
using Product.DAL.Entities;

namespace Product.DAL.Interfaces;

public interface IUserRepository
{
    Task<ApplicationUserEntity?> GetByEmailAsync(string normalizedEmail);

    Task<bool> EmailExistsAsync(string normalizedEmail);
    Task<ApplicationUserEntity?> GetByIdAsync(int id);


    Task<ApplicationUserEntity> CreateAsync(
        ApplicationUserEntity user);

    Task UpdateAsync(ApplicationUserEntity user);
}
