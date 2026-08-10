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
        Assert.Pass();
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