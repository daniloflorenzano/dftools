using System.Text;
using System.Text.RegularExpressions;

namespace DfTools.Sql;

internal sealed class Tokenizer
{
    private readonly Regex _tokenizeRegex;

    public Tokenizer() => _tokenizeRegex = BuildTokenizeRegex();

    public Cursor Tokenize(string query)
    {
        var tokens = new List<Token>();
        var offset = 0;

        while (offset < query.Length)
        {
            var match = _tokenizeRegex.Match(query, offset);
            if (!match.Success || match.Index != offset)
                throw new InvalidOperationException("Unable to tokenize SQL query.");

            var type = TokenType.Whitespace;
            for (var i = 0; i < 12; i++)
            {
                if (!match.Groups[$"t_{i}"].Success)
                    continue;

                type = (TokenType)i;
                break;
            }

            tokens.Add(new Token(type, query.Substring(offset, match.Length)));
            offset += match.Length;
        }

        return new Cursor(tokens);
    }

    private static Regex BuildTokenizeRegex()
    {
        var regexBoundaries = MakeRegexFromList(Boundaries.ToList());
        var regexReserved = MakeRegexFromList(Reserved.ToList());
        var regexReservedTopLevel = MakeRegexFromList(ReservedTopLevel.ToList()).Replace(" ", @"\s+");
        var regexReservedNewline = MakeRegexFromList(ReservedNewline.ToList()).Replace(" ", @"\s+");
        var regexFunction = MakeRegexFromList(Functions.ToList());

        var boundaryCharClass = "[\"'`" + string.Concat(BoundaryChars.Select(c => EscapeCharClass(c.ToString()))) + "]";

        var regexVariable = $"[@:](?:[\\w.$]+|(?:{RegexBacktickQuote})|(?:{RegexQuote}))";
        var regexNumber = $"(?:\\d+(?:\\.\\d+)?|0x[\\da-fA-F]+|0b[01]+)(?=$|\\s|{boundaryCharClass})";
        var regexTopLevel = $"(?<!\\.|\\sCHARACTER\\s(?=SET\\s)){regexReservedTopLevel}(?=$|\\s|{boundaryCharClass})";
        var regexNewline = $"(?<!\\.){regexReservedNewline}(?=$|\\s|{boundaryCharClass})";
        var regexReservedWord = $"(?<!\\.){regexReserved}(?=$|\\s|{boundaryCharClass})|{regexFunction}(?=\\s*\\()";
        var regexWord = $".*?(?=$|\\s|{boundaryCharClass})";

        var parts = new List<string>
        {
            $"(?<t_{(int)TokenType.Whitespace}>{RegexWhitespace})",
            $"(?<t_{(int)TokenType.Comment}>{RegexComment})",
            $"(?<t_{(int)TokenType.BlockComment}>{RegexBlockComment})",
            $"(?<t_{(int)TokenType.BacktickQuote}>{RegexBacktickQuote})",
            $"(?<t_{(int)TokenType.Quote}>{RegexQuote})",
            $"(?<t_{(int)TokenType.Variable}>{regexVariable})",
            $"(?<t_{(int)TokenType.Number}>{regexNumber})",
            $"(?<t_{(int)TokenType.Boundary}>{regexBoundaries})",
            $"(?<t_{(int)TokenType.ReservedTopLevel}>{regexTopLevel})",
            $"(?<t_{(int)TokenType.ReservedNewline}>{regexNewline})",
            $"(?<t_{(int)TokenType.Reserved}>{regexReservedWord})",
            $"(?<t_{(int)TokenType.Word}>{regexWord})",
        };

        var pattern = "\\G(?:" + string.Join("|", parts) + ")";
        return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static string MakeRegexFromList(List<string> values, bool sorted = false)
    {
        if (!sorted)
            values.Sort(CompareRegexListValues);

        var groups = new List<(string Prefix, List<string> Items)>();
        string? prefix = null;
        var items = new List<string>();

        foreach (var value in values)
        {
            if (prefix is not null && !value.StartsWith(prefix[..1], StringComparison.Ordinal))
            {
                groups.Add((prefix, items));
                items = new List<string>();
                prefix = null;
            }

            items.Add(value);

            if (prefix is null)
            {
                prefix = value;
            }
            else
            {
                while (!value.StartsWith(prefix, StringComparison.Ordinal))
                    prefix = prefix[..^1];
            }
        }

        if (items.Count > 0)
            groups.Add((prefix ?? "", items));

        var sb = new StringBuilder();
        sb.Append("(?:");
        var first = true;
        foreach (var (groupPrefix, groupItems) in groups)
        {
            if (!first)
                sb.Append('|');
            first = false;

            sb.Append(PregQuote(groupPrefix));
            if (groupItems.Count == 1)
            {
                sb.Append(PregQuote(groupItems[0][groupPrefix.Length..]));
            }
            else
            {
                sb.Append(MakeRegexFromList(groupItems.Select(v => v[groupPrefix.Length..]).ToList(), true));
            }
        }

        sb.Append(')');
        return sb.ToString();
    }

    private static int CompareRegexListValues(string a, string b)
    {
        var prefixA = a.StartsWith(b, StringComparison.Ordinal);
        var prefixB = b.StartsWith(a, StringComparison.Ordinal);
        return prefixA || prefixB
            ? b.Length.CompareTo(a.Length)
            : string.CompareOrdinal(a, b);
    }

    private static string PregQuote(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (c is '.' or '\\' or '+' or '*' or '?' or '[' or '^' or ']' or '$' or '(' or ')' or '{' or '}' or '=' or '!' or '<' or '>' or '|' or ':' or '-' or '#' or '/')
                sb.Append('\\');
            sb.Append(c);
        }

        return sb.ToString();
    }

    private static string EscapeCharClass(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (c is '\\' or ']' or '-' or '^' or '[')
                sb.Append('\\');
            sb.Append(c);
        }

        return sb.ToString();
    }

    private const string RegexWhitespace = """\s+""";

    private const string RegexComment = """(?:--|#(?!>))[^\n]*""";

    private const string RegexBlockComment = """/\*(?:[^*]+|\*(?!/))*(?:\*|$)(?:/|$)""";

    private const string RegexBacktickQuote = """`(?:[^`]+|`(?:`|$))*(?:`|$)|\[(?:[^\]]+|\](?:\]|$))*(?:\]|$)""";

    private const string RegexQuote = """'(?:[^'\\]+|\\(?:[\s\S]|$)|'(?:'|$))*(?:'|$)|"(?:[^"\\]+|\\(?:[\s\S]|$)|"(?:"|$))*(?:"|$)""";

    private static readonly char[] BoundaryChars =
    {
        ',', ';', ':', ')', '(', '.', '=', '<', '>', '+', '-', '~', '*', '/', '!', '^', '%', '|', '&', '#',
    };

    private static readonly string[] Boundaries =
    {
        ",", ";", "::", ":", ")", "(", ".", "=", "<", ">", "+", "-", "~*", "*", "/", "!", "^", "%", "|", "&", "#",
    };

    private static readonly string[] Reserved =
    {
        "ACCESSIBLE", "ACTION", "ADD", "AFTER", "AGAINST", "AGGREGATE", "ALGORITHM", "ALL", "ALTER",
        "ANALYSE", "ANALYZE", "AND", "AS", "ASC", "AUTOCOMMIT", "AUTO_INCREMENT", "BACKUP", "BEGIN",
        "BETWEEN", "BIGINT", "BINARY", "BINLOG", "BLOB", "BOTH", "BY", "CASCADE", "CASE", "CHANGE",
        "CHANGED", "CHAR", "CHARACTER", "CHARSET", "CHECK", "CHECKSUM", "COLLATE", "COLLATION", "COLUMN",
        "COLUMNS", "COMMENT", "COMMIT", "COMMITTED", "COMPRESSED", "CONCURRENT", "CONSTRAINT", "CONTAINS",
        "CONVERT", "CREATE", "CROSS", "CURRENT", "CURRENT_TIMESTAMP", "DATABASE", "DATABASES", "DAY",
        "DAY_HOUR", "DAY_MINUTE", "DAY_SECOND", "DECIMAL", "DEFAULT", "DEFINER", "DELAYED", "DELETE",
        "DESC", "DESCRIBE", "DETERMINISTIC", "DISTINCT", "DISTINCTROW", "DIV", "DO", "DOUBLE", "DROP",
        "DUMPFILE", "DUPLICATE", "DYNAMIC", "ELSE", "ENCLOSED", "END", "ENGINE", "ENGINES", "ENGINE_TYPE",
        "ESCAPE", "ESCAPED", "EVENTS", "EXCEPT", "EXCLUDE", "EXEC", "EXECUTE", "EXISTS", "EXPLAIN",
        "EXTENDED", "FALSE", "FAST", "FETCH", "FIELDS", "FILE", "FILTER", "FIRST", "FIXED", "FLOAT",
        "FLOAT4", "FLOAT8", "FLUSH", "FOLLOWING", "FOR", "FORCE", "FOREIGN", "FROM", "FULL", "FULLTEXT",
        "FUNCTION", "GLOBAL", "GRANT", "GRANTS", "GROUP", "GROUPS", "HAVING", "HEAP", "HIGH_PRIORITY",
        "HOSTS", "HOUR", "HOUR_MINUTE", "HOUR_SECOND", "IDENTIFIED", "IF", "IFNULL", "IGNORE", "IN",
        "INDEX", "INDEXES", "INFILE", "INNER", "INSERT", "INSERT_ID", "INSERT_METHOD", "INT", "INT1",
        "INT2", "INT3", "INT4", "INT8", "INTEGER", "INTERSECT", "INTERVAL", "INTO", "INVOKER", "IS",
        "ISOLATION", "JOIN", "KEY", "KEYS", "KILL", "LAST_INSERT_ID", "LEADING", "LEFT", "LEVEL", "LIKE",
        "LIMIT", "LINEAR", "LINES", "LOAD", "LOCAL", "LOCK", "LOCKS", "LOGS", "LONG", "LONGBLOB",
        "LONGTEXT", "LOW_PRIORITY", "MARIA", "MASTER", "MASTER_CONNECT_RETRY", "MASTER_HOST",
        "MASTER_LOG_FILE", "MATCH", "MAX_CONNECTIONS_PER_HOUR", "MAX_QUERIES_PER_HOUR", "MAX_ROWS",
        "MAX_UPDATES_PER_HOUR", "MAX_USER_CONNECTIONS", "MEDIUM", "MEDIUMBLOB", "MEDIUMINT", "MEDIUMTEXT",
        "MERGE", "MINUTE", "MINUTE_SECOND", "MIN_ROWS", "MODE", "MODIFY", "MONTH", "MRG_MYISAM", "MYISAM",
        "NAMES", "NATURAL", "NOT", "NULL", "NUMERIC", "OFFSET", "ON", "OPEN", "OPTIMIZE", "OPTION",
        "OPTIONALLY", "OR", "ORDER", "OUTER", "OUTFILE", "OVER", "PACK_KEYS", "PAGE", "PARTIAL",
        "PARTITION", "PARTITIONS", "PASSWORD", "PRECEDING", "PRIMARY", "PRIVILEGES", "PROCEDURE",
        "PROCESS", "PROCESSLIST", "PURGE", "QUICK", "RAID0", "RAID_CHUNKS", "RAID_CHUNKSIZE", "RAID_TYPE",
        "RANGE", "READ", "READ_ONLY", "READ_WRITE", "REAL", "RECURSIVE", "REFERENCES", "REGEXP", "RELOAD",
        "RENAME", "REPAIR", "REPEATABLE", "REPLACE", "REPLICATION", "RESET", "RESTORE", "RESTRICT",
        "RETURN", "RETURNS", "REVOKE", "RIGHT", "RLIKE", "ROLLBACK", "ROW", "ROWS", "ROW_FORMAT",
        "SECOND", "SECURITY", "SELECT", "SEPARATOR", "SERIALIZABLE", "SESSION", "SET", "SHARE", "SHOW",
        "SHUTDOWN", "SLAVE", "SMALLINT", "SONAME", "SOUNDS", "SQL", "SQL_AUTO_IS_NULL", "SQL_BIG_RESULT",
        "SQL_BIG_SELECTS", "SQL_BIG_TABLES", "SQL_BUFFER_RESULT", "SQL_CACHE", "SQL_CALC_FOUND_ROWS",
        "SQL_LOG_BIN", "SQL_LOG_OFF", "SQL_LOG_UPDATE", "SQL_LOW_PRIORITY_UPDATES", "SQL_NO_CACHE",
        "SQL_QUOTE_SHOW_CREATE", "SQL_SAFE_UPDATES", "SQL_SELECT_LIMIT", "SQL_SLAVE_SKIP_COUNTER",
        "SQL_SMALL_RESULT", "SQL_WARNINGS", "START", "STARTING", "STATUS", "STOP", "STORAGE",
        "STRAIGHT_JOIN", "STRING", "STRIPED", "SUPER", "TABLE", "TABLES", "TEMPORARY", "TERMINATED",
        "THEN", "TIES", "TINYBLOB", "TINYINT", "TINYTEXT", "TO", "TRAILING", "TRANSACTIONAL", "TRUE",
        "TRUNCATE", "TYPE", "TYPES", "UNBOUNDED", "UNCOMMITTED", "UNION", "UNIQUE", "UNLOCK", "UNSIGNED",
        "UPDATE", "USAGE", "USE", "USING", "VALUES", "VARBINARY", "VARCHAR", "VARCHARACTER", "VARIABLES",
        "VIEW", "WHEN", "WHERE", "WINDOW", "WITH", "WORK", "WRITE", "XOR", "YEAR_MONTH",
    };

    private static readonly string[] ReservedTopLevel =
    {
        "ADD", "ALTER TABLE", "CHANGE", "DELETE FROM", "DROP", "EXCEPT", "FETCH", "FROM", "GROUP BY",
        "GROUPS", "HAVING", "INTERSECT", "LIMIT", "MODIFY", "OFFSET", "ORDER BY", "PARTITION BY", "RANGE",
        "ROWS", "SELECT", "SET", "UNION", "UNION ALL", "UPDATE", "VALUES", "WHERE", "WINDOW", "WITH",
    };

    private static readonly string[] ReservedNewline =
    {
        "AND", "EXCLUDE", "INNER JOIN", "JOIN", "LEFT JOIN", "LEFT OUTER JOIN", "OR", "OUTER JOIN",
        "RIGHT JOIN", "RIGHT OUTER JOIN", "STRAIGHT_JOIN", "XOR",
    };

    private static readonly string[] Functions =
    {
        "ABS", "ACOS", "ADDDATE", "ADDTIME", "AES_DECRYPT", "AES_ENCRYPT", "APPROX_COUNT_DISTINCT",
        "AREA", "ASBINARY", "ASCII", "ASIN", "ASTEXT", "ATAN", "ATAN2", "AVG", "BDMPOLYFROMTEXT",
        "BDMPOLYFROMWKB", "BDPOLYFROMTEXT", "BDPOLYFROMWKB", "BENCHMARK", "BIN", "BIT_AND", "BIT_COUNT",
        "BIT_LENGTH", "BIT_OR", "BIT_XOR", "BOUNDARY", "BUFFER", "CAST", "CEIL", "CEILING", "CENTROID",
        "CHARACTER_LENGTH", "CHAR_LENGTH", "CHECKSUM_AGG", "COALESCE", "COERCIBILITY", "COMPRESS",
        "CONCAT", "CONCAT_WS", "CONNECTION_ID", "CONV", "CONVERT_TZ", "CONVEXHULL", "COS", "COT", "COUNT",
        "COUNT_BIG", "CRC32", "CROSSES", "CUME_DIST", "CURDATE", "CURRENT_DATE", "CURRENT_TIME",
        "CURRENT_USER", "CURTIME", "DATE", "DATEDIFF", "DATE_ADD", "DATE_DIFF", "DATE_FORMAT", "DATE_SUB",
        "DAYNAME", "DAYOFMONTH", "DAYOFWEEK", "DAYOFYEAR", "DECODE", "DEGREES", "DENSE_RANK",
        "DES_DECRYPT", "DES_ENCRYPT", "DIFFERENCE", "DIMENSION", "DISJOINT", "DISTANCE", "ELT", "ENCODE",
        "ENCRYPT", "ENDPOINT", "ENVELOPE", "EQUALS", "EXP", "EXPORT_SET", "EXTERIORRING", "EXTRACT",
        "EXTRACTVALUE", "FIELD", "FIND_IN_SET", "FIRST_VALUE", "FLOOR", "FORMAT", "FOUND_ROWS",
        "FROM_DAYS", "FROM_UNIXTIME", "GEOMCOLLFROMTEXT", "GEOMCOLLFROMWKB", "GEOMETRYCOLLECTION",
        "GEOMETRYCOLLECTIONFROMTEXT", "GEOMETRYCOLLECTIONFROMWKB", "GEOMETRYFROMTEXT", "GEOMETRYFROMWKB",
        "GEOMETRYN", "GEOMETRYTYPE", "GEOMFROMTEXT", "GEOMFROMWKB", "GET_FORMAT", "GET_LOCK", "GLENGTH",
        "GREATEST", "GROUPING", "GROUPING_ID", "GROUP_CONCAT", "GROUP_UNIQUE_USERS", "HEX", "INET_ATON",
        "INET_NTOA", "INSTR", "INTERIORRINGN", "INTERSECTION", "INTERSECTS", "ISCLOSED", "ISEMPTY",
        "ISNULL", "ISRING", "ISSIMPLE", "IS_FREE_LOCK", "IS_USED_LOCK", "LAG", "LAST_DAY", "LAST_VALUE",
        "LCASE", "LEAD", "LEAST", "LENGTH", "LINEFROMTEXT", "LINEFROMWKB", "LINESTRING",
        "LINESTRINGFROMTEXT", "LINESTRINGFROMWKB", "LISTAGG", "LN", "LOAD_FILE", "LOCALTIME",
        "LOCALTIMESTAMP", "LOCATE", "LOG", "LOG10", "LOG2", "LOWER", "LPAD", "LTRIM", "MAKEDATE",
        "MAKETIME", "MAKE_SET", "MASTER_POS_WAIT", "MAX", "MBRCONTAINS", "MBRDISJOINT", "MBREQUAL",
        "MBRINTERSECTS", "MBROVERLAPS", "MBRTOUCHES", "MBRWITHIN", "MD5", "MICROSECOND", "MID", "MIN",
        "MLINEFROMTEXT", "MLINEFROMWKB", "MOD", "MONTHNAME", "MPOINTFROMTEXT", "MPOINTFROMWKB",
        "MPOLYFROMTEXT", "MPOLYFROMWKB", "MULTILINESTRING", "MULTILINESTRINGFROMTEXT",
        "MULTILINESTRINGFROMWKB", "MULTIPOINT", "MULTIPOINTFROMTEXT", "MULTIPOINTFROMWKB", "MULTIPOLYGON",
        "MULTIPOLYGONFROMTEXT", "MULTIPOLYGONFROMWKB", "NAME_CONST", "NOW", "NTH_VALUE", "NTILE",
        "NULLIF", "NUMGEOMETRIES", "NUMINTERIORRINGS", "NUMPOINTS", "OCT", "OCTET_LENGTH", "OLD_PASSWORD",
        "ORD", "OVERLAPS", "PERCENTILE_CONT", "PERCENTILE_DISC", "PERCENT_RANK", "PERIOD_ADD",
        "PERIOD_DIFF", "PI", "POINT", "POINTFROMTEXT", "POINTFROMWKB", "POINTN", "POINTONSURFACE",
        "POLYFROMTEXT", "POLYFROMWKB", "POLYGON", "POLYGONFROMTEXT", "POLYGONFROMWKB", "POSITION", "POW",
        "POWER", "QUARTER", "QUOTE", "RADIANS", "RAND", "RANK", "RELATED", "RELEASE_LOCK", "REPEAT",
        "REVERSE", "ROUND", "ROW_COUNT", "ROW_NUMBER", "RPAD", "RTRIM", "SCHEMA", "SEC_TO_TIME",
        "SESSION_USER", "SHA", "SHA1", "SIGN", "SIN", "SLEEP", "SOUNDEX", "SPACE", "SQRT", "SRID",
        "STARTPOINT", "STD", "STDDEV", "STDDEV_POP", "STDDEV_SAMP", "STDEV", "STDEVP", "STRCMP",
        "STRING_AGG", "STR_TO_DATE", "SUBDATE", "SUBSTR", "SUBSTRING", "SUBSTRING_INDEX", "SUBTIME",
        "SUM", "SYMDIFFERENCE", "SYSDATE", "SYSTEM_USER", "TAN", "TIME", "TIMEDIFF", "TIMESTAMP",
        "TIMESTAMPADD", "TIMESTAMPDIFF", "TIME_FORMAT", "TIME_TO_SEC", "TOUCHES", "TO_DAYS", "TRIM",
        "UCASE", "UNCOMPRESS", "UNCOMPRESSED_LENGTH", "UNHEX", "UNIQUE_USERS", "UNIX_TIMESTAMP",
        "UPDATEXML", "UPPER", "USER", "UTC_DATE", "UTC_TIME", "UTC_TIMESTAMP", "UUID", "VAR", "VARIANCE",
        "VARP", "VAR_POP", "VAR_SAMP", "VERSION", "WEEK", "WEEKDAY", "WEEKOFYEAR", "WITHIN", "X", "Y",
        "YEAR", "YEARWEEK",
    };
}
