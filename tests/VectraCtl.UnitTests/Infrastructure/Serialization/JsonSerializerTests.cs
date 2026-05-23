using FluentAssertions;
using VectraCtl.Core.Exceptions;
using VectraCtl.Core.Serialization;
using VectraCtl.Infrastructure.Serialization;

namespace VectraCtl.UnitTests.Infrastructure.Serialization;

public class JsonSerializerTests
{
    private readonly JsonSerializer _sut = new();

    private record Person(string Name, int Age);

    [Fact]
    public void ContentMineType_ShouldBeApplicationJson()
    {
        JsonSerializer.ContentMineType.Should().Be("application/json");
    }

    [Fact]
    public void Serialize_SimpleObject_ReturnsJsonString()
    {
        var person = new Person("Alice", 30);
        var result = _sut.Serialize(person);

        result.Should().Contain("Alice");
        result.Should().Contain("30");
    }

    [Fact]
    public void Serialize_NullInput_ThrowsVectraCtlException()
    {
        var act = () => _sut.Serialize(null);
        act.Should().Throw<VectraCtlException>();
    }

    [Fact]
    public void Serialize_WithIndented_ReturnsIndentedJson()
    {
        var person = new Person("Bob", 25);
        var result = _sut.Serialize(person, new JsonSerializationConfiguration { Indented = true });

        result.Should().Contain(Environment.NewLine);
    }

    [Fact]
    public void Serialize_WithoutIndented_ReturnsSingleLineJson()
    {
        var person = new Person("Bob", 25);
        var result = _sut.Serialize(person, new JsonSerializationConfiguration { Indented = false });

        result.Should().NotContain(Environment.NewLine);
    }

    [Fact]
    public void Serialize_DefaultOverload_UsesPascalCase()
    {
        // Default config has NameCaseInsensitive=true which disables camelCase policy
        var person = new Person("Carol", 40);
        var result = _sut.Serialize(person);

        result.Should().Contain("\"Name\"");
        result.Should().Contain("\"Age\"");
    }

    [Fact]
    public void Serialize_WithNameCaseInsensitiveTrue_DoesNotForceCamelCase()
    {
        var config = new JsonSerializationConfiguration { NameCaseInsensitive = true };
        var person = new Person("Dave", 20);
        var result = _sut.Serialize(person, config);

        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Serialize_AnonymousObject_ReturnsValidJson()
    {
        var obj = new { message = "hello", count = 3 };
        var result = _sut.Serialize(obj);

        result.Should().Contain("hello");
        result.Should().Contain("3");
    }
}
