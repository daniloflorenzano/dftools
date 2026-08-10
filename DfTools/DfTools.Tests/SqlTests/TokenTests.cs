using DfTools.Sql;

namespace DfTools.Tests.SqlTests;

public class TokenTests
{
    [Test]
    public void Constructor_SetsTypeAndValueProperties()
    {
        var token = new Token(TokenType.Word, "my_column");

        Assert.That(token.Type, Is.EqualTo(TokenType.Word));
        Assert.That(token.Value, Is.EqualTo("my_column"));
    }

    [Test]
    public void IsOfType_MatchingType_ReturnsTrue()
    {
        var token = new Token(TokenType.ReservedTopLevel, "SELECT");

        Assert.That(token.IsOfType(TokenType.ReservedTopLevel), Is.True);
        Assert.That(token.IsOfType(TokenType.Word, TokenType.ReservedTopLevel), Is.True);
    }

    [Test]
    public void IsOfType_NonMatchingType_ReturnsFalse()
    {
        var token = new Token(TokenType.ReservedTopLevel, "SELECT");

        Assert.That(token.IsOfType(TokenType.Word, TokenType.Boundary), Is.False);
        Assert.That(token.IsOfType(), Is.False);
    }

    [Test]
    [TestCase("SELECT", ExpectedResult = false)]
    [TestCase("SELECT ", ExpectedResult = true)]
    [TestCase("SELECT\nFROM", ExpectedResult = true)]
    [TestCase("SELECT\tFROM", ExpectedResult = true)]
    [TestCase(" GROUP BY ", ExpectedResult = true)]
    public bool HasExtraWhitespace_DetectsSpacesNewlinesAndTabs(string value)
    {
        var token = new Token(TokenType.ReservedTopLevel, value);
        return token.HasExtraWhitespace();
    }

    [Test]
    public void WithValue_ReturnsNewTokenWithSameTypeAndNewValue()
    {
        var originalToken = new Token(TokenType.ReservedTopLevel, "select");
        var newToken = originalToken.WithValue("SELECT");

        Assert.That(newToken, Is.Not.SameAs(originalToken));
        Assert.That(newToken.Type, Is.EqualTo(TokenType.ReservedTopLevel));
        Assert.That(newToken.Value, Is.EqualTo("SELECT"));
        Assert.That(originalToken.Value, Is.EqualTo("select"));
    }
}
