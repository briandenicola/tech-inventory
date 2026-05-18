using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ConnectionString
            });
        });

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
