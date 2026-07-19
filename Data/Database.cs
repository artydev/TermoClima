using System.Collections.Concurrent;
using System.Text.Json;

namespace JsonServer.Data
{
    public class Database
    {
        public ConcurrentDictionary<string, List<Dictionary<string, object>>> Tables { get; set; }

        private readonly string FilePath;

        private readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

        // The underlying List<T> per table isn't thread-safe on its own (only the outer
        // ConcurrentDictionary is) — this lock keeps Insert/Update/Delete atomic relative to
        // each other so two near-simultaneous requests can't corrupt a table or hand out the
        // same auto-generated id. Good enough for the low-concurrency use this is meant for;
        // it is not a substitute for a real database under real write load.
        private readonly object SyncRoot = new();

        public Database(string filePath)
        {
            FilePath = filePath;
            Tables = [];

            Seed();
        }

        public IEnumerable<string> TableNames => Tables.Keys;

        private static string? GetRowId(Dictionary<string, object> row) =>
            row.TryGetValue("id", out var value) && value is not null ? value.ToString() : null;

        public Dictionary<string, object>? GetById(string id, string tableName)
        {
            if (!Tables.TryGetValue(tableName, out var rows))
            {
                return null;
            }

            return rows.FirstOrDefault(x => GetRowId(x) == id);
        }

        public Dictionary<string, object>? Insert(JsonElement element, string tableName)
        {
            if (!Tables.TryGetValue(tableName, out var rows))
            {
                return null;
            }

            var row = JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());

            if (row == null)
            {
                return null;
            }

            lock (SyncRoot)
            {
                // mockapi.io / json-server both auto-assign an id when the client doesn't
                // supply one. Without this, a row could end up with no "id" key at all, which
                // would then throw the next time anything tried to look it up by id.
                var suppliedId = GetRowId(row);
                if (string.IsNullOrWhiteSpace(suppliedId))
                {
                    row["id"] = GenerateNextId(rows);
                }

                rows.Add(row);
            }

            return row;
        }

        public Dictionary<string, object>? Update(string id, JsonElement element, string tableName)
        {
            if (!Tables.TryGetValue(tableName, out var rows))
            {
                return null;
            }

            var row = JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());

            if (row == null)
            {
                return null;
            }

            lock (SyncRoot)
            {
                var index = rows.FindIndex(x => GetRowId(x) == id);

                if (index == -1)
                {
                    return null;
                }

                // The id in the URL is authoritative for PUT, never the body — otherwise a
                // request body that omits "id" (or sends a different one) would silently
                // orphan or renumber the row instead of replacing the item you asked for.
                row["id"] = id;
                rows[index] = row;

                return row;
            }
        }

        public bool Delete(string id, string tableName)
        {
            if (!Tables.TryGetValue(tableName, out var rows))
            {
                return false;
            }

            lock (SyncRoot)
            {
                var row = rows.FirstOrDefault(x => GetRowId(x) == id);

                if (row == null)
                {
                    return false;
                }

                return rows.Remove(row);
            }
        }

        public async Task SaveChangesAsync()
        {
            try
            {
                string jsonString;
                lock (SyncRoot)
                {
                    jsonString = JsonSerializer.Serialize(Tables, SerializerOptions);
                }

                await File.WriteAllTextAsync(FilePath, jsonString);
            }
            catch (Exception)
            {
                // 
            }
        }

        // Generates the next sequential numeric-string id for a table, e.g. "11" after "10" —
        // matching the id style mockapi.io/json-server use. Falls back to "1" for an empty
        // table or one whose existing ids aren't purely numeric.
        private static string GenerateNextId(List<Dictionary<string, object>> rows)
        {
            var maxId = 0;

            foreach (var row in rows)
            {
                var idText = GetRowId(row);
                if (idText != null && int.TryParse(idText, out var idNum) && idNum > maxId)
                {
                    maxId = idNum;
                }
            }

            return (maxId + 1).ToString();
        }

        private void Seed()
        {
            if (!File.Exists(FilePath))
            {
                // Missing entirely — most likely a fresh deployment where db.json is
                // deliberately excluded from future publishes (see the .csproj publish
                // rules) to protect the live server's data, but was never uploaded once
                // manually. Start with the tables the app actually depends on rather
                // than crashing the whole site on startup, and write the file out so
                // it exists for SaveChangesAsync() and future restarts.
                Tables.TryAdd("products", []);
                Tables.TryAdd("orders", []);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(Tables, SerializerOptions));
                return;
            }

            using var stream = File.OpenRead(FilePath);
            using var doc = JsonDocument.Parse(stream);

            foreach (var table in doc.RootElement.EnumerateObject())
            {
                var rows = new List<Dictionary<string, object>>();

                if (table.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in table.Value.EnumerateArray())
                    {
                        var row = JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());

                        if (row != null)
                        {
                            rows.Add(row);
                        }
                    }

                    Tables.TryAdd(table.Name, rows);
                }
            }
        }
    }
}
