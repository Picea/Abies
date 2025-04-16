﻿using FsCheck;
using FsCheck.Xunit;

namespace Abies.Tests;

public class UnitTest1
{
    // Custom Arbitrary Generators
    public class DifferentCharsGenerator
    {
        public static Arbitrary<(char, char)> DifferentChars()
        {
            var gen = from a in Gen.OneOf(Gen.Choose(char.MinValue, char.MaxValue))
                      from b in Gen.OneOf(Gen.Choose(char.MinValue, char.MaxValue))
                      where a != b
                      select ((char)a, (char)b);
            return gen.ToArbitrary();
        }
    }

    public class NonNullCharGenerator
    {
        public static Arbitrary<char> NonNullChar()
        {
            var gen = Gen.OneOf(Gen.Choose(char.MinValue, char.MaxValue))
                         .Where(c => c != '\0')
                         .Select(c => (char)c);
            return gen.ToArbitrary();
        }
    }

    public class NonStartingCharGenerator
    {
        public static Arbitrary<char> NonStartingChar()
        {
            return Gen
                .OneOf(Gen.Choose(char.MinValue, char.MaxValue))
                .Where(c => c != 'A')
                .Select(c => (char)(c))
                .ToArbitrary(); // Example condition
        }
    }

    public class LetterGenerator
    {
        public static Arbitrary<char> NonStartingChar()
        {
            return Gen
                .OneOf(Gen.Choose(char.MinValue, char.MaxValue))
                .Where(c => char.IsLetter(((char)c)))
                .Select(c => (char)c)
                .ToArbitrary(); // Example condition
        }
    }

    [Property]
    public void CharParser_ShouldParseExpectedCharacter(char expected, string remaining)
    {
        // Arrange
        var input = expected + remaining;
        var parser = Parse.Char(expected);

        // Act
        var result = parser.Parse(input.AsSpan());

        // Assert
        Assert.True(result.Success);
        Assert.Equal(expected.ToString(), result.Value.ToString());
        Assert.Equal(remaining, result.Remaining);
    }

    [Property(Arbitrary = [typeof(DifferentCharsGenerator)])]
    public void CharParser_ShouldFail_WhenCharacterDoesNotMatch((char expected, char actual) t, string remaining)
    {
        // Arrange
        var input = t.actual + remaining;
        var parser = Parse.Char(t.expected);

        // Act
        var result = parser.Parse(input.AsSpan());

        // Assert
        Assert.False(result.Success);
    }

    [Property(Arbitrary = [typeof(NonNullCharGenerator)], Verbose = true)]
    public void SatisfyParser_ShouldParse_WhenPredicateIsTrue(char c)
    {
        // Arrange
        Func<char, bool> predicate = char.IsLetter;
        var input = c + "rest";
        var parser = Parse.Satisfy(predicate);

        // Act
        var result = parser.Parse(input.AsSpan());

        // Assert
        if(char.IsLetter(c))
        {
            Assert.True(result.Success);
            Assert.Equal(c.ToString(), result.Value.ToString());
            Assert.Equal("rest", result.Remaining.ToString());
        }
        else
        {
            Assert.False(result.Success);
        }
    }



    [Property(Arbitrary = [typeof(DifferentCharsGenerator), typeof(NonNullCharGenerator)], Verbose = true)]
    public void SatisfyParser_ShouldFail_WhenPredicateIsFalse(char c, string remaining)
    {
        // Arrange
        Func<char, bool> predicate = ch => ch == '\0';
        var input = c + remaining;
        var parser = Parse.Satisfy(predicate);

        // Act
        var result = parser.Parse(input.AsSpan());

        // Assert
        Assert.False(result.Success);
    }

    [Property]
    public void StringParser_ShouldParseExactString(string s, string remaining)
    {
        // Arrange
        var input = s + remaining;
        var parser = Parse.String(s);

        // Act
        var result = parser.Parse(input.AsSpan());

        // Assert
        Assert.True(result.Success);
        Assert.Equal(s, result.Value);
        Assert.Equal(remaining, result.Remaining);
    }

    [Property(Arbitrary = [typeof(NonStartingCharGenerator)], Verbose = true)]
    public void StringParser_ShouldFail_WhenInputDoesNotStartWithExpectedString(NonEmptyString s, char firstUnexpected, string remaining)
    {
        // Arrange
        var input = firstUnexpected + remaining;
        var parser = Parse.String(s.Get);

        // Act
        var result = parser.Parse(input.AsSpan());

        // Assert
        Assert.False(result.Success);
    }

    [Property(Verbose = true)]
    public void IntParser_ShouldParseLeadingDigits(NonEmptyString input)
    {
        // Arrange
        // Extract leading digits only
        var leadingDigits = new string(input.Get.TakeWhile(char.IsDigit).ToArray());
        var nonLeadingDigits = input.Get.Substring(leadingDigits.Length);
        var parser = Parse.Int();

        if (string.IsNullOrEmpty(leadingDigits))
        {
            // No leading digits; parser should fail
            var result = parser.Parse(input.Get.AsSpan());
            Assert.False(result.Success);
        }
        else
        {
            // Leading digits exist; parser should succeed
            var expectedValue = int.Parse(leadingDigits);
            var result = parser.Parse(input.Get.AsSpan());
            Assert.True(result.Success);
            Assert.Equal(expectedValue, result.Value);
            Assert.Equal(nonLeadingDigits, result.Remaining.ToString());
        }
    }


    [Property]
    public void OrParser_ShouldParseFirstAlternative_WhenFirstSucceeds(char a, char b, string remaining)
    {
        // Arrange
        var parser = Parse.Char(a) | Parse.Char(b);
        var input = a + remaining;

        // Act
        var result = parser.Parse(input.AsSpan());

        // Assert
        Assert.True(result.Success);
        Assert.Equal(a.ToString(), result.Value.ToString());
        Assert.Equal(remaining, result.Remaining);
    }

    [Property(Arbitrary = [typeof(DifferentCharsGenerator)])]
    public void OrParser_ShouldParseSecondAlternative_WhenFirstFails(char a, char b, string remaining)
    {
        // Arrange
        var parser = Parse.Char(a) | Parse.Char(b);
        var input = b + remaining;

        // Act
        var result = parser.Parse(input.AsSpan());

        // Assert
        Assert.True(result.Success);
        Assert.Equal(b.ToString(), result.Value.ToString());
        Assert.Equal(remaining, result.Remaining);
    }

    [Property]
    public void ManyParser_ShouldParseMultipleOccurrences(NonEmptyString combined, string remaining)
    {
        // Arrange
        var parser = Parse.String(combined.Get);
        var input = combined.Get + combined + remaining;
        var manyParser = parser.Many();

        // Act
        var result = manyParser.Parse(input.AsSpan());

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Value.Count());
        Assert.Equal(remaining, result.Remaining);
    }

    [Property]
    public void Many1Parser_ShouldParseAtLeastOneOccurrence(NonEmptyString combined, string remaining)
    {
        // Arrange

        var parser = Parse.String(combined.Get);
        var input = combined + remaining;
        var many1Parser = parser.Many1();

        // Act
        var result = many1Parser.Parse(input.AsSpan());

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Value);
        Assert.Equal(remaining, result.Remaining);
    }

    //[Property(Verbose = true)]
    //public void Many1Parser_ShouldFail_WhenNoOccurrences(NonEmptyString combined, string remaining)
    //{
    //    // Arrange
    //    var parser = Parse.String(combined.Get);
    //    var many1Parser = parser.Many1();
    //    var input = "unexpected" + remaining;

    //    // Act
    //    var result = many1Parser.Parse(input.AsSpan());

    //    // Assert
    //    Assert.False(result.Success);
    //}


    [Property]
    public void OptionalParser_ShouldParseWhenParserSucceeds(char a, string remaining)
    {
        // Arrange
        var parser = Parse.Char(a).Optional();
        var input = a + remaining;

        // Act
        var result = parser.Parse(input.AsSpan());

        // Assert
        Assert.True(result.Success);
        Assert.Equal(a, result.Value);
        Assert.Equal(remaining, result.Remaining);
    }


    public class ExcludeAGenerator
    {
        public static Arbitrary<char> Exclude()
        {
            var gen = Gen.OneOf(Gen.Choose(char.MinValue, char.MaxValue))
                         .Where(c => c != 'A')
                         .Select(c => (char)c);
            return gen.ToArbitrary();
        }
    }

    [Property(Arbitrary = [typeof(ExcludeAGenerator)])]
    public void OptionalParser_ShouldReturnDefault_WhenParserFails(char a, string remaining)
    {
        // Arrange
       
        var parser = Parse.Char('A').Optional();
        var input = a + remaining;

        // Act
        var result = parser.Parse(input.AsSpan());

        // Assert
        Assert.True(result.Success);
        Assert.Equal(default, result.Value);
        Assert.Equal(a + remaining, result.Remaining);
    }

    [Property]
    public void SlashParser_ShouldParseSeparatedParsers(char a, char b, string remaining)
    {
        // Arrange
        var parser = Parse.Char(a).Slash(Parse.Char(b));
        var input = $"{a}/{b}" + remaining;

        // Act
        var result = parser.Parse(input.AsSpan());

        // Assert
        Assert.True(result.Success);
        Assert.Equal(b.ToString(), result.Value.ToString());
        Assert.Equal(remaining, result.Remaining);
    }

    [Property]
    public void SlashParser_ShouldFail_WhenSeparatorMissing(char a, char b, string remaining)
    {
        // Arrange
        var parser = Parse.Char(a).Slash(Parse.Char(b));
        var input = $"{a}{b}" + remaining;

        // Act
        var result = parser.Parse(input.AsSpan());

        // Assert
        Assert.False(result.Success);
    }

    [Property]
    public void SelectParser_ShouldTransformParsedValue(char a, char b, string remaining)
    {
        // Arrange
        var parser = Parse.Char(a).Select(c => char.ToUpper(c));
        var input = a + remaining;
        var expected = char.ToUpper(a);

        // Act
        var result = parser.Parse(input.AsSpan());

        // Assert
        Assert.True(result.Success);
        Assert.Equal(expected.ToString(), result.Value.ToString());
        Assert.Equal(remaining, result.Remaining);
    }

    [Property(Verbose = true)]
    public void SelectManyParser_ShouldCombineParsedValues(char a, char b, string remaining)
    {
        // Arrange
        var parser = 
            from first in Parse.Char(a)
            from second in Parse.Char(b)
            select $"{first}{second}";

        var input = $"{a}{b}" + remaining;
        var expected = $"{a}{b}";

        // Act
        var result = parser.Parse(input.AsSpan());

        // Assert
        Assert.True(result.Success);
        Assert.Equal(expected, result.Value);
        Assert.Equal(remaining, result.Remaining);
    }

    [Property]
    public void OrParser_ShouldReturnFirstSuccess_WhenBothAlternativesSucceed(char a, string remaining)
    {
        // Arrange
        var parser = Parse.Char(a) | Parse.Char(a);
        var input = a + remaining;

        // Act
        var result = parser.Parse(input.AsSpan());

        // Assert
        Assert.True(result.Success);
        Assert.Equal(a.ToString(), result.Value.ToString());
        Assert.Equal(remaining, result.Remaining);
    }

    [Property]
    public void OrParser_ShouldReturnSecondSuccess_WhenFirstFails(char a, char b, string remaining)
    {
        // Arrange
        var parser = Parse.Char(a) | Parse.Char(b);
        var input = b + remaining;
        // Act
        var result = parser.Parse(input.AsSpan());
        // Assert
        Assert.True(result.Success);
        Assert.Equal(b.ToString(), result.Value.ToString());
        Assert.Equal(remaining, result.Remaining);
    }

}



