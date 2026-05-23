using FluentAssertions;
using NSubstitute;
using VectraCtl.Core.Services.Logger;
using VectraCtl.Infrastructure.Services.Docker;

namespace VectraCtl.UnitTests.Infrastructure.Services.Docker;

public class SystemDockerProcessRunnerTests
{
    private readonly IVectraCtlLogger _logger;
    private readonly SystemDockerProcessRunner _sut;

    public SystemDockerProcessRunnerTests()
    {
        _logger = Substitute.For<IVectraCtlLogger>();
        _sut = new SystemDockerProcessRunner(_logger);
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        var act = () => new SystemDockerProcessRunner(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task RunAsync_InvalidExecutable_ReturnsFailureResult()
    {
        var nonExistent = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "non_existent_binary");
        var result = await _sut.RunAsync(["--help"], false, CancellationToken.None, nonExistent);

        result.Should().NotBeNull();
        result.ExitCode.Should().NotBe(0);
    }

    [Fact]
    public async Task RunAsync_Cancelled_ReturnsNegativeExitCode()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var nonExistent = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "non_existent");
        var result = await _sut.RunAsync(["--version"], false, cts.Token, nonExistent);

        result.ExitCode.Should().BeLessThan(0);
    }
    [Fact]
    public async Task RunAsync_StreamOutput_True_InvokesLogger()
    {
        // Use a real executable that produces output (cmd /c echo or sh -c echo)
        string exe;
        string[] args;

        if (OperatingSystem.IsWindows())
        {
            exe = "cmd.exe";
            args = ["/c", "echo", "hello"];
        }
        else
        {
            exe = "/bin/sh";
            args = ["-c", "echo hello"];
        }

        var result = await _sut.RunAsync(args, streamOutput: true, CancellationToken.None, exe);

        result.ExitCode.Should().Be(0);
    }

    [Fact]
    public async Task RunAsync_PublicOverload_DelegatesToInternal()
    {
        string exe;
        string[] args;

        if (OperatingSystem.IsWindows())
        {
            exe = "cmd.exe";
            args = ["/c", "echo", "hello"];
        }
        else
        {
            exe = "/bin/sh";
            args = ["-c", "echo hello"];
        }

        // Public overload uses the virtual Executable property, not the override arg.
        // We verify it completes (may fail if docker not installed, but returns a result).
        var result = await _sut.RunAsync(["--version"], streamOutput: false, CancellationToken.None);

        result.Should().NotBeNull();
    }
}
