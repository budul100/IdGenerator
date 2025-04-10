using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using IdGenerator.Extensions;

namespace IdGenerator
{
    /// <summary>
    /// Generator for unique identifiers.
    /// </summary>
    public class Generator
    {
        #region Private Fields

        private readonly bool avoidCamelCases;
        private readonly string delimiter;
        private readonly ConcurrentDictionary<string, int> identifierCounts = new ConcurrentDictionary<string, int>();
        private readonly Regex invalidCharactersRegex;
        private readonly ConcurrentDictionary<string, string> typePrefixCache = new ConcurrentDictionary<string, string>();
        private readonly int typePrefixLength;
        private readonly ConcurrentDictionary<string, bool> usedIds = new ConcurrentDictionary<string, bool>();

        #endregion Private Fields

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Generator"/>.
        /// </summary>
        /// <param name="delimiter">Delimiter for ID parts.</param>
        /// <param name="avoidCamelCases">Avoid converting type prefix to camel case.</param>
        /// <param name="typePrefixLength">Maximum length of the type prefix.</param>
        /// <param name="invalidCharactersPattern">Regular expression for removing invalid characters.</param>
        public Generator(
            string delimiter = "_",
            bool avoidCamelCases = false,
            int typePrefixLength = 4,
            string invalidCharactersPattern = "[^A-Za-z0-9]+")
        {
            this.delimiter = delimiter;
            this.avoidCamelCases = avoidCamelCases;
            this.typePrefixLength = typePrefixLength;

            invalidCharactersRegex = new Regex(invalidCharactersPattern);
        }

        #endregion Public Constructors

        #region Public Methods

        /// <summary>
        /// Generates a unique ID based on the specified type and optional parameters.
        /// </summary>
        /// <typeparam name="T">The type from which the prefix is derived.</typeparam>
        /// <param name="id">An optional base ID.</param>
        /// <param name="suffixes">Optional suffixes.</param>
        /// <returns>A unique ID.</returns>
        public string Generate<T>(string id = null, params string[] suffixes)
        {
            var typePrefix = GetTypePrefix<T>();
            var contentPart = BuildContentPart(
                id: id,
                suffixes: suffixes);

            var baseId = !string.IsNullOrEmpty(contentPart)
                ? $"{typePrefix}{delimiter}{contentPart}"
                : typePrefix;

            var result = EnsureUniqueness(baseId);

            return result;
        }

        /// <summary>
        /// Creates a unique identifier with a custom prefix.
        /// </summary>
        /// <param name="prefix">Custom prefix to use instead of type-derived prefix.</param>
        /// <param name="id">An optional base ID.</param>
        /// <param name="suffixes">Optional suffixes.</param>
        /// <returns>A unique ID.</returns>
        public string Generate(string prefix, string id = null, params string[] suffixes)
        {
            if (string.IsNullOrEmpty(prefix))
                throw new ArgumentException("Prefix cannot be empty.", nameof(prefix));

            var contentPart = BuildContentPart(
                id: id,
                suffixes: suffixes);

            var baseId = !string.IsNullOrEmpty(contentPart)
                ? $"{prefix}{delimiter}{contentPart}"
                : prefix;

            var result = EnsureUniqueness(baseId);

            return result;
        }

        /// <summary>
        /// Clears all stored IDs and counters.
        /// </summary>
        public void Reset()
        {
            identifierCounts.Clear();
            usedIds.Clear();
            typePrefixCache.Clear();
        }

        #endregion Public Methods

        #region Private Methods

        private string BuildContentPart(string id, string[] suffixes)
        {
            var parts = new StringBuilder();

            // Process the base ID
            if (!string.IsNullOrEmpty(id))
            {
                var cleanId = invalidCharactersRegex.Replace(id.Trim(), string.Empty);
                if (!string.IsNullOrEmpty(cleanId))
                    parts.Append(cleanId);
            }

            // Process the suffixes
            if (suffixes?.Length > 0)
            {
                var validSuffixes = suffixes.Where(s => !string.IsNullOrEmpty(s)).ToArray();

                foreach (var suffix in validSuffixes)
                {
                    if (parts.Length > 0)
                        parts.Append(delimiter);

                    parts.Append(invalidCharactersRegex.Replace(suffix.Trim(), string.Empty));
                }
            }

            return parts.ToString();
        }

        private string EnsureUniqueness(string baseId)
        {
            var count = identifierCounts.AddOrUpdate(baseId, 1, (_, c) => c + 1);

            var result = count == 1
                ? baseId
                : $"{baseId}{delimiter}{count}";

            // Ensure the resulting ID is truly unique
            while (!usedIds.TryAdd(
                key: result,
                value: true))
            {
                count++;
                result = $"{baseId}{delimiter}{count}";
            }

            return result;
        }

        private string GetTypePrefix<T>()
        {
            var typeName = typeof(T).Name;

            return typePrefixCache.GetOrAdd(typeName, _ =>
            {
                var shrinked = typeName.Shrink(
                    maxLength: typePrefixLength,
                    preserveCasing: avoidCamelCases);

                return avoidCamelCases
                    ? shrinked.ToLowerInvariant()
                    : shrinked;
            });
        }

        #endregion Private Methods
    }
}