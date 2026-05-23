using FluentAssertions;
using NSubstitute;
using VectraCtl.Core.Services.Logger;
using VectraCtl.Infrastructure.Services.ProcessHost;

namespace VectraCtl.UnitTests.Infrastructure.Services.ProcessHost;

public class ProcessHandlerTests
{
    private readonly IVectraCtlLogger _logger;
    private readonly IProcessProvider _processProvider;
    private readonly ProcessHandler _sut;

    public ProcessHandlerTests()
    {
        _logger = Substitute.For<IVectraCtlLogger>();
        _processProvider = Substitute.For<IProcessProvider>();
        _sut = new ProcessHandler(_logger, _processProvider);
    }

    private static IProcessWrapper[] NoProcesses() => [];

    private static IProcessWrapper[] SomeProcesses(params string[] names)
    {
        return names.Select(name =>
        {
            var p = Substitute.For<IProcessWrapper>();
            p.ProcessName.Returns(name);
            return p;
        }).ToArray();
    }

    // IsRunning

    [Fact]
    public void IsRunning_NoProcesses_ReturnsFalse()
    {
        _processProvider.GetProcessesByName("vectra", "localhost").Returns(NoProcesses());

        var result = _sut.IsRunning("vectra", "localhost");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsRunning_ProcessesExist_ReturnsTrue()
    {
        var processes = SomeProcesses("vectra");
        _processProvider.GetProcessesByName("vectra", "localhost").Returns(processes);

        var result = _sut.IsRunning("vectra", "localhost");

        result.Should().BeTrue();
    }

    // Terminate

    [Fact]
    public void Terminate_NoProcesses_DoesNothing()
    {
        _processProvider.GetProcessesByName("vectra", "localhost").Returns(NoProcesses());

        _sut.Terminate("vectra", "localhost");

        _logger.DidNotReceive().Write(Arg.Any<string>());
    }

    [Fact]
    public void Terminate_ProcessesExist_KillsAllAndLogs()
    {
        var processes = SomeProcesses("vectra", "vectra");
        _processProvider.GetProcessesByName("vectra", "localhost").Returns(processes);

        _sut.Terminate("vectra", "localhost");

        foreach (var process in processes)
        {
            process.Received(1).Kill();
        }

        _logger.Received(2).Write(Arg.Any<string>());
    }

    // IsStopped

    [Fact]
    public void IsStopped_NotRunning_ReturnsTrue()
    {
        _processProvider.GetProcessesByName("vectra", "localhost").Returns(NoProcesses());

        var result = _sut.IsStopped("vectra", "localhost", force: false);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsStopped_Running_ForceFalse_ReturnsFalse()
    {
        var processes = SomeProcesses("vectra");
        _processProvider.GetProcessesByName("vectra", "localhost").Returns(processes);

        var result = _sut.IsStopped("vectra", "localhost", force: false);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsStopped_Running_ForceTrue_TerminatesAndReturnsTrue()
    {
        var processes = SomeProcesses("vectra");
        // Called twice: once in IsRunning, once in Terminate
        _processProvider.GetProcessesByName("vectra", "localhost").Returns(processes);

        var result = _sut.IsStopped("vectra", "localhost", force: true);

        result.Should().BeTrue();
        processes[0].Received(1).Kill();
    }
}
