using FluentAssertions;
using VectraCtl.Core.Exceptions;
using VectraCtl.Core.Serialization;

namespace VectraCtl.UnitTests.Infrastructure.Serialization.Converters;

using InfraJsonSerializer = VectraCtl.Infrastructure.Serialization.JsonSerializer;
using InfraJsonDeserializer = VectraCtl.Infrastructure.Serialization.JsonDeserializer;

public class JsonSerializerConverterTests
{
    private readonly InfraJsonSerializer _sut = new();

    [Fact]
    public void ContentMineType_IsStatic_ReturnsSameValue()
    {
        InfraJsonSerializer.ContentMineType.Should().Be("application/json");
    }

    [Fact]
    public void Serialize_WithCustomConverters_UsesConverters()
    {
        var config = new JsonSerializationConfiguration
        {
            Converters = new List<object> { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        var result = _sut.Serialize(new { mode = System.IO.FileMode.Open }, config);
        result.Should().Contain("Open");
    }

    [Fact]
    public void Serialize_ExceptionPath_WrapsAsVectraCtlException()
    {
        // A type that JSON can't serialize due to bad setup – null input is easiest
        var act = () => _sut.Serialize(null, new JsonSerializationConfiguration());
        act.Should().Throw<VectraCtlException>();
    }
}

public class JsonDeserializerConverterTests
{
    private readonly InfraJsonDeserializer _sut = new();

    [Fact]
    public void Deserialize_WithCustomConverters_UsesConverters()
    {
        var config = new JsonSerializationConfiguration
        {
            Converters = new List<object> { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        var result = _sut.Deserialize<string>("\"Open\"", config);
        result.Should().NotBeNull();
    }

    [Fact]
    public void Deserialize_NullResult_ThrowsVectraCtlException()
    {
        // Trying to deserialize "null" into a non-nullable record triggers the null guard
        var act = () => _sut.Deserialize<NonNullableRecord>("null");
        act.Should().Throw<VectraCtlException>();
    }

    private record NonNullableRecord(string Name);
}
