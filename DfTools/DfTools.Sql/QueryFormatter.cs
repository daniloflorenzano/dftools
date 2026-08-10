using System.Text.RegularExpressions;

namespace DfTools.Sql;

public class QueryFormatter
{
    private const string IndentTypeBlock = "block";
    private const string IndentTypeSpecial = "special";

    private readonly Tokenizer _tokenizer = new();

    public string Format(string query, string indentString = "  ")
    {
        var result = string.Empty;
        var tab = "\t";

        var indentLevel = 0;
        var newline = false;
        var inlineParentheses = false;
        var increaseSpecialIndent = false;
        var increaseBlockIndent = false;
        var indentTypes = new List<string>();
        var addedNewline = false;
        var inlineCount = 0;
        var inlineIndented = false;
        var clauseLimit = false;

        void AppendNewLineIfNotAdded()
        {
            // Add a newline if not already added
            if (addedNewline)
                return;

            result = result.TrimEnd(' ', '\t');
            result += "\n" + new string('\t', Math.Max(0, indentLevel));
        }

        void DecreaseIndentationLevel()
        {
            if (indentTypes.Count > 0)
                indentTypes.RemoveAt(indentTypes.Count - 1);
            indentLevel--;

            // Redo the indentation since it may be different now
            var tail = indentLevel + 2;
            var lastPossiblyIndentLine = tail > 0 && tail <= result.Length
                ? result[^tail..]
                : "";
            if (lastPossiblyIndentLine.TrimEnd('\t') != "\n")
                return;

            var rtrimLength = indentLevel + 1;
            while (rtrimLength + 2 <= result.Length && result[result.Length - (rtrimLength + 2)] == '\n')
                rtrimLength++;

            result = result.Substring(0, Math.Max(0, result.Length - rtrimLength)) + new string('\t', Math.Max(0, indentLevel));
        }

        // Tokenize String
        var cursor = _tokenizer.Tokenize(query);

        // Format token by token
        while (cursor.Next(TokenType.Whitespace) is { } token)
        {
            var prevNotWhitespaceToken = cursor.SubCursor().Previous(TokenType.Whitespace);
            string? tokenValueUpper = token.Value.ToUpperInvariant();
            if (prevNotWhitespaceToken is not null && prevNotWhitespaceToken.Value == ".")
                tokenValueUpper = null;

            var highlighted = token.Type == TokenType.ReservedTopLevel
                ? token.Value.ToUpperInvariant()
                : token.Value;

            // If we are increasing the special indent level now
            if (increaseSpecialIndent)
            {
                indentLevel++;
                increaseSpecialIndent = false;
                indentTypes.Add(IndentTypeSpecial);
            }

            // If we are increasing the block indent level now
            if (increaseBlockIndent)
            {
                indentLevel++;
                increaseBlockIndent = false;
                indentTypes.Add(IndentTypeBlock);
            }

            // If we need a new line before the token
            if (newline)
            {
                result = result.TrimEnd(' ');

                if (prevNotWhitespaceToken is not null && prevNotWhitespaceToken.Value == ";")
                    result += "\n";

                result += "\n" + new string('\t', Math.Max(0, indentLevel));
                newline = false;
                addedNewline = true;
            }
            else
            {
                addedNewline = false;
            }

            // Display comments directly where they appear in the source
            if (token.IsOfType(TokenType.Comment, TokenType.BlockComment))
            {
                if (token.IsOfType(TokenType.BlockComment))
                {
                    var indent = new string('\t', Math.Max(0, indentLevel));
                    result = result.TrimEnd(' ', '\t');
                    result += "\n" + indent;
                    highlighted = highlighted.Replace("\n", "\n" + indent);
                }

                result += highlighted;
                newline = true;
                continue;
            }

            if (inlineParentheses)
            {
                // End of inline parentheses
                if (token.Value == ")")
                {
                    result = result.TrimEnd(' ');

                    if (inlineIndented)
                    {
                        DecreaseIndentationLevel();

                        result = result.TrimEnd(' ');
                        result += "\n" + new string('\t', Math.Max(0, indentLevel));
                    }

                    inlineParentheses = false;

                    result += highlighted + " ";
                    continue;
                }

                if (token.Value == ",")
                {
                    if (inlineCount >= 30)
                    {
                        inlineCount = 0;
                        newline = true;
                    }
                }

                inlineCount += token.Value.Length;
            }

            // Opening parentheses increase the block indent level and start a new line
            if (token.Value == "(")
            {
                // First check if this should be an inline parentheses block
                // Examples are "NOW()", "COUNT(*)", "int(10)", key(`somecolumn`), DECIMAL(7,2)
                // Allow up to 3 non-whitespace tokens inside inline parentheses
                var length = 0;
                var subCursor = cursor.SubCursor();
                for (var j = 1; j <= 250; j++)
                {
                    // Reached end of string
                    var next = subCursor.Next(TokenType.Whitespace);
                    if (next is null)
                        break;

                    // Reached closing parentheses, able to inline it
                    if (next.Value == ")")
                    {
                        inlineParentheses = true;
                        inlineCount = 0;
                        inlineIndented = false;
                        break;
                    }

                    // Reached an invalid token for inline parentheses
                    if (next.Value == ";" || next.Value == "(")
                        break;

                    // Reached an invalid token type for inline parentheses
                    if (next.IsOfType(TokenType.ReservedTopLevel, TokenType.ReservedNewline, TokenType.Comment, TokenType.BlockComment))
                        break;

                    length += next.Value.Length;
                }

                if (inlineParentheses && length > 30)
                {
                    increaseBlockIndent = true;
                    inlineIndented = true;
                    newline = true;
                }

                // Take out the preceding space unless there was whitespace there in the original query
                var prevToken = cursor.SubCursor().Previous();
                if (prevToken is not null && !prevToken.IsOfType(TokenType.Whitespace))
                    result = result.TrimEnd(' ');

                if (!inlineParentheses)
                {
                    increaseBlockIndent = true;
                    // Add a newline after the parentheses
                    newline = true;
                }
            }
            else if (token.Value == ")")
            {
                // Closing parentheses decrease the block indent level
                // Remove whitespace before the closing parentheses
                result = result.TrimEnd(' ');

                while (indentTypes.Count > 0 && indentTypes[^1] == IndentTypeSpecial)
                    DecreaseIndentationLevel();

                DecreaseIndentationLevel();

                if (indentLevel < 0)
                {
                    // This is an error
                    indentLevel = 0;

                    result += token.Value;
                    continue;
                }

                AppendNewLineIfNotAdded();
            }
            else if (token.IsOfType(TokenType.ReservedTopLevel))
            {
                // Top level reserved words start a new line and increase the special indent level
                increaseSpecialIndent = true;

                // If the last indent type was special, decrease the special indent for this round
                if (indentTypes.Count > 0 && indentTypes[^1] == IndentTypeSpecial)
                    DecreaseIndentationLevel();

                // Add a newline after the top level reserved word
                newline = true;

                AppendNewLineIfNotAdded();

                if (token.HasExtraWhitespace())
                    highlighted = Regex.Replace(highlighted, @"\s+", " ");

                // if SQL 'LIMIT' clause, start variable to reset newline
                if (tokenValueUpper == "LIMIT" && !inlineParentheses)
                    clauseLimit = true;
            }
            else if (token.Value == ";")
            {
                // If the last indent type was special, decrease the special indent for this round
                if (indentTypes.Count > 0 && indentTypes[^1] == IndentTypeSpecial)
                    DecreaseIndentationLevel();

                newline = true;
            }
            else if (tokenValueUpper == "CASE")
            {
                increaseBlockIndent = true;
            }
            else if (tokenValueUpper == "BEGIN")
            {
                newline = true;
                increaseBlockIndent = true;
            }
            else if (tokenValueUpper == "LOOP")
            {
                // https://docs.oracle.com/en/database/oracle/oracle-database/19/lnpls/basic-LOOP-statement.html
                if (prevNotWhitespaceToken is not null && prevNotWhitespaceToken.Value.ToUpperInvariant() != "END")
                {
                    newline = true;
                    increaseBlockIndent = true;
                }
            }
            else if (tokenValueUpper is "WHEN" or "THEN" or "ELSE" or "END")
            {
                if (tokenValueUpper != "THEN")
                {
                    DecreaseIndentationLevel();

                    if (prevNotWhitespaceToken is not null && prevNotWhitespaceToken.Value.ToUpperInvariant() != "CASE")
                        AppendNewLineIfNotAdded();
                }

                if (tokenValueUpper is "THEN" or "ELSE")
                {
                    newline = true;
                    increaseBlockIndent = true;
                }
            }
            else if (clauseLimit && token.Value != "," && !token.IsOfType(TokenType.Number, TokenType.Whitespace))
            {
                // Checks if we are out of the limit clause
                clauseLimit = false;
            }
            else if (token.Value == "," && !inlineParentheses)
            {
                // Commas start a new line (unless within inline parentheses or SQL 'LIMIT' clause)
                if (clauseLimit)
                {
                    newline = false;
                    clauseLimit = false;
                }
                else
                {
                    newline = true;
                }
            }
            else if (token.IsOfType(TokenType.ReservedNewline))
            {
                // Newline reserved words start a new line
                AppendNewLineIfNotAdded();

                if (token.HasExtraWhitespace())
                    highlighted = Regex.Replace(highlighted, @"\s+", " ");
            }
            else if (token.IsOfType(TokenType.Boundary))
            {
                // Multiple boundary characters in a row should not have spaces between them (not including parentheses)
                if (prevNotWhitespaceToken is not null && prevNotWhitespaceToken.IsOfType(TokenType.Boundary))
                {
                    var prevToken = cursor.SubCursor().Previous();
                    if (prevToken is not null && !prevToken.IsOfType(TokenType.Whitespace))
                        result = result.TrimEnd(' ');
                }
            }

            // If the token shouldn't have a space before it
            if (token.Value is "." or "," or ";")
                result = result.TrimEnd(' ');

            result += highlighted + " ";

            // If the token shouldn't have a space after it
            if (token.Value is "(" or ".")
                result = result.TrimEnd(' ');

            // If this is the "-" of a negative number, it shouldn't have a space after it
            if (token.Value != "-")
                continue;

            var nextNotWhitespace = cursor.SubCursor().Next(TokenType.Whitespace);
            if (nextNotWhitespace is null || !nextNotWhitespace.IsOfType(TokenType.Number))
                continue;

            var prev = cursor.SubCursor().Previous(TokenType.Whitespace);
            if (prev is null)
                continue;

            if (prev.IsOfType(TokenType.Quote, TokenType.BacktickQuote, TokenType.Word, TokenType.Number))
                continue;

            result = result.TrimEnd(' ');
        }

        // If there are unmatched parentheses
        if (indentTypes.Contains(IndentTypeBlock))
        {
            result = result.TrimEnd(' ');
            result += "WARNING: unclosed parentheses or section";
        }

        // Replace tab characters with the configuration tab character
        result = result.Replace(tab, indentString).Trim();

        return result;
    }
}
