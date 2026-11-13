using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using ClinicManagementSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ClinicDbContext _context;

    public UserRepository(ClinicDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            .ConfigureAwait(false);

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Username == username, ct)
            .ConfigureAwait(false);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Email == email, ct)
            .ConfigureAwait(false);

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken ct = default)
        => await _context.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsSplitQuery()
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task AddAsync(User user, CancellationToken ct = default)
        => await _context.Users.AddAsync(user, ct).ConfigureAwait(false);

    public void Update(User user)
        => _context.Users.Update(user);

    public void Delete(User user)
        => _context.Users.Remove(user);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct).ConfigureAwait(false);
}
