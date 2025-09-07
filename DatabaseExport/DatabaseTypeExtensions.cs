namespace SimpleDataEngine.DatabaseExport
{
    /// <summary>
    /// Database type extensions and utilities
    /// </summary>
    public static class DatabaseTypeExtensions
    {
        /// <summary>
        /// Gets the connection string template for a database type
        /// </summary>
        /// <param name="dbType">Database type</param>
        /// <returns>Connection string template</returns>
        public static string GetConnectionStringTemplate(this DatabaseType dbType)
        {
            return dbType switch
            {
                DatabaseType.SQLite => "Data Source={0};Version=3;",
                DatabaseType.SQLServer => "Server={0};Database={1};Trusted_Connection=true;",
                DatabaseType.MySQL => "Server={0};Database={1};Uid={2};Pwd={3};",
                DatabaseType.PostgreSQL => "Host={0};Database={1};Username={2};Password={3};",
                DatabaseType.Oracle => "Data Source={0};User Id={1};Password={2};",
                DatabaseType.MariaDB => "Server={0};Database={1};Uid={2};Pwd={3};",
                DatabaseType.Access => "Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};",
                _ => throw new NotSupportedException($"Database type {dbType} is not supported")
            };
        }

        /// <summary>
        /// Gets the SQL syntax helper for a database type
        /// </summary>
        /// <param name="dbType">Database type</param>
        /// <returns>SQL syntax helper</returns>
        public static SqlSyntax GetSqlSyntax(this DatabaseType dbType)
        {
            return dbType switch
            {
                DatabaseType.SQLite => new SqliteSyntax(),
                DatabaseType.SQLServer => new SqlServerSyntax(),
                DatabaseType.MySQL => new MySqlSyntax(),
                DatabaseType.PostgreSQL => new PostgreSqlSyntax(),
                DatabaseType.Oracle => new OracleSyntax(),
                DatabaseType.MariaDB => new MySqlSyntax(), // Similar to MySQL
                DatabaseType.Access => new AccessSyntax(),
                _ => throw new NotSupportedException($"Database type {dbType} is not supported")
            };
        }

        /// <summary>
        /// Gets the default port for a database type
        /// </summary>
        /// <param name="dbType">Database type</param>
        /// <returns>Default port number</returns>
        public static int GetDefaultPort(this DatabaseType dbType)
        {
            return dbType switch
            {
                DatabaseType.SQLServer => 1433,
                DatabaseType.MySQL => 3306,
                DatabaseType.PostgreSQL => 5432,
                DatabaseType.Oracle => 1521,
                DatabaseType.MariaDB => 3306,
                _ => 0 // File-based databases don't use ports
            };
        }

        /// <summary>
        /// Checks if the database type supports bulk operations
        /// </summary>
        /// <param name="dbType">Database type</param>
        /// <returns>True if bulk operations are supported</returns>
        public static bool SupportsBulkOperations(this DatabaseType dbType)
        {
            return dbType switch
            {
                DatabaseType.SQLServer => true,
                DatabaseType.MySQL => true,
                DatabaseType.PostgreSQL => true,
                DatabaseType.Oracle => true,
                DatabaseType.MariaDB => true,
                DatabaseType.SQLite => false,
                DatabaseType.Access => false,
                _ => false
            };
        }
    }
}