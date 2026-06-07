using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrelloCloneAPI.DTOs;
using TrelloCloneAPI.Services;

namespace TrelloCloneAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ListsController : ControllerBase
{
    private readonly IListService _listService;

    public ListsController(IListService listService)
    {
        _listService = listService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var lists = await _listService.GetAllAsync();
        return Ok(lists);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var list = await _listService.GetByIdAsync(id);
        return list is null ? NotFound() : Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateListDto dto)
    {
        var list = await _listService.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = list.Id }, list);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateListDto dto)
    {
        var list = await _listService.UpdateAsync(id, dto);
        return list is null ? NotFound() : Ok(list);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _listService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
