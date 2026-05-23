using FluentAssertions;
using NSubstitute;
using VectraCtl.Core.Models.Configuration;
using VectraCtl.Core.Serialization;
using VectraCtl.Core.Services.Location;
using VectraCtl.Infrastructure.Serialization;
using VectraCtl.Infrastructure.Services.Configuration;

namespace VectraCtl.UnitTests.Infrastructure.Services.Configuration;

public class AppSettingsServiceTests : IDisposable
{
    private readonly ILocation _location;
    private readonly JsonSerializer _serializer;
    private readonly JsonDeserializer _deserializer;
    private readonly AppSettingsService _sut;
    private readonly string _tempDir;

    public AppSettingsServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        _location = Substitute.For<ILocation>();
        _location.RootLocation.Returns(_tempDir);
        _location.DefaultVectraDirectoryName.Returns(Path.Combine(_tempDir, ".vectra"));

        _serializer = new JsonSerializer();
        _deserializer = new JsonDeserializer();
        _sut = new AppSettingsService(_location, _serializer, _deserializer);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Constructor_NullLocation_Throws()
    {
        var act = () => new AppSettingsService(null!, _serializer, _deserializer);
        act.Should().Throw<ArgumentNullException>().WithParameterName("location");
    }

    [Fact]
    public void Constructor_NullSerializer_Throws()
    {
        var act = () => new AppSettingsService(_location, null!, _deserializer);
        act.Should().Throw<ArgumentNullException>().WithParameterName("serializer");
    }

    [Fact]
    public void Constructor_NullDeserializer_Throws()
    {
        var act = () => new AppSettingsService(_location, _serializer, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("deserializer");
    }

    [Fact]
    public void GetSettingsPath_ReturnsExpectedPath()
    {
        var path = _sut.GetSettingsPath();
        path.Should().Be(Path.Combine(_tempDir, "appsettings.json"));
    }

    [Fact]
    public async Task LoadAsync_FileDoesNotExist_ReturnsDefaults()
    {
        var settings = await _sut.LoadAsync();

        settings.Should().NotBeNull();
        settings.DeploymentMode.Should().Be(DeploymentMode.Binary);
        settings.Docker.Should().NotBeNull();
        settings.Binary.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadAsync_FileIsEmpty_ReturnsDefaults()
    {
        var path = _sut.GetSettingsPath();
        await File.WriteAllTextAsync(path, "   ");

        var settings = await _sut.LoadAsync();

        settings.Should().NotBeNull();
        settings.DeploymentMode.Should().Be(DeploymentMode.Binary);
    }

    [Fact]
    public async Task LoadAsync_ValidFile_ReturnsDeserializedSettings()
    {
        var original = new AppSettings { DeploymentMode = DeploymentMode.Docker };
        var json = _serializer.Serialize(original, new JsonSerializationConfiguration { Indented = true });
        await File.WriteAllTextAsync(_sut.GetSettingsPath(), json);

        var loaded = await _sut.LoadAsync();

        loaded.DeploymentMode.Should().Be(DeploymentMode.Docker);
    }

    [Fact]
    public async Task LoadAsync_InvalidJson_ReturnsDefaults()
    {
        await File.WriteAllTextAsync(_sut.GetSettingsPath(), "not-valid-json!!!");

        var settings = await _sut.LoadAsync();

        settings.Should().NotBeNull();
        settings.DeploymentMode.Should().Be(DeploymentMode.Binary);
    }

    [Fact]
    public async Task LoadAsync_JsonWithNullDockerAndBinary_NormalizesToNewInstances()
    {
        // Write a JSON where Docker/Binary are null
        var json = """{"deploymentMode":1,"docker":null,"binary":null}""";
        await File.WriteAllTextAsync(_sut.GetSettingsPath(), json);

        var settings = await _sut.LoadAsync();

        settings.Docker.Should().NotBeNull();
        settings.Binary.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveAsync_WritesFileToExpectedPath()
    {
        var settings = new AppSettings { DeploymentMode = DeploymentMode.Docker };

        await _sut.SaveAsync(settings);

        File.Exists(_sut.GetSettingsPath()).Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_NullSettings_Throws()
    {
        var act = () => _sut.SaveAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrip_PreservesData()
    {
        var original = new AppSettings
        {
            DeploymentMode = DeploymentMode.Docker,
            Docker = new DockerSettings { ContainerName = "my-container", Port = 9090 }
        };

        await _sut.SaveAsync(original);
        var loaded = await _sut.LoadAsync();

        loaded.DeploymentMode.Should().Be(DeploymentMode.Docker);
        loaded.Docker.ContainerName.Should().Be("my-container");
        loaded.Docker.Port.Should().Be(9090);
    }
}
