using DfTools.Sql;

namespace DfTools.Tests.SqlTests;

public class TokenizerTests
{
    private Tokenizer _tokenizer;

    [SetUp]
    public void Setup()
    {
        _tokenizer = new Tokenizer();
    }

    [Test]
    public void Tokenize_Whitespace_ReturnsWhitespaceToken()
    {
        var cursor = _tokenizer.Tokenize("   \n\t ");
        var token = cursor.Next();

        Assert.That(token, Is.Not.Null);
        Assert.That(token!.Type, Is.EqualTo(TokenType.Whitespace));
        Assert.That(token.Value, Is.EqualTo("   \n\t "));
        Assert.That(cursor.Next(), Is.Null);
    }

    [Test]
    public void Tokenize_SingleLineComments_RecognizesDashDashAndHash()
    {
        var cursor = _tokenizer.Tokenize("-- comment 1\n# comment 2");

        var t1 = cursor.Next();
        Assert.That(t1!.Type, Is.EqualTo(TokenType.Comment));
        Assert.That(t1.Value, Is.EqualTo("-- comment 1"));

        var t2 = cursor.Next(); // Whitespace \n
        Assert.That(t2!.Type, Is.EqualTo(TokenType.Whitespace));

        var t3 = cursor.Next();
        Assert.That(t3!.Type, Is.EqualTo(TokenType.Comment));
        Assert.That(t3.Value, Is.EqualTo("# comment 2"));
    }

    [Test]
    public void Tokenize_BlockComment_RecognizesMultiLineComments()
    {
        var cursor = _tokenizer.Tokenize("/* line 1\n line 2 */");

        var token = cursor.Next();
        Assert.That(token!.Type, Is.EqualTo(TokenType.BlockComment));
        Assert.That(token.Value, Is.EqualTo("/* line 1\n line 2 */"));
    }

    [Test]
    public void Tokenize_Quotes_RecognizesSingleDoubleAndBackticksAndBrackets()
    {
        var cursor = _tokenizer.Tokenize("'single' \"double\" `backtick` [bracket]");

        var t1 = cursor.Next();
        Assert.That(t1!.Type, Is.EqualTo(TokenType.Quote));
        Assert.That(t1.Value, Is.EqualTo("'single'"));

        cursor.Next(); // whitespace

        var t2 = cursor.Next();
        Assert.That(t2!.Type, Is.EqualTo(TokenType.Quote));
        Assert.That(t2.Value, Is.EqualTo("\"double\""));

        cursor.Next(); // whitespace

        var t3 = cursor.Next();
        Assert.That(t3!.Type, Is.EqualTo(TokenType.BacktickQuote));
        Assert.That(t3.Value, Is.EqualTo("`backtick`"));

        cursor.Next(); // whitespace

        var t4 = cursor.Next();
        Assert.That(t4!.Type, Is.EqualTo(TokenType.BacktickQuote));
        Assert.That(t4.Value, Is.EqualTo("[bracket]"));
    }

    [Test]
    public void Tokenize_Variables_RecognizesAtAndColonPrefixes()
    {
        var cursor = _tokenizer.Tokenize("@var1 :var2 @`var3` :'var4'");

        var t1 = cursor.Next();
        Assert.That(t1!.Type, Is.EqualTo(TokenType.Variable));
        Assert.That(t1.Value, Is.EqualTo("@var1"));

        cursor.Next(); // whitespace

        var t2 = cursor.Next();
        Assert.That(t2!.Type, Is.EqualTo(TokenType.Variable));
        Assert.That(t2.Value, Is.EqualTo(":var2"));

        cursor.Next(); // whitespace

        var t3 = cursor.Next();
        Assert.That(t3!.Type, Is.EqualTo(TokenType.Variable));
        Assert.That(t3.Value, Is.EqualTo("@`var3`"));

        cursor.Next(); // whitespace

        var t4 = cursor.Next();
        Assert.That(t4!.Type, Is.EqualTo(TokenType.Variable));
        Assert.That(t4.Value, Is.EqualTo(":'var4'"));
    }

    [Test]
    public void Tokenize_Numbers_RecognizesIntegerDecimalHexAndBinary()
    {
        var cursor = _tokenizer.Tokenize("123 45.67 0x1A2F 0b1010");

        var t1 = cursor.Next();
        Assert.That(t1!.Type, Is.EqualTo(TokenType.Number));
        Assert.That(t1.Value, Is.EqualTo("123"));

        cursor.Next(); // whitespace

        var t2 = cursor.Next();
        Assert.That(t2!.Type, Is.EqualTo(TokenType.Number));
        Assert.That(t2.Value, Is.EqualTo("45.67"));

        cursor.Next(); // whitespace

        var t3 = cursor.Next();
        Assert.That(t3!.Type, Is.EqualTo(TokenType.Number));
        Assert.That(t3.Value, Is.EqualTo("0x1A2F"));

        cursor.Next(); // whitespace

        var t4 = cursor.Next();
        Assert.That(t4!.Type, Is.EqualTo(TokenType.Number));
        Assert.That(t4.Value, Is.EqualTo("0b1010"));
    }

    [Test]
    public void Tokenize_Boundaries_RecognizesOperatorsAndPunctuation()
    {
        var cursor = _tokenizer.Tokenize(", ; :: ( ) = >= <=");

        var types = new List<TokenType>();
        var values = new List<string>();

        while (cursor.Next(TokenType.Whitespace) is { } t)
        {
            types.Add(t.Type);
            values.Add(t.Value);
        }

        Assert.That(types, Has.All.EqualTo(TokenType.Boundary));
        Assert.That(values, Is.EqualTo(new[] { ",", ";", "::", "(", ")", "=", ">", "=", "<", "=" }));
    }

    [Test]
    public void Tokenize_ReservedTopLevel_RecognizesTopLevelKeywords()
    {
        var cursor = _tokenizer.Tokenize("SELECT FROM WHERE GROUP BY ORDER BY LIMIT");

        var keywords = new List<string>();
        while (cursor.Next(TokenType.Whitespace) is { } t)
        {
            Assert.That(t.Type, Is.EqualTo(TokenType.ReservedTopLevel));
            keywords.Add(t.Value);
        }

        Assert.That(keywords, Is.EqualTo(new[] { "SELECT", "FROM", "WHERE", "GROUP BY", "ORDER BY", "LIMIT" }));
    }

    [Test]
    public void Tokenize_ReservedNewline_RecognizesNewlineKeywords()
    {
        var cursor = _tokenizer.Tokenize("AND OR INNER JOIN LEFT JOIN");

        var keywords = new List<string>();
        while (cursor.Next(TokenType.Whitespace) is { } t)
        {
            Assert.That(t.Type, Is.EqualTo(TokenType.ReservedNewline));
            keywords.Add(t.Value);
        }

        Assert.That(keywords, Is.EqualTo(new[] { "AND", "OR", "INNER JOIN", "LEFT JOIN" }));
    }

    [Test]
    public void Tokenize_ReservedFunctions_RecognizesFunctionsWithParentheses()
    {
        var cursor = _tokenizer.Tokenize("COUNT(*) NOW()");

        var t1 = cursor.Next();
        Assert.That(t1!.Type, Is.EqualTo(TokenType.Reserved));
        Assert.That(t1.Value, Is.EqualTo("COUNT"));

        cursor.Next(); // (
        cursor.Next(); // *
        cursor.Next(); // )
        cursor.Next(); // whitespace

        var t2 = cursor.Next();
        Assert.That(t2!.Type, Is.EqualTo(TokenType.Reserved));
        Assert.That(t2.Value, Is.EqualTo("NOW"));
    }

    [Test]
    public void Tokenize_IdentifierWords_RecognizesGenericWords()
    {
        var cursor = _tokenizer.Tokenize("table_name column1 my_alias");

        var words = new List<string>();
        while (cursor.Next(TokenType.Whitespace) is { } t)
        {
            Assert.That(t.Type, Is.EqualTo(TokenType.Word));
            words.Add(t.Value);
        }

        Assert.That(words, Is.EqualTo(new[] { "table_name", "column1", "my_alias" }));
    }
}
