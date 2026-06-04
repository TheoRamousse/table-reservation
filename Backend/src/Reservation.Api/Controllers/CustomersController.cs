using MediatR;
using Microsoft.AspNetCore.Mvc;
using Reservation.Api.Requests;
using Reservation.Application.Bookings.Queries;
using Reservation.Application.Customers.Commands;
using Reservation.Application.Customers.Queries;
using Reservation.Application.DTOs;

namespace Reservation.Api.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController(IMediator mediator) : ControllerBase
{
    // POST /api/customers
    [HttpPost]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CustomerDto>> Create(
        [FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        var dto = await mediator.Send(
            new CreateCustomerCommand(request.FirstName, request.LastName, request.Phone, request.Email), ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    // GET /api/customers/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> GetById(Guid id, CancellationToken ct)
    {
        var dto = await mediator.Send(new GetCustomerByIdQuery(id), ct);
        return Ok(dto);
    }

    // GET /api/customers?phone=...&email=...  →  { "customers": [ ... ] }
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> Search(
        [FromQuery] string? phone, [FromQuery] string? email, CancellationToken ct)
    {
        var dto = await mediator.Send(new SearchCustomerQuery(phone, email), ct);
        var list = dto is null ? [] : new[] { dto };
        return Ok(new { customers = list });
    }

    // GET /api/customers/{id}/bookings  →  { "bookings": [ ... ] }
    [HttpGet("{id:guid}/bookings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetBookings(Guid id, CancellationToken ct)
    {
        var bookings = await mediator.Send(new GetBookingsByCustomerQuery(id), ct);
        return Ok(new { bookings });
    }

    // PATCH /api/customers/{id}/blacklist
    [HttpPatch("{id:guid}/blacklist")]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> UpdateBlacklist(
        Guid id, [FromBody] UpdateBlacklistRequest request, CancellationToken ct)
    {
        if (request.IsBlacklisted)
            return BadRequest(new
            {
                code = "NOT_SUPPORTED",
                message = "Le blacklist automatique se déclenche via les no-shows."
            });

        var dto = await mediator.Send(new LiftBlacklistCommand(id), ct);
        return Ok(dto);
    }
}
