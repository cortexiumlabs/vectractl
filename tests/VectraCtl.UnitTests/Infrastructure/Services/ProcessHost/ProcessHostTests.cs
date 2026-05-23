using FluentAssertions;
using NSubstitute;
using System.Diagnostics;
using VectraCtl.Infrastructure.Services.ProcessHost;

namespace VectraCtl.UnitTests.Infrastructure.Services.ProcessHost;

public class DefaultProcessProviderTests
{
    [Fact]
    public void GetProcessesByName_UnknownProcess_ReturnsEmptyArray()
    {
        var provider = new DefaultProcessProvider();
        var result = provider.GetProcessesByName("xyzzy_nonexistent_process_abc123", ".");
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetProcessesByName_ReturnsIProcessWrapperInstances()
    {
        // Use the current process name which is guaranteed to exist
        var provider = new DefaultProcessProvider();
        var currentName = Process.GetCurrentProcess().ProcessName;

        var result = provider.GetProcessesByName(currentName, ".");
        result.Should().AllBeOfType<ProcessWrapper>();
    }
}

public class ProcessWrapperTests
{
    [Fact]
    public void ProcessName_ReturnsProcessName()
    {
        var process = Process.GetCurrentProcess();
        var wrapper = new ProcessWrapper(process);

        wrapper.ProcessName.Should().Be(process.ProcessName);
    }
}
