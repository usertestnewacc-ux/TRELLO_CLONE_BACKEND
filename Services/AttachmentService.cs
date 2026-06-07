using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TrelloCloneAPI.Data;
using TrelloCloneAPI.DTOs;
using TrelloCloneAPI.Models;

namespace TrelloCloneAPI.Services;

public interface IAttachmentService
{
    Task<IEnumerable<AttachmentDto>> GetAllAsync();
    Task<IEnumerable<AttachmentDto>> GetByCardIdAsync(Guid cardId);
    Task<AttachmentDto?> GetByIdAsync(Guid id);
    Task<AttachmentDto> CreateAsync(CreateAttachmentDto dto);
    Task<AttachmentDto> UploadAsync(Guid cardId, IFormFile file);
    Task<bool> DeleteAsync(Guid id);
}

public class AttachmentService : IAttachmentService
{
    private readonly TrelloDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public AttachmentService(TrelloDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public async Task<IEnumerable<AttachmentDto>> GetAllAsync()
    {
        return await _context.Attachments
            .Select(a => new AttachmentDto(a.Id, a.CardId, a.FileName, a.FilePath, a.UploadedAt))
            .ToListAsync();
    }

    public async Task<IEnumerable<AttachmentDto>> GetByCardIdAsync(Guid cardId)
    {
        return await _context.Attachments
            .Where(a => a.CardId == cardId)
            .OrderBy(a => a.UploadedAt)
            .Select(a => new AttachmentDto(a.Id, a.CardId, a.FileName, a.FilePath, a.UploadedAt))
            .ToListAsync();
    }

    public async Task<AttachmentDto?> GetByIdAsync(Guid id)
    {
        var attachment = await _context.Attachments.FindAsync(id);
        return attachment is null ? null : new AttachmentDto(attachment.Id, attachment.CardId, attachment.FileName, attachment.FilePath, attachment.UploadedAt);
    }

    public async Task<AttachmentDto> CreateAsync(CreateAttachmentDto dto)
    {
        var attachment = new Attachment
        {
            Id = Guid.NewGuid(),
            CardId = dto.CardId,
            FileName = dto.FileName,
            FilePath = dto.FilePath,
            UploadedAt = DateTime.UtcNow
        };

        _context.Attachments.Add(attachment);
        await _context.SaveChangesAsync();
        return new AttachmentDto(attachment.Id, attachment.CardId, attachment.FileName, attachment.FilePath, attachment.UploadedAt);
    }

    public async Task<AttachmentDto> UploadAsync(Guid cardId, IFormFile file)
    {
        var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = Path.GetRandomFileName() + Path.GetExtension(file.FileName);
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using (var stream = File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        var attachment = new Attachment
        {
            Id = Guid.NewGuid(),
            CardId = cardId,
            FileName = file.FileName,
            FilePath = $"/uploads/{fileName}",
            UploadedAt = DateTime.UtcNow
        };

        _context.Attachments.Add(attachment);
        await _context.SaveChangesAsync();
        return new AttachmentDto(attachment.Id, attachment.CardId, attachment.FileName, attachment.FilePath, attachment.UploadedAt);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var attachment = await _context.Attachments.FindAsync(id);
        if (attachment is null) return false;

        _context.Attachments.Remove(attachment);
        await _context.SaveChangesAsync();
        return true;
    }
}
