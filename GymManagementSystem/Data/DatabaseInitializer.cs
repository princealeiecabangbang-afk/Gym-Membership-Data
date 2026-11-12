using Microsoft.Data.Sqlite;

namespace GymManagementSystem.Data;

public static class DatabaseInitializer
{
    private static readonly string[] RequiredTables =
    [
        "membership_plans",
        "members",
        "attendance",
        "payments"
    ];

    public static async Task InitializeAsync(string connectionString, string scriptPath)
    {
        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException("The SQLite schema script was not found.", scriptPath);
        }

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA foreign_keys;";
            var foreignKeysEnabled = Convert.ToInt32(await command.ExecuteScalarAsync());
            if (foreignKeysEnabled != 1)
            {
                throw new InvalidOperationException("SQLite foreign-key enforcement is not enabled.");
            }
        }

        var existingTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                existingTables.Add(reader.GetString(0));
            }
        }

        if (existingTables.Count == 0)
        {
            var schemaScript = await File.ReadAllTextAsync(scriptPath);
            await using var transaction = await connection.BeginTransactionAsync();
            await using var command = connection.CreateCommand();
            command.Transaction = (SqliteTransaction)transaction;
            command.CommandText = schemaScript;
            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
            return;
        }

        var missingTables = RequiredTables.Where(table => !existingTables.Contains(table)).ToArray();
        if (missingTables.Length > 0)
        {
            throw new InvalidOperationException(
                $"The SQLite database already contains tables but is missing required tables: {string.Join(", ", missingTables)}.");
        }
    }
}
