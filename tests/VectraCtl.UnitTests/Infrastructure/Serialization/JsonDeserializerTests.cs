using FluentAssertions;
using VectraCtl.Core.Exceptions;
using VectraCtl.Core.Serialization;
using VectraCtl.Infrastructure.Serialization;

namespace VectraCtl.UnitTests.Infrastructure.Serialization;

public class JsonDeserializerTests
{
    private readonly JsonDeserializer _sut = new();

    private record Person(string Name, int Age);

    [Fact]
    public void Deserialize_ValidJson_ReturnsTypedObject()
    {
        var json = """{"name":"Alice","age":30}""";
        var result = _sut.Deserialize<Person>(json);

        result.Name.Should().Be("Alice");
        result.Age.Should().Be(30);
    }

    [Fact]
    public void Deserialize_NullInput_ThrowsVectraCtlException()
    {
        var act = () => _sut.Deserialize<Person>(null);
        act.Should().Throw<VectraCtlException>();
    }

    [Fact]
    public void Deserialize_EmptyInput_ThrowsVectraCtlException()
    {
        var act = () => _sut.Deserialize<Person>(string.Empty);
        act.Should().Throw<VectraCtlException>();
    }

    [Fact]
    public void Deserialize_WhitespaceInput_ThrowsVectraCtlException()
    {
        var act = () => _sut.Deserialize<Person>("   ");
        act.Should().Throw<VectraCtlException>();
    }

    [Fact]
    public void Deserialize_InvalidJson_ThrowsVectraCtlException()
    {
        var act = () => _sut.Deserialize<Person>("not-json");
        act.Should().Throw<VectraCtlException>();
    }

    [Fact]
    public void Deserialize_WithConfiguration_ReturnsTypedObject()
    {
        var json = """{"name":"Bob","age":25}""";
        var config = new JsonSerializationConfiguration { NameCaseInsensitive = true };

        var result = _sut.Deserialize<Person>(json, config);

        result.Name.Should().Be("Bob");
        result.Age.Should().Be(25);
    }

    [Fact]
    public void Deserialize_JsonArray_ReturnsTypedList()
    {
        var json = """[{"name":"Alice","age":30},{"name":"Bob","age":25}]""";
        var result = _sut.Deserialize<List<Person>>(json);

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Alice");
        result[1].Name.Should().Be("Bob");
    }
}
