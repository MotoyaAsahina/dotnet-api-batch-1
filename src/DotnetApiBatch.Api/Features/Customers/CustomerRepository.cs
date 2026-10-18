using DotnetApiBatch.Data;
using Microsoft.EntityFrameworkCore;

namespace DotnetApiBatch.Api.Features.Customers;

public class CustomerRepository(AppDbContext db)
{
    public Task<bool> ExistsAsync(long id, CancellationToken cancellationToken) =>
        db.Customers.AnyAsync(c => c.Id == id, cancellationToken);
}
