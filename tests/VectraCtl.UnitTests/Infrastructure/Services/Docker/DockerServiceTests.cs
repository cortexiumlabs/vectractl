using FluentAssertions;
using NSubstitute;
using VectraCtl.Core.Models.Docker;
using VectraCtl.Core.Services.Docker;
using VectraCtl.Infrastructure.Services.Docker;

namespace VectraCtl.UnitTests.Infrastructure.Services.Docker;

public class DockerServiceTests
{
    private readonly IDockerProcessRunner _runner;
    private readonly DockerService _sut;

    public DockerServiceTests()
    {
        _runner = Substitute.For<IDockerProcessRunner>();
        _sut = new DockerService(_runner);
    }

    [Fact]
    public void Constructor_NullRunner_Throws()
    {
        var act = () => new DockerService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("runner");
    }

    // ── GetDockerModeAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetDockerModeAsync_ReturnsLinux_WhenOutputIsLinux()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), false, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0, Output = "linux" });

        var mode = await _sut.GetDockerModeAsync();
        mode.Should().Be("Linux");
    }

    [Fact]
    public async Task GetDockerModeAsync_ReturnsWindows_WhenOutputIsWindows()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), false, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0, Output = "windows" });

        var mode = await _sut.GetDockerModeAsync();
        mode.Should().Be("Windows");
    }

    [Fact]
    public async Task GetDockerModeAsync_ReturnsUnknown_WhenOutputIsUnrecognized()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), false, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0, Output = "darwin" });

        var mode = await _sut.GetDockerModeAsync();
        mode.Should().Be("Unknown");
    }

    [Fact]
    public async Task GetDockerModeAsync_ReturnsUnknown_WhenCommandFails()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), false, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 1, Output = "" });

        var mode = await _sut.GetDockerModeAsync();
        mode.Should().Be("Unknown");
    }

    [Fact]
    public async Task GetDockerModeAsync_ReturnsUnknown_WhenOutputIsWhitespace()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), false, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0, Output = "   " });

        var mode = await _sut.GetDockerModeAsync();
        mode.Should().Be("Unknown");
    }

    // ── IsDockerAvailableAsync ───────────────────────────────────────────────

    [Fact]
    public async Task IsDockerAvailableAsync_ReturnsTrue_WhenCommandSucceedsWithOutput()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), false, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0, Output = "Server info..." });

        var available = await _sut.IsDockerAvailableAsync();
        available.Should().BeTrue();
    }

    [Fact]
    public async Task IsDockerAvailableAsync_ReturnsFalse_WhenCommandFails()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), false, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 1, Output = "" });

        var available = await _sut.IsDockerAvailableAsync();
        available.Should().BeFalse();
    }

    // ── PullImageAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task PullImageAsync_NullImageName_Throws()
    {
        var act = () => _sut.PullImageAsync(null!, "tag");
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task PullImageAsync_PassesCorrectArguments()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), true, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0 });

        await _sut.PullImageAsync("myimage", "v1");

        await _runner.Received(1).RunAsync(
            Arg.Is<IEnumerable<string>>(a => a.Contains("pull") && a.Any(x => x.Contains("myimage"))),
            true,
            Arg.Any<CancellationToken>());
    }

    // ── RunContainerAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task RunContainerAsync_NullOptions_Throws()
    {
        var act = () => _sut.RunContainerAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData("", "tag", "name", "/host", "/cont", "ImageName")]
    [InlineData("img", "", "name", "/host", "/cont", "Tag")]
    [InlineData("img", "tag", "", "/host", "/cont", "ContainerName")]
    [InlineData("img", "tag", "name", "", "/cont", "HostDataPath")]
    [InlineData("img", "tag", "name", "/host", "", "ContainerDataPath")]
    public async Task RunContainerAsync_MissingRequiredField_Throws(
        string image, string tag, string container, string host, string cont, string paramName)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            // HostDataPath must be a real path if it is provided; use temp dir when not blank
            var effectiveHost = host == "" ? "" : tempDir;
            Directory.CreateDirectory(tempDir);

            var opts = new DockerRunOptions
            {
                ImageName = image,
                Tag = tag,
                ContainerName = container,
                HostDataPath = effectiveHost,
                ContainerDataPath = cont
            };

            var act = () => _sut.RunContainerAsync(opts);
            await act.Should().ThrowAsync<ArgumentException>();
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task RunContainerAsync_DetachedFlag_IncludesDashD()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            _runner.RunAsync(Arg.Any<IEnumerable<string>>(), true, Arg.Any<CancellationToken>())
                   .Returns(new DockerCommandResult { ExitCode = 0 });

            await _sut.RunContainerAsync(new DockerRunOptions
            {
                ImageName = "img",
                Tag = "v1",
                ContainerName = "cnt",
                HostPort = 8080,
                ContainerPort = 8080,
                HostDataPath = tempDir,
                ContainerDataPath = "/app/data",
                Detached = true
            });

            await _runner.Received(1).RunAsync(
                Arg.Is<IEnumerable<string>>(a => a.Contains("-d")),
                true,
                Arg.Any<CancellationToken>());
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task RunContainerAsync_WithAdditionalArguments_PassesThem()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            _runner.RunAsync(Arg.Any<IEnumerable<string>>(), true, Arg.Any<CancellationToken>())
                   .Returns(new DockerCommandResult { ExitCode = 0 });

            await _sut.RunContainerAsync(new DockerRunOptions
            {
                ImageName = "img",
                Tag = "v1",
                ContainerName = "cnt",
                HostPort = 8080,
                ContainerPort = 8080,
                HostDataPath = tempDir,
                ContainerDataPath = "/app/data",
                Detached = false,
                AdditionalArguments = "--start"
            });

            await _runner.Received(1).RunAsync(
                Arg.Is<IEnumerable<string>>(a => a.Contains("--start")),
                true,
                Arg.Any<CancellationToken>());
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    // ── StartContainerAsync / StopContainerAsync ─────────────────────────────

    [Fact]
    public async Task StartContainerAsync_EmptyName_Throws()
    {
        var act = () => _sut.StartContainerAsync("");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task StartContainerAsync_PassesNameAsArgument()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), true, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0 });

        await _sut.StartContainerAsync("my-container");

        await _runner.Received(1).RunAsync(
            Arg.Is<IEnumerable<string>>(a => a.Contains("start") && a.Contains("my-container")),
            true,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StopContainerAsync_EmptyName_Throws()
    {
        var act = () => _sut.StopContainerAsync("");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task StopContainerAsync_PassesNameAsArgument()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), true, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0 });

        await _sut.StopContainerAsync("my-container");

        await _runner.Received(1).RunAsync(
            Arg.Is<IEnumerable<string>>(a => a.Contains("stop") && a.Contains("my-container")),
            true,
            Arg.Any<CancellationToken>());
    }

    // ── RemoveContainerAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task RemoveContainerAsync_EmptyName_Throws()
    {
        var act = () => _sut.RemoveContainerAsync("", false);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RemoveContainerAsync_Force_IncludesDashF()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), true, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0 });

        await _sut.RemoveContainerAsync("my-container", force: true);

        await _runner.Received(1).RunAsync(
            Arg.Is<IEnumerable<string>>(a => a.Contains("rm") && a.Contains("-f")),
            true,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveContainerAsync_NotForce_ExcludesDashF()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), true, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0 });

        await _sut.RemoveContainerAsync("my-container", force: false);

        await _runner.Received(1).RunAsync(
            Arg.Is<IEnumerable<string>>(a => a.Contains("rm") && !a.Contains("-f")),
            true,
            Arg.Any<CancellationToken>());
    }

    // ── ContainerExistsAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task ContainerExistsAsync_EmptyName_Throws()
    {
        var act = () => _sut.ContainerExistsAsync("");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ContainerExistsAsync_ReturnsTrue_WhenOutputPresent()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), false, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0, Output = "abc123" });

        var exists = await _sut.ContainerExistsAsync("my-container");
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ContainerExistsAsync_ReturnsFalse_WhenOutputEmpty()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), false, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0, Output = "" });

        var exists = await _sut.ContainerExistsAsync("my-container");
        exists.Should().BeFalse();
    }

    // ── IsContainerRunningAsync ───────────────────────────────────────────────

    [Fact]
    public async Task IsContainerRunningAsync_EmptyName_Throws()
    {
        var act = () => _sut.IsContainerRunningAsync("");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task IsContainerRunningAsync_ReturnsTrue_WhenOutputPresent()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), false, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0, Output = "abc123" });

        var running = await _sut.IsContainerRunningAsync("my-container");
        running.Should().BeTrue();
    }

    [Fact]
    public async Task IsContainerRunningAsync_ReturnsFalse_WhenOutputEmpty()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), false, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0, Output = "" });

        var running = await _sut.IsContainerRunningAsync("my-container");
        running.Should().BeFalse();
    }

    // ── TailLogsAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task TailLogsAsync_EmptyName_Throws()
    {
        var act = () => _sut.TailLogsAsync("");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task TailLogsAsync_PassesLogsAndNameArguments()
    {
        _runner.RunAsync(Arg.Any<IEnumerable<string>>(), true, Arg.Any<CancellationToken>())
               .Returns(new DockerCommandResult { ExitCode = 0 });

        await _sut.TailLogsAsync("my-container");

        await _runner.Received(1).RunAsync(
            Arg.Is<IEnumerable<string>>(a => a.Contains("logs") && a.Contains("my-container")),
            true,
            Arg.Any<CancellationToken>());
    }
}
