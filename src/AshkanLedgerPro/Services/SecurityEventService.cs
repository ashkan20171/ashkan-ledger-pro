using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace AshkanLedgerPro.Services;

/// <summary>Best-effort event journal in a separate SQLite database, so rejected
/// financial transactions do not roll back their diagnostic event.</summary>
public sealed class SecurityEventService
{
    public sealed record SecurityEvent(long Id, DateTime OccurredUtc, string Actor,
        string EventType, string Context, string Message);

    private static string ConnectionString
    {
        get
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AshkanLedgerPro");
            Directory.CreateDirectory(dir);
            return new SqliteConnectionStringBuilder { DataSource = Path.Combine(dir, "security-events.db"), Mode = SqliteOpenMode.ReadWriteCreate }.ToString();
        }
    }

    private static void EnsureSchema(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS SecurityEvents (
              Id INTEGER PRIMARY KEY AUTOINCREMENT,
              OccurredUtc TEXT NOT NULL,
              Actor TEXT NOT NULL,
              EventType TEXT NOT NULL,
              Context TEXT NOT NULL,
              Message TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_SecurityEvents_OccurredUtc ON SecurityEvents(OccurredUtc DESC);
            """;
        command.ExecuteNonQuery();
    }

    public void Record(string actor, string eventType, string context, string message)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        EnsureSchema(connection);
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO SecurityEvents (OccurredUtc,Actor,EventType,Context,Message) VALUES ($utc,$actor,$type,$context,$message)";
        command.Parameters.AddWithValue("$utc", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$actor", actor.Length > 100 ? actor[..100] : actor);
        command.Parameters.AddWithValue("$type", eventType.Length > 80 ? eventType[..80] : eventType);
        command.Parameters.AddWithValue("$context", context.Length > 200 ? context[..200] : context);
        command.Parameters.AddWithValue("$message", message.Length > 2000 ? message[..2000] : message);
        command.ExecuteNonQuery();
    }

    /// <summary>Parameterized server-side filtering; never interpolates user input into SQL.</summary>
    public List<SecurityEvent> Search(string? actor = null, string? eventType = null,
        DateTime? fromUtc = null, DateTime? toUtc = null, int limit = 500)
    {
        limit = Math.Clamp(limit, 1, 5000);
        if (fromUtc.HasValue && toUtc.HasValue && fromUtc > toUtc)
            throw new ArgumentException("Start time must not exceed end time.");
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        EnsureSchema(connection);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id,OccurredUtc,Actor,EventType,Context,Message FROM SecurityEvents WHERE 1=1";
        if (!string.IsNullOrWhiteSpace(actor))
        {
            command.CommandText += " AND instr(lower(Actor), lower($actor)) > 0";
            command.Parameters.AddWithValue("$actor", actor.Trim());
        }
        if (!string.IsNullOrWhiteSpace(eventType))
        {
            command.CommandText += " AND EventType = $type";
            command.Parameters.AddWithValue("$type", eventType.Trim());
        }
        if (fromUtc.HasValue)
        {
            command.CommandText += " AND OccurredUtc >= $from";
            command.Parameters.AddWithValue("$from", fromUtc.Value.ToUniversalTime().ToString("O"));
        }
        if (toUtc.HasValue)
        {
            command.CommandText += " AND OccurredUtc <= $to";
            command.Parameters.AddWithValue("$to", toUtc.Value.ToUniversalTime().ToString("O"));
        }
        command.CommandText += " ORDER BY Id DESC LIMIT $limit";
        command.Parameters.AddWithValue("$limit", limit);
        using var reader = command.ExecuteReader();
        var events = new List<SecurityEvent>();
        while (reader.Read())
            events.Add(new SecurityEvent(reader.GetInt64(0), DateTime.Parse(reader.GetString(1), null,
                System.Globalization.DateTimeStyles.RoundtripKind), reader.GetString(2), reader.GetString(3),
                reader.GetString(4), reader.GetString(5)));
        return events;
    }

    public List<SecurityEvent> Recent(int limit = 200)
    {
        limit = Math.Clamp(limit, 1, 500);
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        EnsureSchema(connection);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id,OccurredUtc,Actor,EventType,Context,Message FROM SecurityEvents ORDER BY Id DESC LIMIT $limit";
        command.Parameters.AddWithValue("$limit", limit);
        using var reader = command.ExecuteReader();
        var events = new List<SecurityEvent>();
        while (reader.Read())
            events.Add(new SecurityEvent(reader.GetInt64(0), DateTime.Parse(reader.GetString(1), null,
                System.Globalization.DateTimeStyles.RoundtripKind), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetString(5)));
        return events;
    }
}
