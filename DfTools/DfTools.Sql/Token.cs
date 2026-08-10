namespace DfTools.Sql;

internal sealed class Token
{
    public Token(TokenType type, string value)
    {
        Type = type;
        Value = value;
    }

    public TokenType Type { get; }

    public string Value { get; }

    public bool IsOfType(params TokenType[] types) => types.Contains(Type);

    public bool HasExtraWhitespace() =>
        Value.Contains(' ') || Value.Contains('\n') || Value.Contains('\t');

    public Token WithValue(string value) => new(Type, value);
}
