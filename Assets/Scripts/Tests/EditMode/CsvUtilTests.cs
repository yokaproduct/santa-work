using NUnit.Framework;
using Santa.EditorTools;

namespace Santa.Tests.EditMode
{
    public class CsvUtilTests
    {
        [Test]
        public void Parse_SimpleUnquotedFields()
        {
            var rows = CsvUtil.Parse("a,b,c\n1,2,3\n");
            Assert.AreEqual(2, rows.Count);
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, rows[0]);
            CollectionAssert.AreEqual(new[] { "1", "2", "3" }, rows[1]);
        }

        [Test]
        public void Parse_QuotedFieldWithEmbeddedComma()
        {
            var rows = CsvUtil.Parse("id,note\nL001,\"a,b,c\"\n");
            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("a,b,c", rows[1][1]);
        }

        [Test]
        public void Parse_EscapedDoubleQuote()
        {
            var rows = CsvUtil.Parse("id,note\nL001,\"she said \"\"hi\"\"\"\n");
            Assert.AreEqual("she said \"hi\"", rows[1][1]);
        }

        [Test]
        public void Parse_LastRowWithoutTrailingNewline()
        {
            var rows = CsvUtil.Parse("a,b\n1,2");
            Assert.AreEqual(2, rows.Count);
            CollectionAssert.AreEqual(new[] { "1", "2" }, rows[1]);
        }

        [Test]
        public void Parse_HandlesCrLf()
        {
            var rows = CsvUtil.Parse("a,b\r\n1,2\r\n");
            Assert.AreEqual(2, rows.Count);
            CollectionAssert.AreEqual(new[] { "1", "2" }, rows[1]);
        }
    }
}
