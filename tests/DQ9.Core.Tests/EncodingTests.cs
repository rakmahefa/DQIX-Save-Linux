using System.Text;
using FriedGinger.DQCheat;
using Xunit;

namespace DQ9.Core.Tests;

/// <summary>
/// P0 tests for the custom DQ9 character codec (FriedGinger.DQCheat), one of
/// the format invariants listed in spec.md §4.11.
/// </summary>
public class EncodingTests
{
    [Theory]
    [InlineData('A', (byte)18)]
    [InlineData('Z', (byte)43)]
    [InlineData('a', (byte)44)]
    [InlineData('z', (byte)69)]
    [InlineData('0', (byte)8)]
    [InlineData('9', (byte)17)]
    [InlineData(' ', (byte)255)] // last table entry
    public void DQ9Encoding_MapsAsciiLettersAndDigits(char character, byte expected)
    {
        var encoding = new DQ9Encoding();
        Assert.Equal(expected, encoding.GetBytes(character.ToString())[0]);
        Assert.Equal(character, encoding.GetString(new[] { expected }, 0, 1)[0]);
    }

    [Fact]
    public void DQ9Encoding_UnknownByteDecodesToQuestionMark()
    {
        var encoding = new DQ9Encoding();
        Assert.Equal('?', encoding.GetString(new byte[] { 0x00, 0x01, 0x03 }, 0, 3)[0]);
    }

    [Fact]
    public void DQ9Encoding_UnknownCharEncodesToPlaceholderByte()
    {
        var encoding = new DQ9Encoding();
        Assert.Equal((byte)1, encoding.GetBytes("Ω")[0]);
    }

    [Fact]
    public void StringFixer_RoundTripsNamesAndNumbers()
    {
        foreach (var text in new[] { "ALLEN", "AQUA", "STEBBING 99", "L12AB", "X" })
        {
            var bytes = StringFixer.GetBytes(text);
            Assert.Equal(text, StringFixer.GetString(bytes, bytes.Length));
        }
    }

    [Fact]
    public void StringFixer_RoundTripsLongProfileMessage()
    {
        const string text = "DRAGON QUEST IX SAVE EDITOR 0.7 TEST MESSAGE ABCDEF";
        var bytes = StringFixer.GetBytes(text);
        Assert.Equal(text, StringFixer.GetString(bytes, bytes.Length));
    }

    [Fact]
    public void StringFixer_BytesAreSingleBytePerCharacter()
    {
        var bytes = StringFixer.GetBytes("ABC123");
        Assert.Equal(6, bytes.Length);
    }

    [Fact]
    public void CodePagesEncodingProvider_SupportsShiftJis()
    {
        // CheckFile registers CodePagesEncodingProvider at runtime; the
        // dependency must resolve on .NET 10 / Linux too.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Assert.Equal(SaveFileBuilder.Signature, Encoding.GetEncoding("shift-jis").GetString(
            Encoding.ASCII.GetBytes(SaveFileBuilder.Signature)));
    }
}
