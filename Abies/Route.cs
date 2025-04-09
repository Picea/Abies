namespace Abies;

public static class Route
{
    public static class Parse
    {
        public static class Segment
        {
            public static Parser<string> String(string segment) =>
                from slash in Abies.Parse.Char('/')
                from chars in Abies.Parse.Many(Abies.Parse.Satisfy(c => c != '/'))
                let result = new string(chars.ToArray())
                where string.Equals(result, segment, StringComparison.OrdinalIgnoreCase)
                select segment;

            /// <summary>
            /// A parser that parses a sequence of one or more characters that are not a '/' character.
            /// For example, the parser succeeds when input is "foo" or "bar", but fails when input is "foo/bar".
            /// It also rejects control characters as specified in the RFC 3986.
            /// </summary>
            public static Parser<char[]> Any =>
                from chars in Abies.Parse.Many(Abies.Parse.Satisfy(c => c != '/' && !char.IsControl(c)))
                select chars.ToArray();

            /// <summary>
            /// Parses an empty segment, which is represented by a single forward slash and returns an empty string.
            /// Applications can use this parser to represent the root path of a URL.
            /// </summary>
            public static Parser<string> Empty =>
                from empty in Abies.Parse.Char('/')
                select string.Empty;

            // parse ONLY the root segment
            public static Parser<string> Root =>
                from root in Abies.Parse.Char('/')
                select "/";
        }

        /// <summary>
        /// A parser that parses a sequence of one or more digits (0-9) that are not preceded by a '/' character.
        /// If the input does not contain at least one digit or contains a '/' character, the parser fails.
        /// </summary>
        /// <remarks>
        /// This parser uses the `Parse.Many1` combinator to ensure that at least one digit is present in the input.
        /// It uses the `Parse.Satisfy` combinator to check that each character is a digit and not a '/' character.
        /// When the parser succeeds, it returns an integer parsed from the sequence of digits.
        /// </remarks>
        public static Parser<int> Int =>
            from digits in Abies.Parse.Many1(Abies.Parse.Satisfy(c => c != '/' && char.IsDigit(c)))
            select int.Parse(new string([.. digits]));

        public static Parser<double> Double =>
            from digits in Abies.Parse.Many1(Abies.Parse.Satisfy(c => c != '/' && char.IsDigit(c)))
            select double.Parse(new string([.. digits]));


        public static class Strict
        {
            public static Parser<int?> Int =>
                from items in Segment.Any
                let digits = int.TryParse(new string([.. items]), out var result) ? result : (int?)null
                select digits;

            public static Parser<double?> Double =>
                from items in Segment.Any
                let digits = double.TryParse(new string([.. items]), out var result) ? result : (double?)null
                select digits;
        }

        /// <summary>
        /// A parser that parses a sequence of one or more characters that are not a '/' character.
        /// If the input does not contain at least one character or contains a '/' character, the parser fails.
        /// </summary>
        public static Parser<string> String =>
            from chars in Abies.Parse.Many(Abies.Parse.Satisfy(c => c != '/'))
            select new string([.. chars]);
   }  

}