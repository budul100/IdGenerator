using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace IdGenerator.Extensions
{
    /// <summary>
    /// Provides string shrinking capabilities to reduce string length while maintaining readability.
    /// </summary>
    public static class StringShrinker
    {
        #region Private Fields

        private const string Vowels = "AEIOUaeiouÁÀÄÂĂĀĄÅÃÆáàäâăāąåãæÉÈËÊĔĚĒĘéèëêĕěēęÍÌÏÎĬĪĮĨíìïîĭīįĩÓÒÖÔŎŌØÕŐóòöôŏōøõőÚÙÜÛŬŪŲŨúùüûŭūųũ";

        // Detects CamelCase format - looks for uppercase letters preceded by lowercase letters
        private static readonly Regex CamelCasePattern = new Regex(
            pattern: @"(\p{Ll})(\p{Lu})",
            options: RegexOptions.Compiled);

        // Matches word separators (spaces, punctuation, special characters)
        private static readonly Regex SplitPattern = new Regex(
            pattern: @"[_\s\W]",
            options: RegexOptions.Compiled);

        #endregion Private Fields

        #region Public Methods

        /// <summary>
        /// Shrinks a string to a specified maximum length while maintaining readability.
        /// </summary>
        /// <param name="value">The string to shrink.</param>
        /// <param name="maxLength">The maximum length of the output string. Use 0 for no limit.</param>
        /// <param name="preserveCasing">Whether to preserve casing of original words (if false, will convert to camelCase).</param>
        /// <returns>A shrinked version of the input string.</returns>
        public static string Shrink(this string value, int maxLength, bool preserveCasing = false)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            value = value.Trim();

            // Split the string at camel case boundaries and other separators
            var parts = SplitIntoParts(value);

            if (parts.Count == 0)
                return string.Empty;

            // Shrink each part by removing internal vowels
            for (var i = 0; i < parts.Count; i++)
            {
                parts[i] = ShrinkVowels(
                    input: parts[i],
                    preserveCasing: preserveCasing || i > 0);
            }

            // If maxLength is 0 or greater than the result would be, just return the shrinked parts
            var initialResult = string.Concat(parts);

            if (maxLength == 0 || maxLength >= initialResult.Length)
            {
                return preserveCasing
                    ? initialResult
                    : ToCamelCaseStyle(parts);
            }

            // If still too long, proportionally shrink each part
            AllocateCharacters(
                parts: parts,
                totalAllowedLength: maxLength);

            // Convert to camelCase if needed
            return preserveCasing
                ? string.Concat(parts)
                : ToCamelCaseStyle(parts);
        }

        /// <summary>
        /// Converts a string to camelCase (first character lowercase, subsequent words capitalized).
        /// </summary>
        public static string ToCamelCase(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var parts = SplitIntoParts(input);

            return ToCamelCaseStyle(parts);
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Allocates characters to each part proportionally based on original length.
        /// </summary>
        private static void AllocateCharacters(List<string> parts, int totalAllowedLength)
        {
            var totalLength = parts.Sum(p => p.Length);
            var remainingChars = totalAllowedLength;

            for (var i = 0; i < parts.Count - 1; i++)
            {
                // Calculate proportional length for this part
                var allocatedChars = (int)Math.Ceiling((double)parts[i].Length / totalLength * totalAllowedLength);

                // Don't allocate more than we have or more than the part's length
                allocatedChars = Math.Min(allocatedChars, remainingChars);
                allocatedChars = Math.Min(allocatedChars, parts[i].Length);

                if (allocatedChars <= 0)
                {
                    parts[i] = string.Empty;
                    continue;
                }

                parts[i] = parts[i].Substring(0, allocatedChars);
                remainingChars -= allocatedChars;
            }

            // Allocate remaining chars to the last part
            if (parts.Count > 0 && remainingChars > 0)
            {
                var lastIndex = parts.Count - 1;
                var lastPartAllocation = Math.Min(remainingChars, parts[lastIndex].Length);
                parts[lastIndex] = parts[lastIndex].Substring(0, lastPartAllocation);
            }
        }

        /// <summary>
        /// Shrinks a string by removing vowels that are not at the beginning of a word.
        /// Preserves uppercase letters.
        /// </summary>
        private static string ShrinkVowels(string input, bool preserveCasing)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Special case to handle uppercase vowels correctly
            var result = new StringBuilder();
            var isFirstChar = true;

            for (var i = 0; i < input.Length; i++)
            {
                var c = input[i];

                // Keep the character if:
                // 1. It's the first character of the word
                // 2. It's an uppercase letter
                // 3. It's not a vowel

                if (isFirstChar || char.IsUpper(c))
                {
                    result.Append(c);
                }
                else
                {
                    var isVowel = Vowels.Contains(c);

                    if (!isVowel)
                    {
                        result.Append(c);
                    }
                }

                isFirstChar = false;
            }

            // Apply casing if needed
            if (!preserveCasing && result.Length > 0)
            {
                result[0] = char.ToLowerInvariant(result[0]);
            }

            return result.ToString();
        }

        /// <summary>
        /// Splits a string into parts at camelCase boundaries and other separators.
        /// </summary>
        private static List<string> SplitIntoParts(string input)
        {
            // First split by explicit separators
            var explicitParts = SplitPattern.Split(input)
                .Where(p => !string.IsNullOrEmpty(p)).ToList();

            var result = new List<string>();

            // Then split each part by camelCase boundaries
            foreach (var explicitPart in explicitParts)
            {
                // Insert a marker before each capital letter that follows a lowercase letter
                var withMarkers = CamelCasePattern.Replace(
                    input: explicitPart,
                    replacement: "$1|$2");

                // Split by the markers we just added
                var camelParts = withMarkers.Split(
                    separator: new[] { '|' },
                    options: StringSplitOptions.RemoveEmptyEntries);

                result.AddRange(camelParts);
            }

            return result;
        }

        /// <summary>
        /// Converts a list of parts to camelCase style (first part lowercase, rest with first letter uppercase).
        /// </summary>
        private static string ToCamelCaseStyle(List<string> parts)
        {
            if (parts.Count == 0)
                return string.Empty;

            var result = new StringBuilder();

            // First part should start with lowercase
            if (parts[0].Length > 0)
            {
                result.Append(char.ToLowerInvariant(parts[0][0]));

                if (parts[0].Length > 1)
                {
                    result.Append(parts[0].Substring(1));
                }
            }

            // Subsequent parts should start with uppercase
            for (var i = 1; i < parts.Count; i++)
            {
                if (parts[i].Length > 0)
                {
                    result.Append(char.ToUpperInvariant(parts[i][0]));

                    if (parts[i].Length > 1)
                    {
                        result.Append(parts[i].Substring(1));
                    }
                }
            }

            return result.ToString();
        }

        #endregion Private Methods
    }
}