using FluentAssertions;
using VectraCtl.Core.Exceptions;
using VectraCtl.Core.Serialization;
using VectraCtl.Infrastructure.Serialization;

namespace VectraCtl.Infrastructure.UnitTests.Serialization;

public class JsonSerializerTests
{
    private readonly JsonSerializer _sut = new();

    // --- ContentMineType ---

    [Fact]
    public void ContentMineType_ReturnsApplicationJson()
    {
        JsonSerializer.ContentMineType.Should().Be("application/json");
    }

    // --- Serialize(object?) ---

    [Fact]
    public void Serialize_WithNull_ThrowsVectraCtlException()
    {
        var act = () => _sut.Serialize(null);
        act.Should().Throw<VectraCtlException>().WithMessage("*null*");
    }

    [Fact]
    public void Serialize_SimpleObject_ReturnsValidJson()
    {
        var result = _sut.Serialize(new { name = "test", value = 42 });
        result.Should().Contain("\"name\"").And.Contain("\"test\"");
    }

    [Fact]
    public void Serialize_DefaultConfig_UsesPascalCase()
    {
        // Default config has NameCaseInsensitive=true which disables camelCase policy
        var result = _sut.Serialize(new SampleModel { FirstName = "Alice" });
        result.Should().Contain("\"FirstName\"");
    }

    [Fact]
    public void Serialize_DefaultConfig_IsNotIndented()
    {
        var result = _sut.Serialize(new SampleModel { FirstName = "Alice" });
        result.Should().NotContain("\n");
    }

    // --- Serialize(object?, JsonSerializationConfiguration) ---

    [Fact]
    public void Serialize_WithIndented_ProducesFormattedOutput()
    {
        var result = _sut.Serialize(new SampleModel { FirstName = "Alice" },
            new JsonSerializationConfiguration { Indented = true });
        result.Should().Contain("\n");
    }

    [Fact]
    public void Serialize_WithNameCaseInsensitive_PreservesOriginalCasing()
    {
        var result = _sut.Serialize(new SampleModel { FirstName = "Alice" },
            new JsonSerializationConfiguration { NameCaseInsensitive = true });
        result.Should().Contain("\"FirstName\"");
    }

    [Fact]
    public void Serialize_WithNullInput_ThrowsVectraCtlException()
    {
        var act = () => _sut.Serialize(null, new JsonSerializationConfiguration());
        act.Should().Throw<VectraCtlException>();
    }

    [Fact]
    public void Serialize_RoundTrip_ProducesDeserializableOutput()
    {
        var original = new SampleModel { FirstName = "Bob" };
        var json = _sut.Serialize(original);
        var deserializer = new JsonDeserializer();
        var result = deserializer.Deserialize<SampleModel>(json);
        result.FirstName.Should().Be("Bob");
    }

    private class SampleModel
    {
        public string FirstName { get; set; } = string.Empty;
    }
}
