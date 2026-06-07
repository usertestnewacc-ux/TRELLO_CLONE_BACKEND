using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using TrelloCloneAPI.Data;
using TrelloCloneAPI.DTOs;
using TrelloCloneAPI.Models;

namespace TrelloCloneAPI.Services;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllAsync();
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<UserDto> CreateAsync(CreateUserDto dto);
    Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> VerifyPasswordAsync(string email, string password);
    Task<User?> FindByEmailAsync(string email);
}

public class UserService : IUserService
{
    private readonly TrelloDbContext _context;

    public UserService(TrelloDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        return await _context.Users
            .Select(u => ToDto(u))
            .ToListAsync();
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        return user is null ? null : ToDto(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto)
    {
        var existing = await _context.Users.AnyAsync(u => u.Email == dto.Email.Trim().ToLowerInvariant());
        if (existing)
        {
            throw new InvalidOperationException("A user with that email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            RoleId = dto.RoleId,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            PasswordHash = CreatePasswordHash(dto.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return ToDto(user);
    }

    public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(dto.FirstName))
        {
            user.FirstName = dto.FirstName;
        }

        if (!string.IsNullOrWhiteSpace(dto.LastName))
        {
            user.LastName = dto.LastName;
        }

        if (dto.RoleId.HasValue)
        {
            user.RoleId = dto.RoleId;
        }

        if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            user.Status = dto.Status;
        }

        await _context.SaveChangesAsync();
        return ToDto(user);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> VerifyPasswordAsync(string email, string password)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email.Trim().ToLowerInvariant());
        return user is not null && VerifyPasswordHash(password, user.PasswordHash);
    }

    public async Task<User?> FindByEmailAsync(string email)
    {
        return await _context.Users.SingleOrDefaultAsync(u => u.Email == email.Trim().ToLowerInvariant());
    }

    private static UserDto ToDto(User user)
        => new(user.Id, user.Email, user.FirstName, user.LastName, user.RoleId, user.Status, user.CreatedAt);

    private static string CreatePasswordHash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        using var deriveBytes = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var hash = deriveBytes.GetBytes(32);

        return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
    }

    private static bool VerifyPasswordHash(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var expectedHash = Convert.FromBase64String(parts[1]);
        using var deriveBytes = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var actualHash = deriveBytes.GetBytes(32);

        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }
}
