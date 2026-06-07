using Microsoft.EntityFrameworkCore;
using TrelloCloneAPI.Data;
using TrelloCloneAPI.DTOs;
using TrelloCloneAPI.Models;

namespace TrelloCloneAPI.Services;

public interface IRoleService
{
    Task<IEnumerable<RoleDto>> GetAllAsync();
    Task<RoleDto?> GetByIdAsync(Guid id);
    Task<RoleDto> CreateAsync(CreateRoleDto dto);
}

public class RoleService : IRoleService
{
    private readonly TrelloDbContext _context;

    public RoleService(TrelloDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RoleDto>> GetAllAsync()
    {
        return await _context.Roles.Select(r => new RoleDto(r.Id, r.RoleName)).ToListAsync();
    }

    public async Task<RoleDto?> GetByIdAsync(Guid id)
    {
        var role = await _context.Roles.FindAsync(id);
        return role is null ? null : new RoleDto(role.Id, role.RoleName);
    }

    public async Task<RoleDto> CreateAsync(CreateRoleDto dto)
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            RoleName = dto.RoleName.Trim()
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();
        return new RoleDto(role.Id, role.RoleName);
    }
}
