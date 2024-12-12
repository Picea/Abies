using System;
using System.Linq;
using System.Collections.Generic;
using Abies;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace Abies.Tests
{
    // ================= Arbitrary Classes =================

    public class ValidSchemeArb
    {
        public static Arbitrary<Scheme> Generator()
        {

            // Generate a scheme followed by ':'. Only http or https are valid
            return Gen.Elements("http", "https").Select(s => s + ":").Select(s => new Scheme(s)).ToArbitrary();


        }
    }

    public class InvalidSchemeArb
    {
        public static Arbitrary<Scheme> Generator()
        {
            var invalidGen = Gen.OneOf(
                // No colon
                Arb.Default.String().Generator.Where(str => !string.IsNullOrEmpty(str) && !str.Contains(':')),
                // Colon at the start
                from rest in Arb.Default.String().Generator
                select ":" + rest,
                // Contains invalid chars before colon
                from str in Arb.Default.String().Generator
                where str.Contains(':')
                let parts = str.Split(':')
                where parts[0].Any(c => !(char.IsLetterOrDigit(c) || c == '+'))
                select str
            );

            return invalidGen.Select(s => new Scheme(s)).ToArbitrary();

        }
    }

    public class ValidHostArb
    {
        public static Arbitrary<Host> Generator()
        {

            var hostChar = Arb.Default.Char().Generator
                .Where(c => c != ':' && c != '/' && c != '?' && c != '#');

            return (from chars in Gen.NonEmptyListOf(hostChar)
                    select "//" + new string(chars.ToArray())).Select(h => new Host(h)).ToArbitrary();

        }
    }

    public class InvalidHostArb
    {
        public static Arbitrary<Host> Generator()
        {
            var invalidGen = Gen.OneOf<string>(
                // Missing "//"
                Arb.Default.NonEmptyString().Generator.Where(s => !s.Get.StartsWith("//")).Select(s => s.Get),
                // Contains invalid characters after "//"
                from s in Arb.Default.NonEmptyString().Generator
                where s.Get.StartsWith("//") && s.Get.Skip(2).Any(c => !char.IsLetterOrDigit(c) && c != '.' && c != '-')
                select s.Get,
                // Empty string
                Gen.Constant("")
            );

            return invalidGen.Select(h => new Host(h)).ToArbitrary();
        }
    }


    public class ValidQueryArb
    {
        private static Gen<char> QueryChar = Gen.Elements(
            Enumerable.Range('0', 10).Select(i => (char)i)
            .Concat(Enumerable.Range('A', 26).Select(i => (char)i))
            .Concat(Enumerable.Range('a', 26).Select(i => (char)i))
            .Concat(new[] { '_' })
            .ToArray()
        );

        private static Gen<string> QueryParameterGen =>
            from keyChars in Gen.NonEmptyListOf(QueryChar)
            from valueChars in Gen.NonEmptyListOf(QueryChar)
            select new string(keyChars.ToArray()) + "=" + new string(valueChars.ToArray());

        public static Arbitrary<Query> Generator()
        {
            return Gen.OneOf(
                Gen.Constant(""),
                from parameters in Gen.NonEmptyListOf(QueryParameterGen)
                select "?" + string.Join("&", parameters)
            ).Select(q => new Query(q)).ToArbitrary();

        }
    }

    public class InvalidQueryArb
    {
        private static Gen<char> QueryChar = Gen.Elements(
            Enumerable.Range('0', 10).Select(i => (char)i)
            .Concat(Enumerable.Range('A', 26).Select(i => (char)i))
            .Concat(Enumerable.Range('a', 26).Select(i => (char)i))
            .Concat(new[] { '_' })
            .ToArray()
        );

        private static Gen<string> QueryParameterGen =>
            from keyChars in Gen.NonEmptyListOf(QueryChar)
            from valueChars in Gen.NonEmptyListOf(QueryChar)
            select new string(keyChars.ToArray()) + "=" + new string(valueChars.ToArray());

        public static Arbitrary<Query> Generator()
        {
            var invalidQueryGen = Gen.OneOf(
                // Starts with '?' but no parameters
                Gen.Constant("?"),
                // Contains invalid chars in keys/values
                from parameters in Gen.NonEmptyListOf(QueryParameterGen)
                from invalidChar in Arb.Default.Char().Generator.Where(c => c == '/' || c == '?')
                select "?" + string.Join("&", parameters) + invalidChar,
                // Missing '=' after '?'
                from keyChars in Gen.NonEmptyListOf(QueryChar)
                select "?" + new string(keyChars.ToArray()) // no '=' means invalid parameter
            );

            return invalidQueryGen.Select(q => new Query(q)).ToArbitrary();

        }
    }

    public class FragmentArb
    {
        public static Arbitrary<Fragment> Generator()
        {
            var fragmentChar = Arb.Default.Char().Generator;
            return Gen.OneOf(
                Gen.Constant(""), // optional empty fragment
                from chars in Gen.ListOf(fragmentChar)
                select "#" + new string(chars.ToArray())
            ).Select(f => new Fragment(f)).ToArbitrary();

        }
    }

    public class PathArb
    {
        public static Arbitrary<Path> Generator()
        {
            var pathChar = Arb.Default.Char().Generator.Where(c => c != '?' && c != '#');
            return Gen.OneOf(
                Gen.Constant(""), // empty path defaults to "/"
                from chars in Gen.ListOf(pathChar)
                select new string(chars.ToArray())
            ).Select(p => new Path(p)).ToArbitrary();

        }
    }

    public class ValidIntArb
    {
        public static Arbitrary<string> Generator()
        {
            var digitGen = Gen.Elements('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
            return (from digits in Gen.NonEmptyListOf(digitGen)
                    where int.TryParse(new string(digits.ToArray()), out _)
                    select new string(digits.ToArray())).ToArbitrary();

        }
    }

    public class ValidUrlArb
    {
        public static Arbitrary<string> Generator()
        {
            return Arb.From(
                from scheme in ValidSchemeArb.Generator().Generator
                from host in ValidHostArb.Generator().Generator
                from path in PathArb.Generator().Generator
                from query in ValidQueryArb.Generator().Generator
                from fragment in FragmentArb.Generator().Generator
                let url = $"{scheme.Value}{host.Value}{path.Value}{query.Value}{fragment.Value}"
                where Uri.TryCreate(url, UriKind.Absolute, out _)
                select url
            );
        }
    }

    public class InvalidIntArb
    {
        /// <summary>
        /// Generates invalid integers, which can be empty, contain non-digit characters, or contain '/'
        /// </summary>
        /// <returns></returns>
        public static Arbitrary<string> Generator()
        {
            var invalidGen = Gen.OneOf<string>(
                // Contains '/'
                from s in Arb.Default.NonEmptyString().Generator
                where s.Get.Contains('/')
                select s.Get,
                // Contains non-digit chars (other than '/')
                from s in Arb.Default.String().Generator
                where s.Any(c => !char.IsDigit(c) && c != '/')
                select s,
                // Empty
                Gen.Constant("")
            );

            return invalidGen.ToArbitrary();
        }
    }


    public class UrlParsingTests
    {
        private static T ParseOrFail<T>(Parser<T> parser, string input)
        {
            var result = parser.Parse(input.AsSpan());
            if (!result.Success)
                throw new Exception($"Parsing failed for input '{input}'");
            return result.Value!;
        }

        private static bool ParseFails<T>(Parser<T> parser, string input)
        {
            var result = parser.Parse(input.AsSpan());
            return !result.Success;
        }

        // ----------------- URL Creation Tests -----------------
        // We can combine scheme, host, path, query, and fragment to form a full URL and test Url.Create.
        [Property(
            Arbitrary =
            [
                typeof(ValidUrlArb)
            ])]
        public void UrlParser_ShouldParseValidUrls(string urlString)
        {
            // Extract scheme and host from generated inputs
            var scheme = urlString.Split(':').First();
            var host = urlString.Contains("//") ? urlString.Split("//").Last().Split('/').First() : "".ToLowerInvariant();
            var query = urlString.Contains('?') ? urlString.Split('?').Last() : "";
            var fragment = urlString.Contains('#') ? urlString.Split('#').Last() : "";
            var path = urlString.Contains(host) ? urlString.Substring(scheme.Length + host.Length) : "";

            // Random port or no port
            var portGen = Gen.OneOf(
                Gen.Constant<int?>(null),
                Gen.Choose(1, 65535).Select(p => (int?)p)
            );
            var port = portGen.Sample(1, 1).First();

            var portPart = port.HasValue && port > 0 ? $":{port}" : "";

            // Construct a full URL
            var urlInput = new Url.Decoded.String(urlString);

            // Attempt to create URL
            var url = Url.Create(urlInput);

            Assert.NotNull(url);
            Assert.Equal(scheme, url.Scheme switch
            {
                Url.Protocol.Http => "http",
                Url.Protocol.Https => "https",
                _ => throw new Exception("Unknown scheme")
            });
            Assert.Equal(host, url.Host);
            if (port is not null and > 0) Assert.Equal(port, url.Port);
            Assert.Equal(Uri.UnescapeDataString(string.IsNullOrEmpty(path) ? "/" : path), url.Path.Value);
            Assert.Equal(query, url.Query.Value);
            Assert.Equal(fragment, url.Fragment);
        }

        [Property]
        public void UrlParser_ShouldFailOnInvalidUrls(NonEmptyString invalidUrl)
        {
            var urlInput = new Url.Decoded.String(invalidUrl.Get);
            Assert.Throws<FormatException>(() => Url.Create(urlInput));
        }

        // ----------------- Individual Parser Component Tests -----------------

        [Property(Arbitrary = new[] { typeof(ValidSchemeArb) })]
        public void Scheme_Parses_Valid_Scheme_Correctly(string scheme)
        {
            var parsed = ParseOrFail(Url.Parse.Scheme, scheme);
            Assert.False(string.IsNullOrEmpty(parsed));
            Assert.Equal(scheme.TrimEnd(':'), parsed);
        }

        [Property(Arbitrary = new[] { typeof(InvalidSchemeArb) })]
        public void Scheme_Fails_On_Invalid_Scheme(string scheme)
        {
            Assert.True(ParseFails(Url.Parse.Scheme, scheme));
        }

        [Property(Arbitrary = new[] { typeof(ValidHostArb) })]
        public void Host_Parses_Valid_Host(string host)
        {
            var result = Url.Parse.Host.Parse(host.AsSpan());
            Assert.True(result.Success);
            Assert.Equal(host.Substring(2), result.Value);
        }

        [Property(Arbitrary = new[] { typeof(InvalidHostArb) })]
        public void Host_Fails_On_Invalid_Host(string host)
        {
            Assert.True(ParseFails(Url.Parse.Host, host));
        }

        [Property(Arbitrary = new[] { typeof(ValidQueryArb) })]
        public void Query_Parses_Valid_Query(string query)
        {
            var parsed = ParseOrFail(Url.Parse.Query, query);
            if (string.IsNullOrEmpty(query))
            {
                Assert.Equal(string.Empty, parsed);
            }
            else
            {
                Assert.True(!string.IsNullOrEmpty(parsed));
            }
        }

        [Property(Arbitrary = new[] { typeof(InvalidQueryArb) })]
        public void Query_Fails_On_Invalid_Query(string query)
        {
            var result = Url.Parse.Query.Parse(query.AsSpan());
            if (query.StartsWith("?"))
            {
                if (query.Length == 1 || !query.Contains('='))
                {
                    // The parser is optional, might return empty if parameters aren't parsed.
                    if (result.Success)
                    {
                        Assert.Equal(string.Empty, result.Value);
                    }
                }
            }
        }

        [Property(Arbitrary = new[] { typeof(FragmentArb) })]
        public void Fragment_Parses_Valid_Fragment(string fragment)
        {
            var result = Url.Parse.Fragment.Parse(fragment.AsSpan());
            Assert.True(result.Success);
            if (string.IsNullOrEmpty(fragment))
            {
                Assert.Equal(string.Empty, result.Value);
            }
            else
            {
                Assert.Equal(fragment.Substring(1), result.Value);
            }
        }

        [Property(Arbitrary = new[] { typeof(PathArb) })]
        public void Path_Parses_Valid_Path(string path)
        {
            var result = Url.Parse.Path.Parse(path.AsSpan());
            Assert.True(result.Success);
            if (string.IsNullOrEmpty(path))
            {
                Assert.Equal("/", result.Value);
            }
            else
            {
                Assert.Equal(path, result.Value);
            }
        }

        [Property(Arbitrary = new[] { typeof(ValidIntArb) })]
        public void Int_Parses_Valid_Int(string number)
        {
            var parsed = ParseOrFail(Url.Parse.Int, number);
            Assert.Equal(int.Parse(number), parsed);
        }

        [Property(Arbitrary = new[] { typeof(InvalidIntArb) })]
        public void Int_Fails_On_Invalid_Int(NonEmptyString input)
        {
            if (string.IsNullOrEmpty(input.Get) || input.Get.Contains('/') || input.Get.Any(c => !char.IsDigit(c)))
            {
                Assert.True(ParseFails(Url.Parse.Int, input.Get));
            }
        }

        [Property(Arbitrary = new[] { typeof(ValidIntArb) })]
        public void Double_Parses_Valid_Number(string number)
        {
            var parsed = ParseOrFail(Url.Parse.Double, number);
            Assert.Equal(double.Parse(number), parsed);
        }

        [Property(Arbitrary = new[] { typeof(ValidIntArb) })]
        public void Strict_Int_Parses_Valid(string number)
        {
            var result = Url.Parse.Strict.Int.Parse(number.AsSpan());
            Assert.True(result.Success);
            Assert.Equal(int.Parse(number), result.Value);
        }

        [Property(Arbitrary = new[] { typeof(InvalidIntArb) })]
        public void Strict_Int_Fails_On_Invalid(string input)
        {
            var result = Url.Parse.Strict.Int.Parse(input.AsSpan());
            if (string.IsNullOrEmpty(input) || input.Contains('/') || input.Any(c => !char.IsDigit(c)))
            {
                Assert.False(result.Success);
            }
        }

        [Property(Arbitrary = new[] { typeof(ValidIntArb) })]
        public void Strict_Double_Parses_Valid(string number)
        {
            var result = Url.Parse.Strict.Double.Parse(number.AsSpan());
            Assert.True(result.Success);
            Assert.Equal(double.Parse(number), result.Value);
        }

        [Property(Arbitrary = new[] { typeof(InvalidIntArb) })]
        public void Strict_Double_Fails_On_Invalid(string input)
        {
            var result = Url.Parse.Strict.Double.Parse(input.AsSpan());
            if (string.IsNullOrEmpty(input) || input.Contains('/') || input.Any(c => !char.IsDigit(c)))
            {
                Assert.False(result.Success);
            }
        }

        [Property(Arbitrary = new[] { typeof(PathArb) })]
        public void String_Parser_Accepts_No_Slash(string input)
        {
            // The String parser parses chars not containing '/'
            if (!input.Contains('/'))
            {
                var result = Url.Parse.String.Parse(input.AsSpan());
                Assert.True(result.Success);
                Assert.Equal(input, result.Value);
            }
            else
            {
                Assert.True(ParseFails(Url.Parse.String, input));
            }
        }
    }
}
