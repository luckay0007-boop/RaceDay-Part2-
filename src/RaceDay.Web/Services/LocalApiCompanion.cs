using System.Diagnostics;
using System.Net.Sockets;

namespace RaceDay.Web.Services;

/// <summary>
/// Ensures that when running locally in Visual Studio or the CLI with only RaceDay.Web started,
/// the companion RaceDay.Api process is automatically started on localhost:52070 if not already listening.
/// </summary>
public sealed class LocalApiCompanion : IHostedService, IDisposable
{
    private readonly Uri _apiBaseUri;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<LocalApiCompanion> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private Process? _startedProcess;

    public LocalApiCompanion(
        IConfiguration configuration,
        IWebHostEnvironment env,
        ILogger<LocalApiCompanion> logger)
    {
        var rawUrl = Environment.GetEnvironmentVariable("API_BASE_URL")
            ?? configuration["ApiSettings:BaseUrl"]
            ?? "http://localhost:52070";

        _apiBaseUri = new Uri(rawUrl.TrimEnd('/') + "/");
        _env = env;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (IsLocalLoopback())
        {
            // Kick off check in background on startup so Web boots immediately while API spins up
            _ = Task.Run(() => EnsureApiRunningAsync(CancellationToken.None));
        }
        await Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        DisposeProcess();
        return Task.CompletedTask;
    }

    public async Task<bool> EnsureApiRunningAsync(CancellationToken ct = default)
    {
        if (!IsLocalLoopback())
            return false;

        if (await IsPortOpenAsync(_apiBaseUri.Host, _apiBaseUri.Port, ct))
            return true;

        await _lock.WaitAsync(ct);
        try
        {
            // Wait briefly in case Visual Studio Multiple Startup Projects is already starting RaceDay.Api
            for (var i = 0; i < 4; i++)
            {
                if (await IsPortOpenAsync(_apiBaseUri.Host, _apiBaseUri.Port, ct))
                    return true;
                await Task.Delay(250, ct);
            }

            if (_startedProcess is { HasExited: false })
            {
                return await WaitForApiPortAsync(TimeSpan.FromSeconds(10), ct);
            }

            var apiProjectDir = FindApiProjectDirectory();
            if (apiProjectDir is null)
            {
                _logger.LogWarning("Could not locate src/RaceDay.Api directory to auto-start the API.");
                return false;
            }

            var dllPath = FindBuiltApiDll(apiProjectDir);
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = apiProjectDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };

            var listenUrl = $"{_apiBaseUri.Scheme}://{_apiBaseUri.Host}:{_apiBaseUri.Port}";
            if (dllPath is not null)
            {
                psi.ArgumentList.Add(dllPath);
                psi.ArgumentList.Add("--urls");
                psi.ArgumentList.Add(listenUrl);
            }
            else
            {
                var csprojPath = Path.Combine(apiProjectDir, "RaceDay.Api.csproj");
                psi.ArgumentList.Add("run");
                psi.ArgumentList.Add("--project");
                psi.ArgumentList.Add(csprojPath);
                psi.ArgumentList.Add("--no-launch-profile");
                psi.ArgumentList.Add("--urls");
                psi.ArgumentList.Add(listenUrl);
            }

            psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
            psi.Environment["ASPNETCORE_URLS"] = listenUrl;

            _logger.LogInformation("Auto-starting companion RaceDay.Api process on {ListenUrl}...", listenUrl);
            _startedProcess = Process.Start(psi);

            return await WaitForApiPortAsync(TimeSpan.FromSeconds(15), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to auto-start companion RaceDay.Api process.");
            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    private bool IsLocalLoopback() =>
        _apiBaseUri.IsLoopback &&
        (string.Equals(_apiBaseUri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(_apiBaseUri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase));

    private string? FindApiProjectDirectory()
    {
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(_env.ContentRootPath, "..", "RaceDay.Api")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "RaceDay.Api"))
        };

        return candidates.FirstOrDefault(dir => File.Exists(Path.Combine(dir, "RaceDay.Api.csproj")));
    }

    private static string? FindBuiltApiDll(string apiProjectDir)
    {
        var candidates = new[]
        {
            Path.Combine(apiProjectDir, "bin", "Debug", "net10.0", "RaceDay.Api.dll"),
            Path.Combine(apiProjectDir, "bin", "Release", "net10.0", "RaceDay.Api.dll")
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private async Task<bool> WaitForApiPortAsync(TimeSpan timeout, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout && !ct.IsCancellationRequested)
        {
            if (await IsPortOpenAsync(_apiBaseUri.Host, _apiBaseUri.Port, ct))
            {
                // Give Kestrel an extra moment to finish route registration
                await Task.Delay(150, ct);
                return true;
            }
            await Task.Delay(250, ct);
        }
        return false;
    }

    private static async Task<bool> IsPortOpenAsync(string host, int port, CancellationToken ct)
    {
        try
        {
            using var tcp = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromMilliseconds(350));
            await tcp.ConnectAsync(host, port, cts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void DisposeProcess()
    {
        try
        {
            if (_startedProcess is { HasExited: false })
            {
                _startedProcess.Kill(entireProcessTree: true);
                _startedProcess.Dispose();
            }
        }
        catch
        {
            // Ignore shutdown errors
        }
        finally
        {
            _startedProcess = null;
        }
    }

    public void Dispose()
    {
        DisposeProcess();
        _lock.Dispose();
    }
}
