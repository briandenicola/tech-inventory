using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TechInventory.IntegrationTests.Support;

namespace TechInventory.IntegrationTests;

public class IntegrationTestFactory<TMarker> : WebApplicationFactory<Program>
    where TMarker : class
{
    private readonly List<Type> _registeredDbContextTypes = [];
    private readonly string _databaseDirectory;
    private readonly string _databasePath;
    private bool _disposed;

    public IntegrationTestFactory()
    {
        _databaseDirectory = Path.Combine(AppContext.BaseDirectory, "integration-databases");
        Directory.CreateDirectory(_databaseDirectory);

        _databasePath = Path.Combine(
            _databaseDirectory,
            $"{SanitizeFileName(typeof(TMarker).Name)}-{Guid.NewGuid():N}.sqlite");
    }

    public string ConnectionString => $"Data Source={_databasePath}";

    // Allow derived classes to override the environment
    protected virtual string Environment => "Development";

    /// <summary>
    /// Role stamped onto every authenticated request by <see cref="TestAuthHandler"/>.
    /// Defaults to Admin; override in a derived factory (e.g.
    /// <see cref="Support.MemberRoleIntegrationTestFactory{T}"/>) to flip the
    /// caller's role for role-based authorization tests.
    /// </summary>
    protected virtual string TestAuthRole => "Admin";

    /// <summary>
    /// When <c>true</c> (default), the factory swaps Program.cs's auth wiring
    /// for the in-memory <see cref="TestAuthHandler"/> so every request is
    /// authenticated as <see cref="TestAuthRole"/>. Override to <c>false</c>
    /// in factories that need to exercise the real auth pipeline — e.g. the
    /// JWT validation tests in <c>AuthIntegrationTests</c>, or the local
    /// HS256 flow in <c>LocalAuthEndpointTests</c>.
    /// </summary>
    protected virtual bool UseTestAuth => true;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environment);
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ConnectionString,
                // Disable the local-admin seed in test hosts. Each test class
                // gets its own SQLite file and uses TestAuthHandler for auth,
                // so the seed service would just create noise.
                ["Auth:Local:SeedEnabled"] = "false"
            });
        });

        if (UseTestAuth)
        {
            // Swap whatever auth Program.cs registered for a TestAuthHandler
            // that authenticates every request as the configured role. Lives
            // in the test project so the production binary stays bypass-free.
            builder.ConfigureTestServices(services =>
            {
                services.Configure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                });

                var roleForRequests = TestAuthRole;
                services.AddAuthentication()
                    .AddScheme<TestAuthHandlerOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName,
                        options => options.Role = roleForRequests);
            });
        }

        builder.ConfigureServices(services =>
        {
            _registeredDbContextTypes.Clear();
            _registeredDbContextTypes.AddRange(
                services
                    .Where(descriptor => descriptor.ServiceType.IsClass && descriptor.ServiceType.IsSubclassOf(typeof(DbContext)))
                    .Select(descriptor => descriptor.ServiceType)
                    .Distinct());

            // TODO: Hicks/Apone - if AppDbContext lands with a non-standard registration path,
            // replace that DbContext registration here so integration tests always point at ConnectionStrings:Default.
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        ApplyMigrations(host);

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            DeleteDatabaseFiles();
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        DeleteDatabaseFiles();
    }

    private void ApplyMigrations(IHost host)
    {
        if (_registeredDbContextTypes.Count == 0)
        {
            return;
        }

        using var scope = host.Services.CreateScope();

        foreach (var dbContextType in _registeredDbContextTypes)
        {
            if (scope.ServiceProvider.GetService(dbContextType) is not DbContext dbContext)
            {
                continue;
            }

            if (!dbContext.Database.GetMigrations().Any())
            {
                continue;
            }

            dbContext.Database.Migrate();
        }
    }

    private void DeleteDatabaseFiles()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        DeleteIfExists(_databasePath);
        DeleteIfExists($"{_databasePath}-wal");
        DeleteIfExists($"{_databasePath}-shm");
    }

    private static void DeleteIfExists(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return;
            }

            const int maxAttempts = 5;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    File.Delete(path);
                    return;
                }
                catch (IOException) when (attempt < maxAttempts)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Thread.Sleep(100);
                }
                catch (IOException)
                {
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    return;
                }
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string SanitizeFileName(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitizedCharacters = value.Select(character => invalidCharacters.Contains(character) ? '-' : character);

        return new string(sanitizedCharacters.ToArray());
    }
}
