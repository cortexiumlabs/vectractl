using FluentAssertions;
using VectraCtl.Core.Exceptions;
using VectraCtl.Core.Models.Configuration;
using VectraCtl.Core.Models.Docker;
using VectraCtl.Core.Serialization;
using VectraCtl.Core.Services.Github;

namespace VectraCtl.UnitTests.Core;

public class VectraCtlExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        var ex = new VectraCtlException("oops");
        ex.Message.Should().Be("oops");
    }

    [Fact]
    public void Constructor_WithMessageAndInner_SetsMessageAndInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new VectraCtlException("outer", inner);

        ex.Message.Should().Be("outer");
        ex.InnerException.Should().BeSameAs(inner);
    }
}

public class AppSettingsTests
{
    [Fact]
    public void DefaultValues_AreExpected()
    {
        var settings = new AppSettings();

        settings.DeploymentMode.Should().Be(DeploymentMode.Binary);
        settings.Docker.Should().NotBeNull();
        settings.Binary.Should().NotBeNull();
    }

    [Fact]
    public void DeploymentMode_CanBeSetToDocker()
    {
        var settings = new AppSettings { DeploymentMode = DeploymentMode.Docker };
        settings.DeploymentMode.Should().Be(DeploymentMode.Docker);
    }
}

public class DockerSettingsTests
{
    [Fact]
    public void DefaultValues_AreExpected()
    {
        var ds = new DockerSettings();

        ds.ImageName.Should().BeEmpty();
        ds.Tag.Should().BeEmpty();
        ds.ContainerName.Should().BeEmpty();
        ds.Port.Should().Be(7080);
        ds.HostDataPath.Should().BeEmpty();
        ds.ContainerDataPath.Should().Be("/app/data");
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var ds = new DockerSettings
        {
            ImageName = "img",
            Tag = "latest",
            ContainerName = "c1",
            Port = 9090,
            HostDataPath = "/host",
            ContainerDataPath = "/container"
        };

        ds.ImageName.Should().Be("img");
        ds.Tag.Should().Be("latest");
        ds.ContainerName.Should().Be("c1");
        ds.Port.Should().Be(9090);
        ds.HostDataPath.Should().Be("/host");
        ds.ContainerDataPath.Should().Be("/container");
    }
}

public class BinarySettingsTests
{
    [Fact]
    public void DefaultVersion_IsNull()
    {
        var bs = new BinarySettings();
        bs.Version.Should().BeNull();
    }

    [Fact]
    public void Version_CanBeSet()
    {
        var bs = new BinarySettings { Version = "1.2.3" };
        bs.Version.Should().Be("1.2.3");
    }
}

public class DockerRunOptionsTests
{
    [Fact]
    public void DefaultValues_AreExpected()
    {
        var opts = new DockerRunOptions();

        opts.ImageName.Should().BeEmpty();
        opts.Tag.Should().BeEmpty();
        opts.ContainerName.Should().BeEmpty();
        opts.HostPort.Should().Be(0);
        opts.ContainerPort.Should().Be(7080);
        opts.HostDataPath.Should().BeEmpty();
        opts.ContainerDataPath.Should().BeEmpty();
        opts.Detached.Should().BeTrue();
        opts.AdditionalArguments.Should().BeEmpty();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var opts = new DockerRunOptions
        {
            ImageName = "img",
            Tag = "v1",
            ContainerName = "cnt",
            HostPort = 8080,
            ContainerPort = 8080,
            HostDataPath = "/h",
            ContainerDataPath = "/c",
            Detached = false,
            AdditionalArguments = "--flag"
        };

        opts.HostPort.Should().Be(8080);
        opts.Detached.Should().BeFalse();
        opts.AdditionalArguments.Should().Be("--flag");
    }
}

public class DockerCommandResultTests
{
    [Fact]
    public void Success_WhenExitCodeZero_IsTrue()
    {
        var r = new DockerCommandResult { ExitCode = 0 };
        r.Success.Should().BeTrue();
    }

    [Fact]
    public void Success_WhenExitCodeNonZero_IsFalse()
    {
        var r = new DockerCommandResult { ExitCode = 1 };
        r.Success.Should().BeFalse();
    }

    [Fact]
    public void DefaultValues_AreExpected()
    {
        var r = new DockerCommandResult();
        r.Output.Should().BeEmpty();
        r.Error.Should().BeEmpty();
    }
}

public class JsonSerializationConfigurationTests
{
    [Fact]
    public void DefaultValues_AreExpected()
    {
        var cfg = new JsonSerializationConfiguration();

        cfg.Indented.Should().BeFalse();
        cfg.NameCaseInsensitive.Should().BeTrue();
        cfg.Converters.Should().BeNull();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var cfg = new JsonSerializationConfiguration
        {
            Indented = true,
            NameCaseInsensitive = false,
            Converters = new List<object> { new object() }
        };

        cfg.Indented.Should().BeTrue();
        cfg.NameCaseInsensitive.Should().BeFalse();
        cfg.Converters.Should().HaveCount(1);
    }
}

public class GitHubSettingsTests
{
    [Fact]
    public void Organization_IsCorrect()
    {
        GitHubSettings.Organization.Should().Be("cortexiumlabs");
    }

    [Fact]
    public void VectraRepository_IsCorrect()
    {
        GitHubSettings.VectraRepository.Should().Be("vectra");
    }

    [Fact]
    public void VectraCtlRepository_IsCorrect()
    {
        GitHubSettings.VectraCtlRepository.Should().Be("vectractl");
    }

    [Fact]
    public void VectraArchiveFileName_ContainsRepositoryName()
    {
        GitHubSettings.VectraArchiveFileName.Should().Contain("vectra");
    }

    [Fact]
    public void VectraArchiveHashFileName_ContainsSha256()
    {
        GitHubSettings.VectraArchiveHashFileName.Should().Contain("sha256");
    }

    [Fact]
    public void VectraCtlArchiveFileName_ContainsVectraCtl()
    {
        GitHubSettings.VectraCtlArchiveFileName.Should().Contain("vectractl");
    }

    [Fact]
    public void VectraCtlArchiveHashFileName_ContainsSha256()
    {
        GitHubSettings.VectraCtlArchiveHashFileName.Should().Contain("sha256");
    }

    [Fact]
    public void VectraArchiveTemporaryFileName_IsDifferentEachCall()
    {
        var a = GitHubSettings.VectraArchiveTemporaryFileName;
        var b = GitHubSettings.VectraArchiveTemporaryFileName;
        a.Should().NotBe(b);
    }

    [Fact]
    public void VectraArchiveTemporaryHashFileName_ContainsSha256()
    {
        GitHubSettings.VectraArchiveTemporaryHashFileName.Should().Contain("sha256");
    }
}
