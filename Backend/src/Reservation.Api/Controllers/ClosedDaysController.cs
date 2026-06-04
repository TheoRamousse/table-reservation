using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reservation.Api.Requests;
using Reservation.Application.ClosedDays.Commands;
using Reservation.Application.ClosedDays.Queries;
using Reservation.Application.DTOs;

namespace Reservation.Api.Controllers;

[ApiController]
[Route("api/closed-days")]
[Authorize]
public sealed class ClosedDaysController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ClosedDayDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetAll(CancellationToken ct)
    {
        var dtos = await mediator.Send(new GetClosedDaysQuery(), ct);
        return Ok(new { closedDays = dtos });
    }

    [Authorize(Roles = "Manager,Admin")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult> Declare([FromBody] DeclareClosedDayRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new DeclareClosedDayCommand(request.Date, request.Reason), ct);
        return CreatedAtAction(null, new { id = result.ClosedDay.Id }, new
        {
            closedDay = result.ClosedDay,
            cancelledBookingsCount = result.CancelledBookingsCount
        });
    }
}
