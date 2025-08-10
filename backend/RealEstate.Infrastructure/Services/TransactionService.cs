using MongoDB.Driver;
using RealEstate.Infrastructure;

namespace RealEstate.Infrastructure.Services;

public interface ITransactionService
{
    Task<T> ExecuteInTransactionAsync<T>(Func<IClientSessionHandle, Task<T>> operation, CancellationToken ct = default);
    Task ExecuteInTransactionAsync(Func<IClientSessionHandle, Task> operation, CancellationToken ct = default);
}

public sealed class TransactionService : ITransactionService
{
    private readonly MongoContext _ctx;

    public TransactionService(MongoContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<IClientSessionHandle, Task<T>> operation, CancellationToken ct = default)
    {
        using var session = await _ctx.Database.Client.StartSessionAsync(cancellationToken: ct);
        
        try
        {
            session.StartTransaction();
            
            var result = await operation(session);
            
            await session.CommitTransactionAsync(ct);
            
            return result;
        }
        catch
        {
            await session.AbortTransactionAsync(ct);
            throw;
        }
    }

    public async Task ExecuteInTransactionAsync(Func<IClientSessionHandle, Task> operation, CancellationToken ct = default)
    {
        using var session = await _ctx.Database.Client.StartSessionAsync(cancellationToken: ct);
        
        try
        {
            session.StartTransaction();
            
            await operation(session);
            
            await session.CommitTransactionAsync(ct);
        }
        catch
        {
            await session.AbortTransactionAsync(ct);
            throw;
        }
    }
}
