using NetArchTest.Rules;

namespace DotnetApiBatch.Data.Tests;

/// <summary>
/// Data の依存関係のルール。ドメイン間の依存の向きを固定しておき、
/// ドメインを別のプロジェクトに切り出すときに参照が循環しないようにする。
/// </summary>
public class ArchitectureTests
{
    private static readonly Types DataTypes = Types.InAssembly(typeof(AppDbContext).Assembly);

    /// <summary>
    /// 依存してよい向きは Orders → Customers / Catalog だけ。
    /// Customers と Catalog は、Orders にも互いにも依存しない。
    /// </summary>
    [Theory]
    [InlineData("DotnetApiBatch.Data.Customers", "DotnetApiBatch.Data.Orders")]
    [InlineData("DotnetApiBatch.Data.Customers", "DotnetApiBatch.Data.Catalog")]
    [InlineData("DotnetApiBatch.Data.Catalog", "DotnetApiBatch.Data.Orders")]
    [InlineData("DotnetApiBatch.Data.Catalog", "DotnetApiBatch.Data.Customers")]
    public void Domain_does_not_depend_on_disallowed_domain(string domain, string disallowed)
    {
        var result = DataTypes
            .That().ResideInNamespace(domain)
            .ShouldNot().HaveDependencyOn(disallowed)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"{domain} が {disallowed} に依存しています: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    /// <summary>
    /// Data は API / Batch 共通のライブラリなので、どちらのアプリにも ASP.NET Core にも依存しない。
    /// </summary>
    [Fact]
    public void Data_does_not_depend_on_apps()
    {
        var result = DataTypes
            .ShouldNot().HaveDependencyOnAny("DotnetApiBatch.Api", "DotnetApiBatch.Batch", "Microsoft.AspNetCore")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Data がアプリに依存しています: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
