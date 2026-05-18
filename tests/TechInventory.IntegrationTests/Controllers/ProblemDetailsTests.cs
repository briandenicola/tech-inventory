using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace TechInventory.IntegrationTests.Controllers;

public sealed class ProblemDetailsTests(IntegrationTestFactory<ProblemDetailsTests> factory)
    : ControllerTestBase<ProblemDetailsTests>(factory), IClassFixture<IntegrationTestFactory<ProblemDetailsTests>>
{
    [Fact]
    public async Task ValidationFailure_WhenRequestInvalid_ReturnsValidationProblemDetailsShape()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();
        var request = new { name = string.Empty, website = "not-a-uri", notes = new string('n', 4001) };

        var response = await client.PostAsync("/api/v1/brands", CreateJsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ReadValidationProblemDetailsAsync(response);
        problem.Type.Should().NotBeNullOrWhiteSpace();
        problem.Title.Should().NotBeNullOrWhiteSpace();
        problem.Errors.Keys.Should().Contain(key => string.Equals(key, "Name", StringComparison.OrdinalIgnoreCase));
        problem.Extensions.Keys.Should().Contain(key => string.Equals(key, "code", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UnhandledException_WhenProductionEnvironment_ReturnsProblemDetailsWithoutStackTrace()
    {
        await using var productionFactory = new ProductionProblemDetailsFactory();
        await productionFactory.ResetStateAsync();
        using var client = productionFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/__test/throw");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var problem = await ReadProblemDetailsAsync(response);
        problem.Detail.Should().NotContain("InvalidOperationException");
        problem.Extensions.Keys.Should().NotContain(key => key.Contains("stack", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task MissingResource_WhenRouteDoesNotExist_ReturnsProblemDetailsWithTypeUrl()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/brands/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await ReadProblemDetailsAsync(response);
        Uri.TryCreate(problem.Type, UriKind.Absolute, out _).Should().BeTrue();
        problem.Status.Should().Be((int)HttpStatusCode.NotFound);
    }

    private sealed class ProductionProblemDetailsFactory : IntegrationTestFactory<ProductionProblemDetailsFactory>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IHostEnvironment>(new FixedHostEnvironment("Production"));
                services.AddSingleton<IStartupFilter, ThrowRouteStartupFilter>();
            });
        }

        public async Task ResetStateAsync()
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TechInventory.Infrastructure.Persistence.AppDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.MigrateAsync();
        }

        private sealed class ThrowRouteStartupFilter : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
                => app =>
                {
                    next(app);
                    app.Map("/__test/throw", branch => branch.Run(_ => throw new InvalidOperationException("Simulated exception for ProblemDetailsTests.")));
                };
        }

        private sealed class FixedHostEnvironment(string environmentName) : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = environmentName;

            public string ApplicationName { get; set; } = typeof(Program).Assembly.GetName().Name ?? nameof(Program);

            public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }
    }
}
