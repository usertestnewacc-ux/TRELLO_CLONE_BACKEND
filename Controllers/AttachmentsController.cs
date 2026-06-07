using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TrelloCloneAPI.DTOs;
using TrelloCloneAPI.Services;

namespace TrelloCloneAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttachmentsController : ControllerBase
{
    private readonly IAttachmentService _attachmentService;

    public AttachmentsController(IAttachmentService attachmentService)
    {
        _attachmentService = attachmentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var attachments = await _attachmentService.GetAllAsync();
        return Ok(attachments);
    }

    [HttpGet("card/{cardId:guid}")]
    public async Task<IActionResult> GetByCardId(Guid cardId)
    {
        var attachments = await _attachmentService.GetByCardIdAsync(cardId);
        return Ok(attachments);
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] UploadAttachmentDto dto)
    {
        if (dto.File is null || dto.File.Length == 0)
        {
            return BadRequest(new { error = "A valid file is required." });
        }

        var attachment = await _attachmentService.UploadAsync(dto.CardId, dto.File);
        return CreatedAtAction(nameof(Get), new { id = attachment.Id }, attachment);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var attachment = await _attachmentService.GetByIdAsync(id);
        return attachment is null ? NotFound() : Ok(attachment);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAttachmentDto dto)
    {
        var attachment = await _attachmentService.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = attachment.Id }, attachment);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _attachmentService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
