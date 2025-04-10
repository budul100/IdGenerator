using IdGenerator.Extensions;

namespace IdGenerator.Tests
{
    // Test classes
    public class TestEntity;

    public class Tests
    {
        #region Public Methods

        [Fact]
        public void Generate_WithCamelCaseDisabled_MaintainsOriginalCasing()
        {
            // Arrange
            var generator = new Generator(avoidCamelCases: true);

            // Act
            var id = generator.Generate<TestEntity>("Sample");

            // Assert
            Assert.Contains("tsen", id);
        }

        [Fact]
        public void Generate_WithCustomDelimiter_UsesCorrectDelimiter()
        {
            // Arrange
            var generator = new Generator(delimiter: "-");

            // Act
            var id = generator.Generate<TestEntity>("sample", "part");

            // Assert
            Assert.Contains("sample-part", id);
        }

        [Fact]
        public void Generate_WithCustomPrefix_ReturnsCorrectFormat()
        {
            // Arrange
            var generator = new Generator();

            // Act
            var id = generator.Generate("custom", "sample");

            // Assert
            Assert.StartsWith("custom", id);
            Assert.Contains("sample", id);
        }

        [Fact]
        public void Generate_WithInvalidCharacters_RemovesInvalidCharacters()
        {
            // Arrange
            var generator = new Generator();

            // Act
            var id = generator.Generate<TestEntity>("sample@123!#");

            // Assert
            Assert.Contains("sample123", id);
        }

        [Fact]
        public void Generate_WithLongTypeName_ShrinksCorrectly()
        {
            // Arrange
            var generator = new Generator(typePrefixLength: 5);

            // Act
            var id = generator.Generate<LongEntityNameForTesting>("test");

            // Assert
            Assert.StartsWith("lEnN", id);
        }

        [Fact]
        public void Generate_WithMultipleSuffixes_CombinesSuffixesCorrectly()
        {
            // Arrange
            var generator = new Generator();

            // Act
            var id = generator.Generate<TestEntity>("base", "suffix1", "suffix2");

            // Assert
            Assert.Contains("base_suffix1_suffix2", id);
        }

        [Fact]
        public void Generate_WithSameBaseId_EnsuresUniqueness()
        {
            // Arrange
            var generator = new Generator();

            // Act
            var id1 = generator.Generate<TestEntity>("duplicate");
            var id2 = generator.Generate<TestEntity>("duplicate");

            // Assert
            Assert.NotEqual(id1, id2);
            Assert.Equal("tsEn_duplicate", id1);
            Assert.Equal("tsEn_duplicate_2", id2);
        }

        [Fact]
        public void Generate_WithTypePrefix_ReturnsCorrectFormat()
        {
            // Arrange
            var generator = new Generator();

            // Act
            var id = generator.Generate<TestEntity>("sample");

            // Assert
            Assert.StartsWith("tsEn", id);
            Assert.Contains("sample", id);
        }

        [Fact]
        public void LongRunningSequence_GeneratesCorrectSequence()
        {
            const string baseId = "sequence";
            const int count = 10;

            // Arrange
            var generator = new Generator();

            var expected = new List<string>();

            for (int i = 1; i <= count; i++)
            {
                expected.Add(i == 1
                    ? $"tsEn_{baseId}"
                    : $"tsEn_{baseId}_{i}");
            }

            // Act
            var actual = new List<string>();
            for (int i = 0; i < count; i++)
            {
                actual.Add(generator.Generate<TestEntity>(baseId));
            }

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ParallelGeneration_EnsuresUniqueness()
        {
            const int iterations = 1000;

            // Arrange
            var generator = new Generator();
            var results = new HashSet<string>();

            // Act
            Parallel.For(0, iterations, _ =>
            {
                var id = generator.Generate<TestEntity>("parallel");
                lock (results)
                {
                    results.Add(id);
                }
            });

            // Assert
            Assert.Equal(iterations, results.Count); // All IDs should be unique
        }

        [Fact]
        public void Reset_ClearsAllStoredIds()
        {
            // Arrange
            var generator = new Generator();
            generator.Generate<TestEntity>("test");

            // Act
            generator.Reset();
            var newId = generator.Generate<TestEntity>("test");

            // Assert
            Assert.Equal("tsEn_test", newId);
        }

        [Fact]
        public void StringShrinker_PreservesCasing_WhenConfigured()
        {
            // Arrange & Act
            var shrinked = StringShrinker.Shrink("ProductCategory", 8, true);

            // Assert
            Assert.StartsWith("P", shrinked); // Should preserve uppercase P
        }

        [Fact]
        public void StringShrinker_ShrinksString_AccordingToRules()
        {
            // Arrange & Act
            var shrinked = StringShrinker.Shrink("ProductCategory", 8);

            // Assert
            Assert.Equal("prdcCtgr", shrinked);
        }

        #endregion Public Methods
    }

    public class LongEntityNameForTesting;
}