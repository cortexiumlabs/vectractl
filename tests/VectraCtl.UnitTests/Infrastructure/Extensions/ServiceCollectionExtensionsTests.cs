using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using VectraCtl.Core.Services.Configuration;
using VectraCtl.Core.Services.Docker;
using VectraCtl.Core.Services.Extractor;
using VectraCtl.Core.Services.Github;
using VectraCtl.Core.Services.ProcessHost;
using VectraCtl.Infrastructure.Extensions;

namespace VectraCtl.UnitTests.Infrastructure.Extensions;

public class InfrastructureServiceCollectionExtensionsTests
{
    [Fact]
    public void AddInfrastructure_RegistersAllExpectedServices()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure();

        var serviceTypes = services.Select(d => d.ServiceType).ToList();

        serviceTypes.Should().Contain(typeof(IGitHubReleaseManager));
        serviceTypes.Should().Contain(typeof(IProcessHandler));
        serviceTypes.Should().Contain(typeof(IArchiveExtractor));
        serviceTypes.Should().Contain(typeof(IAppSettingsService));
        serviceTypes.Should().Contain(typeof(IDockerService));
    }

    [Fact]
    public void AddInfrastructure_ReturnsServiceCollection_ForChaining()
    {
        var result = new ServiceCollection().AddInfrastructure();

        result.Should().NotBeNull();
    }
}
