using System.Diagnostics;
using System.Net.Http.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Xunit;

namespace RealEstate.Tests.E2E;

public class E2eFixture : IAsyncLifetime
{
    public HttpClient Client { get; private set; } = default!;
    public string BaseAddress { get; private set; } = "http://localhost:5244";

    private IContainer _mongo = default!;
    private Process? _apiProcess;

    public async Task InitializeAsync()
    {
        // Start MongoDB container
        _mongo = new ContainerBuilder()
            .WithImage("mongo:7")
            .WithName($"mongo-e2e-{Guid.NewGuid():N}")
            .WithPortBinding(27027, 27017)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(27017))
            .Build();
        await _mongo.StartAsync();

        // Start API process on fixed port with env vars
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var apiProj = Path.Combine(repoRoot, "RealEstate.Api", "RealEstate.Api.csproj");
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{apiProj}\" -c Release",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        // Configure Mongo and CORS via env vars
        startInfo.Environment["ASPNETCORE_URLS"] = BaseAddress;
        startInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";
        startInfo.Environment["MongoDb__ConnectionString"] = "mongodb://localhost:27027";
        startInfo.Environment["MongoDb__Database"] = "realestate_e2e";
        startInfo.Environment["CORS_ALLOWED_ORIGINS"] = "http://localhost:3000";
        startInfo.Environment["SWAGGER_ENABLED"] = "false";

        _apiProcess = Process.Start(startInfo)!;

        // wait until /health responds
        using var bootstrapClient = new HttpClient { BaseAddress = new Uri(BaseAddress) };
        var sw = Stopwatch.StartNew();
        Exception? lastEx = null;
        while (sw.Elapsed < TimeSpan.FromSeconds(60))
        {
            try
            {
                var resp = await bootstrapClient.GetAsync("/health");
                if (resp.IsSuccessStatusCode)
                {
                    Client = new HttpClient { BaseAddress = new Uri(BaseAddress) };
                    return;
                }
            }
            catch (Exception ex)
            {
                lastEx = ex;
            }
            await Task.Delay(500);
        }

        throw new TimeoutException($"API did not become healthy in time. Last error: {lastEx?.Message}");
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (_apiProcess is not null && !_apiProcess.HasExited)
        {
            try { _apiProcess.Kill(entireProcessTree: true); }
            catch { /* ignore */ }
        }
        if (_mongo is not null)
        {
            await _mongo.DisposeAsync();
        }
    }
}


