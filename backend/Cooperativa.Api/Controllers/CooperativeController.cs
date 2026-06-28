using Cooperativa.Api.Common;
using Cooperativa.Api.Contracts;
using Cooperativa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cooperativa.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CooperativeController : ControllerBase
{
    private readonly CooperativeService _coops;
    private readonly RoomService _room;

    public CooperativeController(CooperativeService coops, RoomService room)
    {
        _coops = coops;
        _room = room;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCooperativeRequest req)
    {
        var result = await _coops.CreateAsync(User.GetUserId(), req.Name);
        if (result is null) return BadRequest("Ya perteneces a una cooperativa.");
        return Ok(new CreateCooperativeResponse(result.Value.CooperativeId, result.Value.InviteCode));
    }

    [HttpPost("join")]
    public async Task<IActionResult> Join(JoinCooperativeRequest req)
    {
        var ok = await _coops.JoinAsync(User.GetUserId(), req.InviteCode);
        return ok ? Ok() : BadRequest("Código inválido o la cooperativa ya está completa.");
    }

    [HttpGet("state")]
    public async Task<IActionResult> State()
    {
        var dto = await _room.GetStateForUserAsync(User.GetUserId());
        return dto is null ? NotFound("Aún no perteneces a ninguna cooperativa.") : Ok(dto);
    }

    [HttpGet("blooms")]
    public async Task<IActionResult> Blooms()
    {
        var blooms = await _room.GetBloomsForUserAsync(User.GetUserId());
        return Ok(blooms);
    }

    [HttpGet("garden")]
    public async Task<IActionResult> Garden()
    {
        var garden = await _room.GetGardenForUserAsync(User.GetUserId());
        return Ok(garden);
    }

    [HttpGet("plant/{id:guid}")]
    public async Task<IActionResult> Plant(Guid id)
    {
        var detail = await _room.GetPlantDetailForUserAsync(User.GetUserId(), id);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpGet("photos")]
    public async Task<IActionResult> Photos()
    {
        var photos = await _room.GetPhotosForUserAsync(User.GetUserId());
        return Ok(photos);
    }

    [HttpPost("photo")]
    public async Task<IActionResult> SetPhoto(SetPhotoRequest req)
    {
        await _room.SetPhotoAsync(User.GetUserId(), req.Slot, req.DataUrl);
        return Ok();
    }

    [HttpDelete("photo/{slot:int}")]
    public async Task<IActionResult> DeletePhoto(int slot)
    {
        await _room.DeletePhotoAsync(User.GetUserId(), slot);
        return Ok();
    }
}
