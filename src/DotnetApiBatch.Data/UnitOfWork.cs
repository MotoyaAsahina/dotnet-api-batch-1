namespace DotnetApiBatch.Data;

/// <summary>
/// リポジトリで追加・変更したエンティティを、まとめて DB に保存する。
/// 同じスコープの中では、どのリポジトリも同じ <see cref="AppDbContext"/>（Scoped）を使うので、
/// 複数のリポジトリで行った変更も、<see cref="SaveChangesAsync"/> 1 回で 1 つのトランザクションとして保存される。
/// </summary>
/// <remarks>
/// API / Batch で共有するので、保存（確定の境界）だけを持たせる。
/// リポジトリはアプリごとに異なるので、プロパティとしては持たない。
/// </remarks>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
