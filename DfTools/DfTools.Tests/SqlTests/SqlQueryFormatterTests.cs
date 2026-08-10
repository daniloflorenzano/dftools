using DfTools.Sql;

namespace DfTools.Tests.SqlTests;

public class SqlQueryFormatterTests
{
    private QueryFormatter _formatter;

    [SetUp]
    public void Setup()
    {
        _formatter = new QueryFormatter();
    }

    [Test]
    [Category("Sql")]
    [TestCase(ExampleQueryA, FormattedQueryA)]
    [TestCase(ExampleQueryB, FormattedQueryB)]
    public void FormatQuery_Should_Work(string input, string expectedOutput)
    {
        var formattedQuery = _formatter.Format(input);

        Assert.That(formattedQuery, Is.EqualTo(expectedOutput));
    }

    [Test]
    [Category("Sql")]
    public void Format_WithCustomIndentString_UsesCustomIndent()
    {
        var input = "SELECT id, name FROM table";
        var expected = "SELECT\n    id,\n    name\nFROM\n    table";
        var result = _formatter.Format(input, "    ");
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [Category("Sql")]
    public void Format_TopLevelReservedWords_UppercasesAndIndents()
    {
        var input = "select a from b where c group by d order by e limit 10";
        var expected = """
                       SELECT
                         a
                       FROM
                         b
                       WHERE
                         c
                       GROUP BY
                         d
                       ORDER BY
                         e
                       LIMIT
                         10
                       """;
        var result = _formatter.Format(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [Category("Sql")]
    public void Format_DotMemberAccess_DoesNotUppercaseAfterDot()
    {
        var input = "SELECT table.select FROM schema.table";
        var expected = """
                       SELECT
                         table.select
                       FROM
                         schema.table
                       """;
        var result = _formatter.Format(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [Category("Sql")]
    public void Format_ReservedTopLevelWithExtraWhitespace_CollapsesSpaces()
    {
        var input = "SELECT a FROM b GROUP   BY c ORDER   BY d";
        var expected = """
                       SELECT
                         a
                       FROM
                         b
                       GROUP BY
                         c
                       ORDER BY
                         d
                       """;
        var result = _formatter.Format(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [Category("Sql")]
    public void Format_ReservedNewlineWords_StartsNewlineWithoutIncreasingIndent()
    {
        var input = "SELECT a FROM b WHERE c AND d OR e";
        var expected = """
                       SELECT
                         a
                       FROM
                         b
                       WHERE
                         c
                         AND d
                         OR e
                       """;
        var result = _formatter.Format(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [Category("Sql")]
    public void Format_Commas_AddNewlinesOutsideInlineParentheses()
    {
        var input = "SELECT col1, col2, col3 FROM table";
        var expected = """
                       SELECT
                         col1,
                         col2,
                         col3
                       FROM
                         table
                       """;
        var result = _formatter.Format(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [Category("Sql")]
    public void Format_LimitClause_KeepsCommaSeparatedNumbersInline()
    {
        var input = "SELECT col FROM table LIMIT 5, 10";
        var expected = """
                       SELECT
                         col
                       FROM
                         table
                       LIMIT
                         5, 10
                       """;
        var result = _formatter.Format(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [Category("Sql")]
    public void Format_ShortInlineParentheses_KeepsSingleLine()
    {
        var input = "SELECT COUNT(*), NOW() FROM table";
        var expected = """
                       SELECT
                         COUNT(*),
                         NOW()
                       FROM
                         table
                       """;
        var result = _formatter.Format(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [Category("Sql")]
    public void Format_LongInlineParentheses_FormatsWithIndentedNewline()
    {
        var input = "SELECT fn(very_long_column_name_123456789, another_very_long_column_name_123456789) FROM table";
        var expected = """
                       SELECT
                         fn(
                           very_long_column_name_123456789,
                           another_very_long_column_name_123456789
                         )
                       FROM
                         table
                       """;
        var result = _formatter.Format(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [Category("Sql")]
    public void Format_NonInlineParentheses_IncreasesBlockIndent()
    {
        var input = "SELECT col FROM table WHERE (col = 1 AND col2 = 2)";
        var expected = """
                       SELECT
                         col
                       FROM
                         table
                       WHERE
                         (
                           col = 1
                           AND col2 = 2
                         )
                       """;
        var result = _formatter.Format(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [Category("Sql")]
    public void Format_UnclosedParentheses_AppendsWarning()
    {
        var input = "SELECT col FROM table WHERE (col = 1";
        var expected = """
                       SELECT
                         col
                       FROM
                         table
                       WHERE
                         (
                           col = 1WARNING: unclosed parentheses or section
                       """;
        var result = _formatter.Format(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [Category("Sql")]
    public void Format_ExtraClosingParentheses_ResetsIndentLevelToZero()
    {
        var input = "SELECT col FROM table WHERE col = 1))";
        var expected = """
                       SELECT
                         col
                       FROM
                         table
                       WHERE
                         col = 1))
                       """;
        var result = _formatter.Format(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [Category("Sql")]
    public void Format_CaseStatements_IndentsWhenThenElseEnd()
    {
        var input = "SELECT CASE WHEN col = 1 THEN 'one' ELSE 'other' END FROM table";
        var expected = """
                       SELECT
                         CASE WHEN col = 1 THEN
                           'one'
                         ELSE
                           'other'
                         END
                       FROM
                         table
                       """;
        var result = _formatter.Format(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [Category("Sql")]
    public void Format_CaseStatement_DirectlyAfterCase_DoesNotAddNewlineBeforeWhen()
    {
        var input = "SELECT CASE WHEN a = 1 THEN 2 END FROM table";
        var expected = """
                       SELECT
                         CASE WHEN a = 1 THEN
                           2
                         END
                       FROM
                         table
                       """;
        var result = _formatter.Format(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [Category("Sql")]
    public void Format_BeginBlock_IncreasesBlockIndent()
    {
        var input = "BEGIN SELECT col FROM table; END";
        var expected = """
                       BEGIN
                         SELECT
                           col
                         FROM
                           table;
                       END
                       """;
        var result = _formatter.Format(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [Category("Sql")]
    public void Format_LoopBlock_IncreasesBlockIndentUnlessPrecededByEnd()
    {
        var input = "LOOP SELECT col FROM table; END LOOP;";
        var expected = """
                       LOOP
                       SELECT
                         col
                       FROM
                         table;
                       END LOOP;
                       """;
        var result = _formatter.Format(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [Category("Sql")]
    public void Format_Comments_PreservedInSourcePosition()
    {
        var input = "SELECT col -- single line comment\nFROM table /* block\ncomment */ WHERE x = 1";
        var expected = """
                       SELECT
                         col -- single line comment
                       FROM
                         table
                         /* block
                         comment */
                       WHERE
                         x = 1
                       """;
        var result = _formatter.Format(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [Category("Sql")]
    public void Format_Semicolon_ResetsSpecialIndentAndAddsNewline()
    {
        var input = "SELECT col1 FROM table1; SELECT col2 FROM table2;";
        var expected = """
                       SELECT
                         col1
                       FROM
                         table1;

                       SELECT
                         col2
                       FROM
                         table2;
                       """;
        var result = _formatter.Format(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [Category("Sql")]
    public void Format_NegativeNumbers_RemovesSpaceBetweenMinusAndNumber()
    {
        var input = "SELECT col FROM table WHERE val = -5 AND val2 = - 10";
        var expected = """
                       SELECT
                         col
                       FROM
                         table
                       WHERE
                         val = -5
                         AND val2 = -10
                       """;
        var result = _formatter.Format(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [Category("Sql")]
    public void Format_MinusAsSubtraction_PreservesSpace()
    {
        var input = "SELECT col - 5 FROM table";
        var expected = """
                       SELECT
                         col - 5
                       FROM
                         table
                       """;
        var result = _formatter.Format(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [Category("Sql")]
    public void Format_MultipleBoundaries_RemovesIntermediarySpaces()
    {
        var input = "SELECT col FROM table WHERE a >= b AND c <= d";
        var expected = """
                       SELECT
                         col
                       FROM
                         table
                       WHERE
                         a >= b
                         AND c <= d
                       """;
        var result = _formatter.Format(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [Category("Sql")]
    public void Format_LimitClause_WithComma_ResetsNewlineOnlyForLimitNumbers()
    {
        // Limit clause with comma keeps numbers inline, but comma after non-numbers starts newline again
        var input = "SELECT col FROM table LIMIT 5, 10 WHERE col = 1";
        var expected = """
                       SELECT
                         col
                       FROM
                         table
                       LIMIT
                         5, 10
                       WHERE
                         col = 1
                       """;
        var result = _formatter.Format(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [Category("Sql")]
    public void Format_InlineParentheses_LongContentWithCommaAndWhitespace_HandlesInlineCountAndFormatting()
    {
        // Tests inlineCount >= 30 on comma inside inlineParentheses (when initial scan inlineParentheses is true, length <= 30 before open paren check or length <= 30 when scanned, but accumulated inlineCount >= 30 later)
        var input = "SELECT fn('arg1_arg2_arg3_arg4_arg5_30chars', 'second_arg') FROM table";
        var expected = """
                       SELECT
                         fn(
                           'arg1_arg2_arg3_arg4_arg5_30chars',
                           'second_arg'
                         )
                       FROM
                         table
                       """;
        var result = _formatter.Format(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [Category("Sql")]
    public void Format_UnmatchedClosingParentheses_MaintainsIndentLevelAtZero()
    {
        var input = "SELECT a FROM b WHERE a = 1 ) AND b = 2";
        var expected = """
                       SELECT
                         a
                       FROM
                         b
                       WHERE
                         a = 1)
                       AND b = 2
                       """;
        var result = _formatter.Format(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [Category("Sql")]
    public void Format_CaseWhenThenElseEnd_MultipleBranches_FormattedCorrectly()
    {
        var input = "SELECT CASE WHEN a = 1 THEN 'one' WHEN a = 2 THEN 'two' ELSE 'other' END FROM table";
        var expected = """
                       SELECT
                         CASE WHEN a = 1 THEN
                           'one'
                         WHEN a = 2 THEN
                           'two'
                         ELSE
                           'other'
                         END
                       FROM
                         table
                       """;
        var result = _formatter.Format(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [Category("Sql")]
    public void Format_NegativeNumbers_WithPrecedingWordOrQuoteOrNumber_PreservesMinusSpace()
    {
        // Tests the negative number space suppression checks (when preceding token IS Word/Quote/Number, minus is subtraction)
        var input = "SELECT col -5, 'str' -5, 10 -5 FROM table";
        var expected = """
                       SELECT
                         col - 5,
                         'str' - 5,
                         10 - 5
                       FROM
                         table
                       """;
        var result = _formatter.Format(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [Category("Sql")]
    public void Format_BlockComment_IndentsEveryLineOfComment()
    {
        var input = "SELECT col /* multi-line\n block comment\n line 3 */ FROM table";
        var expected = """
                       SELECT
                         col
                         /* multi-line
                          block comment
                          line 3 */
                       FROM
                         table
                       """;
        var result = _formatter.Format(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    private const string ExampleQueryA = """
                                         SELECT count(*),`Column1`,`Testing`, `Testing Three` FROM `Table1`
                                         WHERE Column1 = 'testing' AND ( (`Column2` = `Column3` OR Column4 >= NOW()) )
                                         GROUP BY Column1 ORDER BY Column3 DESC LIMIT 5,10
                                         """;

    private const string FormattedQueryA = """
                                           SELECT
                                             count(*),
                                             `Column1`,
                                             `Testing`,
                                             `Testing Three`
                                           FROM
                                             `Table1`
                                           WHERE
                                             Column1 = 'testing'
                                             AND (
                                               (
                                                 `Column2` = `Column3`
                                                 OR Column4 >= NOW()
                                               )
                                             )
                                           GROUP BY
                                             Column1
                                           ORDER BY
                                             Column3 DESC
                                           LIMIT
                                             5, 10
                                           """;

    private const string ExampleQueryB = """
                                         select id as Id, name as Name, state, postal_code as Zip, time_zone as Timezone
                                         from example.location where 1 = 1
                                         """;

    private const string FormattedQueryB = """
                                           SELECT
                                             id as Id,
                                             name as Name,
                                             state,
                                             postal_code as Zip,
                                             time_zone as Timezone
                                           FROM
                                             example.location
                                           WHERE
                                             1 = 1
                                           """;
}