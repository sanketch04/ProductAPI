
using Microsoft.EntityFrameworkCore;
using Product.DAL.Context;
using Product.DAL.Entities;
using Product.DAL.Interfaces;

namespace Product.DAL.Repository;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    #region Ctor

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    #endregion

    #region Get User

    public async Task<ApplicationUserEntity?> GetByEmailAsync(
     string normalizedEmail)
    {
        return await _context.users
            .FirstOrDefaultAsync(
                u => u.NormalizedEmail == normalizedEmail);
    }


    public async Task<ApplicationUserEntity?> GetByIdAsync(int id)
    {
        return await _context.users
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    #endregion

    #region Email Validation


    public async Task<bool> EmailExistsAsync(string normalizedEmail)
    {
        return await _context.users
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail);
    }



    #endregion

    #region Create User

    public async Task<ApplicationUserEntity> CreateAsync(
        ApplicationUserEntity user)
    {
        await _context.users.AddAsync(user);

        await _context.SaveChangesAsync();

        return user;
    }

    #endregion


    #region Update User

    public async Task UpdateAsync(ApplicationUserEntity user)
    {
        _context.users.Update(user);

        await _context.SaveChangesAsync();
    }

    #endregion
}
