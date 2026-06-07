using Microsoft.EntityFrameworkCore;
using TrelloCloneAPI.Data;
using TrelloCloneAPI.DTOs;
using TrelloCloneAPI.Models;

namespace TrelloCloneAPI.Services;

public interface IActivityLogService
{
    Task<IEnumerable<ActivityLogDto>> GetAllAsync();
    Task<IEnumerable<ActivityLogDto>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<ActivityLogDto>> GetByEntityAsync(string entityType, string entityId);
    Task<ActivityLogDto?> GetByIdAsync(Guid id);
    Task<ActivityLogDto> CreateAsync(CreateActivityLogDto dto);
}

public class ActivityLogService : IActivityLogService
{
    private readonly TrelloDbContext _context;

    public ActivityLogService(TrelloDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ActivityLogDto>> GetAllAsync()
    {
        return await _context.ActivityLogs
            .Select(a => new ActivityLogDto(a.Id, a.UserId, a.Action, a.EntityType, a.EntityId, a.CreatedAt))
            .ToListAsync();
    }

    public async Task<IEnumerable<ActivityLogDto>> GetByUserIdAsync(Guid userId)
    {
        return await _context.ActivityLogs
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new ActivityLogDto(a.Id, a.UserId, a.Action, a.EntityType, a.EntityId, a.CreatedAt))
            .ToListAsync();
    }

    public async Task<IEnumerable<ActivityLogDto>> GetByEntityAsync(string entityType, string entityId)
    {
        return await _context.ActivityLogs
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new ActivityLogDto(a.Id, a.UserId, a.Action, a.EntityType, a.EntityId, a.CreatedAt))
            .ToListAsync();
    }

    public async Task<ActivityLogDto?> GetByIdAsync(Guid id)
    {
        var activity = await _context.ActivityLogs.FindAsync(id);
        return activity is null ? null : new ActivityLogDto(activity.Id, activity.UserId, activity.Action, activity.EntityType, activity.EntityId, activity.CreatedAt);
    }

    public async Task<ActivityLogDto> CreateAsync(CreateActivityLogDto dto)
    {
        var activity = new ActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            Action = dto.Action,
            EntityType = dto.EntityType,
            EntityId = dto.EntityId,
            CreatedAt = DateTime.UtcNow
        };

        _context.ActivityLogs.Add(activity);
        await _context.SaveChangesAsync();
        return new ActivityLogDto(activity.Id, activity.UserId, activity.Action, activity.EntityType, activity.EntityId, activity.CreatedAt);
    }
}
