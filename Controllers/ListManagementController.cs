using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrelloCloneAPI.DTOs;
using TrelloCloneAPI.Services;

namespace TrelloCloneAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ListManagementController : ControllerBase
{
    private readonly IListManagementService _listManagementService;

    public ListManagementController(IListManagementService listManagementService)
    {
        _listManagementService = listManagementService;
    }

    [HttpGet("board/{boardId:guid}")]
    public async Task<IActionResult> GetByBoardId(Guid boardId)
    {
        var lists = await _listManagementService.GetByBoardIdAsync(boardId);
        return Ok(lists);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateListDto dto)
    {
        var list = await _listManagementService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetByBoardId), new { boardId = list.BoardId }, list);
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> Reorder(ReorderListsDto dto)
    {
        var reordered = await _listManagementService.ReorderAsync(dto);
        return Ok(reordered);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _listManagementService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
