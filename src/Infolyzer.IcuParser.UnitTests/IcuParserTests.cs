using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Infolyzer.IcuParser.UnitTests
{
    public class IcuParserTests
    {
        [Fact]
        public void PlainStringWithoutVariables()
        {
            var input = "Test";

            var result = MessageParser.Parse(input);

            Assert.Equal(input, result);
        }

        [Fact]
        public void ReplaceSingleVariable()
        {
            var input = "The big brown {animal}.";

            var result = MessageParser.Parse(input, new Dictionary<string, object> { {"animal", "dog"} });

            Assert.Equal("The big brown dog.", result);
        }

        [Fact]
        public void ReplaceMultipleVariables()
        {
            var input = "We own {number} {animal}s.";

            var result = MessageParser.Parse(input, new Dictionary<string, object>
            {
                {"number", 2},
                {"animal", "dog"}
            });

            Assert.Equal("We own 2 dogs.", result);
        }

        [Fact]
        public void EmptyStringBuilderBehaviorTest()
        {
            var sb = new StringBuilder();
            var token = sb.ToString();
            token = token.Trim().ToLower();
            Assert.Equal(string.Empty, token);
        }

        [Fact]
        public void StringBuilderFlushBehaviorTest()
        {
            var sb = new StringBuilder();
            sb.Append("minutes");
            var token = sb.Flush().Trim();
            Assert.Equal("minutes", token);
        }

        [Fact]
        public void ThrowInvalidCommandTest()
        {
            var input = "minute{minutes, invalid_command, =0{s} one{} other{s}}.";

            Assert.Throws<IcuParserException>(() =>
            {
                MessageParser.Parse(input, new Dictionary<string, object> { {"minutes", 1} });
            });
        }

        [Theory]
        [InlineData(0, "0 minutes.")]
        [InlineData(1, "1 minute.")]
        [InlineData(2, "2 minutes.")]
        public void SimplePluralizationTest(int minutes, string expected)
        {
            var input = "{minutes} minute{minutes, plural, =0{s} one{} other{s}}.";

            var result = MessageParser.Parse(input, new Dictionary<string, object> { {"minutes", minutes} });

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("male", "Frank", "His name is Frank")]
        [InlineData("female", "Gretchen", "Her name is Gretchen")]
        [InlineData("group", "Zoe and Alex", "Their names are Zoe and Alex")]
        public void BasicSelectTest(string gender, string name, string expected)
        {
            var input = "{ gender, select, male {His name is} female{Her name is} other{Their names are} } {name}";

            var result = MessageParser.Parse(input, new Dictionary<string, object>
            {
                { "gender", gender },
                { "name", name }
            });

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0, "There is nothing")]
        [InlineData(1, "There is a thing")]
        [InlineData(666, "There are several things")]
        public void MultiplePluralsTest(int count, string expected)
        {
            var input = "There {count, plural, " +
                            "=0{is }" +
                            "one{is }" +
                            "other{are }" +
                        "}{count, plural, " +
                            "=0{}" +
                            "one{a }" +
                            "other{several }" +
                        "}{count, plural, " +
                            "=0{nothing}" +
                            "one{thing}" +
                            "other{things}" +
                        "}";

            var result = MessageParser.Parse(input, new Dictionary<string, object> { { "count", count } });

            Assert.Equal(expected, result);
        }

        [Fact]
        public void InvalidVariableTest()
        {
            var input = "Just one branch {invalid_variable, plural, =1 {x}}";

            var msg = Assert.Throws<IcuParserException>(() =>
            {
                MessageParser.Parse(input);
            }).Message;
            Assert.Equal("Invalid variable: invalid_variable", msg);
        }

        [Fact]
        public void JustOneBranchTest()
        {
            var input = "Just one branch {variable, plural, =1 {x}}";

            var msg = Assert.Throws<IcuParserException>(() =>
            {
                MessageParser.Parse(input, new Dictionary<string, object> { { "variable", "some value" } });
            }).Message;
            Assert.Equal("Invalid conditional, should have at least 2 branches.", msg);
        }

        [Fact]
        public void PluralNotFoundTest()
        {
            var input = "{variable, plural, =1 {a} =2 {b}}";

            var result = MessageParser.Parse(input, new Dictionary<string, object> { { "variable", 10 } });

            Assert.Empty(result);
        }

        [Theory]
        [InlineData("{variable, plural, =1 {a} =2 {b}}}")]
        public void ScopeMismatchTest(string input)
        {
            var msg = Assert.Throws<IcuParserException>(() =>
            {
                MessageParser.Parse(input, new Dictionary<string, object> { { "variable", 10 } });
            }).Message;

            Assert.Equal("Scope stack mismatch", msg);
        }

        [Theory]
        [InlineData("{{variable, plural, =1 {a} =2 {b}}", "{variable")]
        [InlineData("{variable_X}", "variable_X")]
        public void InvalidVariableNameTest(string input, string expectedErrorMessageVariableName)
        {
            var msg = Assert.Throws<IcuParserException>(() =>
            {
                MessageParser.Parse(input, new Dictionary<string, object> { { "variable", 10 } });
            }).Message;

            Assert.Equal($"Invalid variable: {expectedErrorMessageVariableName}", msg);
        }

        [Theory]
        [InlineData("=0", "other", 0, "A")]
        [InlineData("=1", "other", 1, "A")]
        [InlineData("=2", "other", 2, "A")]
        [InlineData("one", "other", 1, "A")]
        [InlineData("other", "=0", 6, "A")]
        [InlineData("=0", "other", 10, "B")]
        [InlineData("=1", "other", 11, "B")]
        [InlineData("=2", "other", 12, "B")]
        [InlineData("one", "other", 11, "B")]
        public void SelectorValidityTests(string testSubjectSelector, string validAlternativeSelector, int variable, string expected)
        {
            var input = $"{{variable, plural, {testSubjectSelector}{{A}} {validAlternativeSelector}{{B}}}}";

            var result = MessageParser.Parse(input, new Dictionary<string, object> { { "variable", variable } });

            Assert.Equal(expected, result);

        }

        [Theory]
        [InlineData("=0", "other", 0, "A")]
        [InlineData("other", "=0", 0, "A")]
        public void SelectorPrecedenceTests(string testSubjectSelector, string validAlternativeSelector, int variable, string expected)
        {
            // Branches are evaluated in the order they are parsed, so "other" might short circuit remaining matching branches
            var input = $"{{variable, plural, {testSubjectSelector}{{A}} {validAlternativeSelector}{{B}}}}";

            var result = MessageParser.Parse(input, new Dictionary<string, object> { { "variable", variable } });

            Assert.Equal(expected, result);
        }

        [Fact]
        public void InvalidSelectorValueTests()
        {
            var input = "{count, plural, =2z {1} =100{10}}";

            Assert.Throws<IcuParserException>(() =>
            {
                MessageParser.Parse(input, new Dictionary<string, object> { { "count", 2 } });
            });
        }

        [Theory]
        [InlineData("Look {person}, we can use '{}'...", "ma", "Look ma, we can use {}...")]
        [InlineData("Look {person}, we can use '{}'", "ma", "Look ma, we can use {}")]
        [InlineData("'{'{person}'}'", "ma", "{ma}")]
        [InlineData("''{person}''", "ma", "'ma'")]
        [InlineData("'{'''{person}'''}'", "ma", "{'ma'}")]
        [InlineData("Don''t", "", "Don't")]
        [InlineData("Don' '''t", "", "Don 't")]
        [InlineData("Don'''a'''t", "", "Don'a't")]
        public void QuotingTest(string input, string person, string expected)
        {
            var result = MessageParser.Parse(input, new Dictionary<string, object> { { "person", person } });

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("'")]
        [InlineData("abc '123")]
        public void CatchUnterminatedQuotes(string input)
        {
            var msg = Assert.Throws<IcuParserException>(() =>
            {
                MessageParser.Parse(input, new Dictionary<string, object> { { "variable", 10 } });
            }).Message;

            Assert.Equal("Unterminated quote", msg);
        }

        [Fact]
        public void InvalidOffsetValueTests()
        {
            var input = "{count, plural, offset:1a =2 {1} offset:10 =100{10}}";

            Assert.Throws<IcuParserException>(() =>
            {
                MessageParser.Parse(input, new Dictionary<string, object> { { "count", 2 } });
            });
        }

        [Theory]
        [InlineData(2, "1 + 1")]
        [InlineData(100, "10 + 90")]
        public void OffsetTests(int count, string expected)
        {
            var input = "{count, plural, offset:1 =2 {1 + #} offset:10 =100{10 + #}}";

            var result = MessageParser.Parse(input, new Dictionary<string, object> { { "count", count } });

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("female", 0, "Pam", "Oswald", "Pam does not give a party.")]
        [InlineData("female", 1, "Pam", "Oswald", "Pam invites Oswald to her party.")]
        [InlineData("female", 2, "Pam", "Oswald", "Pam invites Oswald and one other person to her party.")]
        [InlineData("female", 12, "Pam", "Oswald", "Pam invites Oswald and 11 other people to her party.")]
        [InlineData("male", 0, "Jon", "Oswald", "Jon does not give a party.")]
        [InlineData("male", 1, "Jon", "Oswald", "Jon invites Oswald to his party.")]
        [InlineData("male", 2, "Jon", "Oswald", "Jon invites Oswald and one other person to his party.")]
        [InlineData("male", 12, "Jon", "Oswald", "Jon invites Oswald and 11 other people to his party.")]
        [InlineData("they", 0, "Jon and Pam", "Oswald", "Jon and Pam does not give a party.")]
        [InlineData("they", 1, "Jon and Pam", "Oswald", "Jon and Pam invite Oswald to their party.")]
        [InlineData("they", 2, "Jon and Pam", "Oswald", "Jon and Pam invite Oswald and one other person to their party.")]
        [InlineData("they", 12, "Jon and Pam", "Oswald", "Jon and Pam invite Oswald and #11 other people to their party.")]
        public void IcuMessageFormatExampleTest(string gender, int no_of_guests, string host, string guest, string expected)
        {
            var input = "{gender, select, " + "" +
                            "female {" +
                                "{no_of_guests, plural, " +
                                    "=0 {{host} does not give a party.}" +
                                    "=1 {{host} invites {guest} to her party.}" +
                                    "=2 {{host} invites { guest} and one other person to her party.}" +
                                    "offset:1 other {{ host} invites { guest } and # other people to her party.}" +
                                "}" +
                            "}" +
                            "male {" +
                                "{no_of_guests, plural, " +
                                    "=0 {{host} does not give a party.}" +
                                    "=1 {{host} invites {guest} to his party.}" +
                                    "=2 {{host} invites {guest} and one other person to his party.}" +
                                    "offset:1 other {{host} invites {guest} and # other people to his party.}" +
                                "}" +
                            "}" +
                            "other {" +
                                "{no_of_guests, plural, " +
                                    "=0 {{host} does not give a party.}" +
                                    "=1 {{host} invite {guest} to their party.}" +
                                    "=2 {{host} invite {guest} and one other person to their party.}" +
                                    "offset:1 other {{host} invite { guest} and '#'# other people to their party.}" +
                                "}" +
                            "}" +
                        "}";

            var result = MessageParser.Parse(input, new Dictionary<string, object>
            {
                { "gender", gender },
                { "no_of_guests", no_of_guests },
                { "host", host },
                { "guest", guest }
            });

            Assert.Equal(expected, result);
        }
    }
}