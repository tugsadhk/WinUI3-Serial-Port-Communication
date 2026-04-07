using WinUI3_Serial_Port_Communication;
using Xunit;

namespace WinUI3_Serial_Port_Communication.Tests
{
    public class SerialTerminalHelpersTests
    {
        // ------------------------------------------------------------------ //
        //  FormatBytes                                                        //
        // ------------------------------------------------------------------ //

        [Theory]
        [InlineData(0,           "0 B")]
        [InlineData(1,           "1 B")]
        [InlineData(1023,        "1023 B")]
        [InlineData(1024,        "1.0 KB")]
        [InlineData(1536,        "1.5 KB")]
        [InlineData(1048576,     "1.0 MB")]
        [InlineData(1572864,     "1.5 MB")]
        public void FormatBytes_ReturnsCorrectUnit(int bytes, string expected)
        {
            Assert.Equal(expected, SerialTerminalHelpers.FormatBytes(bytes));
        }

        // ------------------------------------------------------------------ //
        //  TrimToMaxLines                                                     //
        // ------------------------------------------------------------------ //

        [Fact]
        public void TrimToMaxLines_UnderLimit_ReturnsOriginal()
        {
            string text = "line1\nline2\nline3";
            Assert.Equal(text, SerialTerminalHelpers.TrimToMaxLines(text, 10));
        }

        [Fact]
        public void TrimToMaxLines_ExactlyAtLimit_ReturnsOriginal()
        {
            // "a\nb\nc\n" has 3 newlines; maxLines=3 should not trim
            string text = "a\nb\nc\n";
            Assert.Equal(text, SerialTerminalHelpers.TrimToMaxLines(text, 3));
        }

        [Fact]
        public void TrimToMaxLines_OverLimit_KeepsTail()
        {
            // 5 lines, keep last 3: "3\n4\n5\n"
            string text = "1\n2\n3\n4\n5\n";
            string result = SerialTerminalHelpers.TrimToMaxLines(text, 3);
            Assert.Equal("3\n4\n5\n", result);
        }

        [Fact]
        public void TrimToMaxLines_EmptyString_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, SerialTerminalHelpers.TrimToMaxLines(string.Empty, 10));
        }

        [Fact]
        public void TrimToMaxLines_NoNewlines_ReturnsOriginal()
        {
            string text = "no newlines here";
            Assert.Equal(text, SerialTerminalHelpers.TrimToMaxLines(text, 5));
        }
    }
}
