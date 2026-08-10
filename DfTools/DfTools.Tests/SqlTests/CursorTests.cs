using DfTools.Sql;

namespace DfTools.Tests.SqlTests;

public class CursorTests
{
    [Test]
    public void Next_EmptyTokenList_ReturnsNull()
    {
        var cursor = new Cursor(new List<Token>());

        Assert.That(cursor.Next(), Is.Null);
    }

    [Test]
    public void Next_AdvancesThroughTokensInOrder()
    {
        var t1 = new Token(TokenType.ReservedTopLevel, "SELECT");
        var t2 = new Token(TokenType.Whitespace, " ");
        var t3 = new Token(TokenType.Word, "col");
        var cursor = new Cursor(new[] { t1, t2, t3 });

        Assert.That(cursor.Next(), Is.EqualTo(t1));
        Assert.That(cursor.Next(), Is.EqualTo(t2));
        Assert.That(cursor.Next(), Is.EqualTo(t3));
        Assert.That(cursor.Next(), Is.Null);
    }

    [Test]
    public void Next_ExceptTokenType_SkipsMatchingTokens()
    {
        var t1 = new Token(TokenType.ReservedTopLevel, "SELECT");
        var t2 = new Token(TokenType.Whitespace, " ");
        var t3 = new Token(TokenType.Word, "col");
        var cursor = new Cursor(new[] { t1, t2, t3 });

        Assert.That(cursor.Next(TokenType.Whitespace), Is.EqualTo(t1));
        Assert.That(cursor.Next(TokenType.Whitespace), Is.EqualTo(t3));
        Assert.That(cursor.Next(TokenType.Whitespace), Is.Null);
    }

    [Test]
    public void Previous_MovesBackwardsThroughTokens()
    {
        var t1 = new Token(TokenType.ReservedTopLevel, "SELECT");
        var t2 = new Token(TokenType.Whitespace, " ");
        var t3 = new Token(TokenType.Word, "col");
        var cursor = new Cursor(new[] { t1, t2, t3 });

        cursor.Next(); // at t1
        cursor.Next(); // at t2
        cursor.Next(); // at t3

        Assert.That(cursor.Previous(), Is.EqualTo(t2));
        Assert.That(cursor.Previous(), Is.EqualTo(t1));
        Assert.That(cursor.Previous(), Is.Null);
    }

    [Test]
    public void Previous_ExceptTokenType_SkipsMatchingTokens()
    {
        var t1 = new Token(TokenType.ReservedTopLevel, "SELECT");
        var t2 = new Token(TokenType.Whitespace, " ");
        var t3 = new Token(TokenType.Word, "col");
        var cursor = new Cursor(new[] { t1, t2, t3 });

        cursor.Next(); // at t1
        cursor.Next(); // at t2
        cursor.Next(); // at t3

        Assert.That(cursor.Previous(TokenType.Whitespace), Is.EqualTo(t1));
        Assert.That(cursor.Previous(TokenType.Whitespace), Is.Null);
    }

    [Test]
    public void SubCursor_SharesTokensAndPosition_DoesNotAffectParentPositionOnMove()
    {
        var t1 = new Token(TokenType.ReservedTopLevel, "SELECT");
        var t2 = new Token(TokenType.Whitespace, " ");
        var t3 = new Token(TokenType.Word, "col");
        var cursor = new Cursor(new[] { t1, t2, t3 });

        cursor.Next(); // Position 0 (t1)

        var subCursor = cursor.SubCursor();
        Assert.That(subCursor.Next(), Is.EqualTo(t2)); // SubCursor advances to position 1
        Assert.That(subCursor.Next(), Is.EqualTo(t3)); // SubCursor advances to position 2

        // Parent cursor remains at position 0
        Assert.That(cursor.Next(), Is.EqualTo(t2));
    }
}
