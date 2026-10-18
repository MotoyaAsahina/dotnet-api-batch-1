using DotnetApiBatch.Data;
using Microsoft.EntityFrameworkCore;

namespace DotnetApiBatch.Api.Features.Customers;

public interface ICustomerRepository
{
    Task<bool> ExistsAsync(long id, CancellationToken cancellationToken);
}

public class CustomerRepository(AppDbContext db) : ICustomerRepository
{
    public Task<bool> ExistsAsync(long id, CancellationToken cancellationToken) =>
        db.Customers.AnyAsync(c => c.Id == id, cancellationToken);
}
