using System.Runtime.Versioning;
using static Abies.ParserExtensions;

[SupportedOSPlatform("browser")]
public class StringValueParserTests
{
    [Fact]
    public void StringValueParser_ShouldParseExactMatch()
    {
        // Arrange
        var parser = new StringValueParser("hello");
        var input = "hello world".AsSpan();

        // Act
        var result = parser.Parse(input);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("hello", result.Value);
        Assert.Equal(" world", result.Remaining.ToString());
    }

    [Fact]
    public void StringValueParser_ShouldFailOnPartialMatch()
    {
        // Arrange
        var parser = new StringValueParser("hello");
        var input = "hell".AsSpan();

        // Act
        var result = parser.Parse(input);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public void StringValueParser_ShouldFailOnNoMatch()
    {
        // Arrange
        var parser = new StringValueParser("hello");
        var input = "world".AsSpan();

        // Act
        var result = parser.Parse(input);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public void StringValueParser_ShouldFailOnEmptyInput()
    {
        // Arrange
        var parser = new StringValueParser("hello");
        var input = "".AsSpan();

        // Act
        var result = parser.Parse(input);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public void StringValueParser_ShouldParseEmptyExpectedString()
    {
        // Arrange
        var parser = new StringValueParser("");
        var input = "hello".AsSpan();

        // Act
        var result = parser.Parse(input);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("", result.Value);
        Assert.Equal("hello", result.Remaining.ToString());
    }
}
