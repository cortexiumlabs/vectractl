using FluentAssertions;
using VectraCtl.Core.Exceptions;
using VectraCtl.Core.Serialization;
using VectraCtl.Infrastructure.Serialization;

namespace VectraCtl.Infrastructure.UnitTests.Serialization;

public class JsonDeserializerTests
{
    private readonly JsonDeserializer _sut = new();

    // --- Deserialize<T>(string?) ---

    [Fact]
    public void Deserialize_WithNull_ThrowsVectraCtlException()
    {
        var act = () => _sut.Deserialize<SampleModel>(null);
        act.Should().Throw<VectraCtlException>().WithMessage("*empty or null*");
    }

    [Fact]
    public void Deserialize_WithEmptyString_ThrowsVectraCtlException()
    {
        var act = () => _sut.Deserialize<SampleModel>(string.Empty);
        act.Should().Throw<VectraCtlException>();
    }

    [Fact]
    public void Deserialize_WithWhiteSpace_ThrowsVectraCtlException()
    {
        var act = () => _sut.Deserialize<SampleModel>("   ");
        act.Should().Throw<VectraCtlException>();
    }

    [Fact]
    public void Deserialize_ValidJson_ReturnsMappedObject()
    {
        var json = "{\"firstName\":\"Alice\",\"age\":30}";
        var result = _sut.Deserialize<SampleModel>(json);
        result.FirstName.Should().Be("Alice");
        result.Age.Should().Be(30);
    }

    [Fact]
    public void Deserialize_CaseInsensitive_MapsRegardlessOfCase()
    {
        var json = "{\"FIRSTNAME\":\"Bob\",\"AGE\":5}";
        var result = _sut.Deserialize<SampleModel>(json);
        result.FirstName.Should().Be("Bob");
    }

    [Fact]
    public void Deserialize_InvalidJson_ThrowsVectraCtlException()
    {
        var act = () => _sut.Deserialize<SampleModel>("not valid json");
        act.Should().Throw<VectraCtlException>();
    }

    // --- Deserialize<T>(string?, JsonSerializationConfiguration) ---

    [Fact]
    public void Deserialize_WithConfiguration_HonorsNameCaseInsensitive()
    {
        var json = "{\"FirstName\":\"Carol\",\"Age\":25}";
        var result = _sut.Deserialize<SampleModel>(json,
            new JsonSerializationConfiguration { NameCaseInsensitive = true });
        result.FirstName.Should().Be("Carol");
    }

    [Fact]
    public void Deserialize_WithNullInput_AndConfiguration_ThrowsVectraCtlException()
    {
        var act = () => _sut.Deserialize<SampleModel>(null, new JsonSerializationConfiguration());
        act.Should().Throw<VectraCtlException>();
    }

    [Fact]
    public void Deserialize_DeserializingToWrongType_ThrowsVectraCtlException()
    {
        var json = "\"just a string\"";
        var act = () => _sut.Deserialize<SampleModel>(json);
        act.Should().Throw<VectraCtlException>();
    }

    private class SampleModel
    {
        public string FirstName { get; set; } = string.Empty;
        public int Age { get; set; }
    }
}
