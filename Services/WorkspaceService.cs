using Microsoft.EntityFrameworkCore;
using TrelloCloneAPI.Data;
using TrelloCloneAPI.DTOs;
using TrelloCloneAPI.Models;

namespace TrelloCloneAPI.Services;

public interface IWorkspaceService
{
    Task<IEnumerable<WorkspaceDto>> GetAllAsync(Guid? userId = null);
    Task<WorkspaceDto?> GetByIdAsync(Guid id);
    Task<WorkspaceDto> CreateAsync(CreateWorkspaceDto dto);
    Task<WorkspaceDto?> UpdateAsync(Guid id, UpdateWorkspaceDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<WorkspaceMemberDto> AddMemberAsync(CreateWorkspaceMemberDto dto);
}

public class WorkspaceService : IWorkspaceService
{
    private readonly TrelloDbContext _context;

    public WorkspaceService(TrelloDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<WorkspaceDto>> GetAllAsync(Guid? userId = null)
    {
        IQueryable<Workspace> query = _context.Workspaces;
        if (userId.HasValue)
        {
            query = query.Where(w => w.OwnerId == userId.Value || w.Members.Any(m => m.UserId == userId.Value));
        }
        return await query
            .Select(w => new WorkspaceDto(w.Id, w.Name, w.Description, w.OwnerId, w.CreatedAt))
            .ToListAsync();
    }

    public async Task<WorkspaceDto?> GetByIdAsync(Guid id)
    {
        var workspace = await _context.Workspaces.FindAsync(id);
        return workspace is null ? null : new WorkspaceDto(workspace.Id, workspace.Name, workspace.Description, workspace.OwnerId, workspace.CreatedAt);
    }

    public async Task<WorkspaceDto> CreateAsync(CreateWorkspaceDto dto)
    {
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Description = dto.Description,
            OwnerId = dto.OwnerId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();
        return new WorkspaceDto(workspace.Id, workspace.Name, workspace.Description, workspace.OwnerId, workspace.CreatedAt);
    }

    public async Task<WorkspaceDto?> UpdateAsync(Guid id, UpdateWorkspaceDto dto)
    {
        var workspace = await _context.Workspaces.FindAsync(id);
        if (workspace is null) return null;

        if (!string.IsNullOrWhiteSpace(dto.Name)) workspace.Name = dto.Name.Trim();
        if (dto.Description is not null) workspace.Description = dto.Description;

        await _context.SaveChangesAsync();
        return new WorkspaceDto(workspace.Id, workspace.Name, workspace.Description, workspace.OwnerId, workspace.CreatedAt);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var workspace = await _context.Workspaces.FindAsync(id);
        if (workspace is null) return false;

        _context.Workspaces.Remove(workspace);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<WorkspaceMemberDto> AddMemberAsync(CreateWorkspaceMemberDto dto)
    {
        var member = new WorkspaceMember
        {
            Id = Guid.NewGuid(),
            WorkspaceId = dto.WorkspaceId,
            UserId = dto.UserId,
            Role = dto.Role
        };

        _context.WorkspaceMembers.Add(member);
        await _context.SaveChangesAsync();
        return new WorkspaceMemberDto(member.Id, member.WorkspaceId, member.UserId, member.Role);
    }
}
