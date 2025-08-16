using SagaPatternDemo.Host.Infrastructure.Messaging;
using System.Text.Json;

namespace SagaPatternDemo.Tests;

public class JsonSerializerProviderTests
{
    [Fact]
    public void Serialize_ProducesCorrectJson()
    {
        // Arrange
        var provider = new JsonSerializerProvider();
        var testObject = new { Name = "Test", Value = 42 };

        // Act
        var json = provider.Serialize(testObject);

        // Assert
        Assert.Contains("\"name\":", json); // camelCase
        Assert.Contains("\"value\":", json);
        Assert.Contains("42", json);
    }

    [Fact]
    public void Deserialize_HandlesNullOrEmpty()
    {
        // Arrange
        var provider = new JsonSerializerProvider();

        // Act & Assert
        Assert.Null(provider.Deserialize<string>(null!));
        Assert.Null(provider.Deserialize<string>(""));
        Assert.Null(provider.Deserialize<string>("   "));
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_WorksCorrectly()
    {
        // Arrange
        var provider = new JsonSerializerProvider();
        var original = new TestData
        {
            StringProp = "Hello World",
            IntProp = 123,
            DateProp = DateTime.UtcNow,
            EnumProp = TestEnum.ValueTwo
        };

        // Act
        var json = provider.Serialize(original);
        var deserialized = provider.Deserialize<TestData>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.StringProp, deserialized.StringProp);
        Assert.Equal(original.IntProp, deserialized.IntProp);
        Assert.Equal(original.DateProp.ToString("O"), deserialized.DateProp.ToString("O"));
        Assert.Equal(original.EnumProp, deserialized.EnumProp);
    }

    [Fact]
    public void Options_ConfiguredCorrectly()
    {
        // Arrange
        var provider = new JsonSerializerProvider();

        // Act
        var options = provider.Options;

        // Assert
        Assert.Equal(JsonNamingPolicy.CamelCase, options.PropertyNamingPolicy);
        Assert.False(options.WriteIndented);
    }

    private class TestData
    {
        public string StringProp { get; set; } = string.Empty;
        public int IntProp { get; set; }
        public DateTime DateProp { get; set; }
        public TestEnum EnumProp { get; set; }
    }

    private enum TestEnum
    {
        ValueOne,
        ValueTwo,
        ValueThree
    }
}