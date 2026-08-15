using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PersonaNest.Infrastructure.Data;

/// <summary>
/// Lets <c>dotnet ef</c> build the context without starting the web host, so migrations run
/// entirely inside the Infrastructure project:
/// <code>
/// dotnet ef migrations add InitialCreate -p PersonaNest.Infrastructure -s PersonaNest.Infrastructure
/// </code>
/// This keeps the approved dependency direction intact - PersonaNest.Web never needs a
/// reference to PersonaNest.Infrastructure (approved option (a), Phase 1 report §5.1).
/// <para>
/// Design-time only. Never used at runtime, and holds no secrets: override the connection
/// string with the <c>PERSONANEST_CONNECTION</c> environment variable when needed.
/// </para>
/// </summary>
public class PersonaNestDbContextFactory : IDesignTimeDbContextFactory<PersonaNestDbContext>
{
    private const string DefaultDesignTimeConnection =
        "Server=(localdb)\\MSSQLLocalDB;Database=PersonaNest;Trusted_Connection=True;" +
        "TrustServerCertificate=True";

    public PersonaNestDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("PERSONANEST_CONNECTION")
            ?? DefaultDesignTimeConnection;

        var options = new DbContextOptionsBuilder<PersonaNestDbContext>()
            .UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(PersonaNestDbContext).Assembly.GetName().Name))
            .Options;

        return new PersonaNestDbContext(options);
    }
}
