using MediatR;
using Reservation.Application.DTOs;
using Reservation.Application.Exceptions;
using Reservation.Application.Interfaces;

namespace Reservation.Application.Customers.Queries;

public record GetCustomerByIdQuery(Guid CustomerId) : IRequest<CustomerDto>;

public sealed class GetCustomerByIdQueryHandler(ICustomerRepository customerRepo)
    : IRequestHandler<GetCustomerByIdQuery, CustomerDto>
{
    public async Task<CustomerDto> Handle(GetCustomerByIdQuery query, CancellationToken ct)
    {
        var customer = await customerRepo.GetByIdAsync(query.CustomerId, ct)
            ?? throw new EntityNotFoundException("Customer", query.CustomerId);
        return customer.ToDto();
    }
}
