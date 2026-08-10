namespace DfTools.Sql;

internal sealed class Cursor
{
    private readonly IReadOnlyList<Token> _tokens;
    private int _position = -1;

    public Cursor(IReadOnlyList<Token> tokens) => _tokens = tokens;

    public Token? Next(TokenType? exceptTokenType = null)
    {
        while (++_position < _tokens.Count)
        {
            var token = _tokens[_position];
            if (exceptTokenType is { } type && token.IsOfType(type))
                continue;

            return token;
        }

        return null;
    }

    public Token? Previous(TokenType? exceptTokenType = null)
    {
        while (--_position >= 0)
        {
            var token = _tokens[_position];
            if (exceptTokenType is { } type && token.IsOfType(type))
                continue;

            return token;
        }

        return null;
    }

    public Cursor SubCursor()
    {
        var cursor = new Cursor(_tokens) { _position = _position };
        return cursor;
    }
}
