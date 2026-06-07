using Microsoft.EntityFrameworkCore;
using TrelloCloneAPI.Data;
using TrelloCloneAPI.DTOs;

namespace TrelloCloneAPI.Services;

public interface IReportService
{
    Task<DashboardReportDto> GetDashboardReportAsync();
}

public class ReportService : IReportService
{
    private readonly TrelloDbContext _context;

    public ReportService(TrelloDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardReportDto> GetDashboardReportAsync()
    {
        var totalWorkspaces = await _context.Workspaces.CountAsync();
        var totalBoards     = await _context.Boards.CountAsync();
        var totalLists      = await _context.Lists.CountAsync();
        var totalCards      = await _context.Cards.CountAsync();

        // Use simple == so EF Core / MySQL can translate without OrdinalIgnoreCase
        var toDoTasks       = await _context.Cards.CountAsync(c => c.Status == "ToDo" || c.Status == null);
        var inProgressTasks = await _context.Cards.CountAsync(c => c.Status == "InProgress");
        var completedTasks  = await _context.Cards.CountAsync(c => c.Status == "Done");
        var overdueTasks    = await _context.Cards.CountAsync(c =>
            c.DueDate != null && c.DueDate < DateTime.UtcNow && c.Status != "Done");

        var highPriorityTasks   = await _context.Cards.CountAsync(c => c.Priority == "High");
        var mediumPriorityTasks = await _context.Cards.CountAsync(c => c.Priority == "Medium");
        var lowPriorityTasks    = await _context.Cards.CountAsync(c => c.Priority == "Low");

        var statusBreakdown = await _context.Cards
            .GroupBy(c => c.Status == null ? "ToDo" : c.Status)
            .Select(g => new ChartSegmentDto(g.Key, g.Count()))
            .ToListAsync();

        var priorityBreakdown = await _context.Cards
            .GroupBy(c => c.Priority == null ? "Unspecified" : c.Priority)
            .Select(g => new ChartSegmentDto(g.Key, g.Count()))
            .ToListAsync();

        var assigneeBreakdown = await _context.Cards
            .Where(c => c.AssignedUserId != null)
            .Join(_context.Users,
                card => card.AssignedUserId,
                user => user.Id,
                (card, user) => new { user.Email })
            .GroupBy(x => x.Email == null ? "Unknown" : x.Email)
            .Select(g => new ChartSegmentDto(g.Key, g.Count()))
            .ToListAsync();

        var averageCardsPerList = totalLists > 0
            ? Math.Round(totalCards / (double)totalLists, 2)
            : 0;

        return new DashboardReportDto(
            totalWorkspaces,
            totalBoards,
            totalLists,
            totalCards,
            averageCardsPerList,
            completedTasks,
            inProgressTasks,
            toDoTasks,
            overdueTasks,
            highPriorityTasks,
            mediumPriorityTasks,
            lowPriorityTasks,
            statusBreakdown,
            priorityBreakdown,
            assigneeBreakdown);
    }
}
