using MediatR;
using Reservation.Application.DTOs;
using Reservation.Application.Interfaces;

namespace Reservation.Application.Customers.Queries;

public record SearchCustomerQuery(string? Phone, string? Email) : IRequest<CustomerDto?>;

public sealed class SearchCustomerQueryHandler(ICustomerRepository customerRepo)
    : IRequestHandler<SearchCustomerQuery, CustomerDto?>
{
    public async Task<CustomerDto?> Handle(SearchCustomerQuery query, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(query.Phone))
        {
            var byPhone = await customerRepo.GetByPhoneAsync(query.Phone, ct);
            if (byPhone is not null) return byPhone.ToDto();
        }

        if (!string.IsNullOrWhiteSpace(query.Email))
        {
            var byEmail = await customerRepo.GetByEmailAsync(query.Email, ct);
            if (byEmail is not null) return byEmail.ToDto();
        }

        return null;
    }
}
