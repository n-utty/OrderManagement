using Microsoft.Data.Sqlite;

namespace OrderManagement.Api.Configuration;

public static class SqliteConnectionString
{
    // A relative database path resolves against the process's working directory, which differs
    // between Kestrel and IIS Express. Anchoring it to the content root keeps one database file
    // in the project folder however the app is launched. In-memory databases are left untouched.
    public static string ResolveRelativeTo(string connectionString, string contentRootPath)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);

        var isFile = builder.Mode != SqliteOpenMode.Memory
            && !string.Equals(builder.DataSource, ":memory:", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrEmpty(builder.DataSource);

        if (isFile && !Path.IsPathRooted(builder.DataSource))
        {
            builder.DataSource = Path.Combine(contentRootPath, builder.DataSource);
        }

        return builder.ToString();
    }
}
