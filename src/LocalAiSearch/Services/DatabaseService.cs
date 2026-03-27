using Microsoft.Data.Sqlite;
using LocalAiSearch.Models;

namespace LocalAiSearch.Services;

/// <summary>
/// Manages SQLite database operations for media items.
/// </summary>
public class DatabaseService
{
    private readonly string _connectionString;
    private readonly SqliteConnection? _sharedConnection;

    public DatabaseService(string dbPath = "./local.db")
    {
        _connectionString = $"Data Source={dbPath}";
    }

    internal DatabaseService(SqliteConnection sharedConnection)
    {
        _sharedConnection = sharedConnection;
        _connectionString = sharedConnection.ConnectionString;
    }

    private SqliteConnection GetConnection()
    {
        if (_sharedConnection != null)
        {
            return _sharedConnection;
        }
        return new SqliteConnection(_connectionString);
    }

    private bool ShouldDisposeConnection() => _sharedConnection == null;

    public async Task InitializeAsync()
    {
        var connection = GetConnection();
        var shouldDispose = ShouldDisposeConnection();
        
        try
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS media_items (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    file_path TEXT NOT NULL UNIQUE,
                    file_hash TEXT NOT NULL,
                    description TEXT NOT NULL DEFAULT '',
                    tags TEXT NOT NULL DEFAULT '',
                    media_type TEXT NOT NULL DEFAULT 'image',
                    file_size_bytes INTEGER NOT NULL DEFAULT 0,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    is_tagged INTEGER NOT NULL DEFAULT 0
                );
                CREATE INDEX IF NOT EXISTS idx_file_hash ON media_items(file_hash);
                CREATE INDEX IF NOT EXISTS idx_tags ON media_items(tags);
            ";
            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            if (shouldDispose)
            {
                connection.Dispose();
            }
        }
    }

    public async Task<List<MediaItem>> GetAllAsync()
    {
        var items = new List<MediaItem>();
        var connection = GetConnection();
        var shouldDispose = ShouldDisposeConnection();

        try
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM media_items ORDER BY created_at DESC";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(ReadMediaItem(reader));
            }
        }
        finally
        {
            if (shouldDispose)
            {
                connection.Dispose();
            }
        }

        return items;
    }

    public async Task<MediaItem?> GetByIdAsync(int id)
    {
        var connection = GetConnection();
        var shouldDispose = ShouldDisposeConnection();

        try
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM media_items WHERE id = $id";
            command.Parameters.AddWithValue("$id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return ReadMediaItem(reader);
            }
        }
        finally
        {
            if (shouldDispose)
            {
                connection.Dispose();
            }
        }

        return null;
    }

    public async Task<MediaItem?> GetByFilePathAsync(string filePath)
    {
        var connection = GetConnection();
        var shouldDispose = ShouldDisposeConnection();

        try
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM media_items WHERE file_path = $filePath";
            command.Parameters.AddWithValue("$filePath", filePath);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return ReadMediaItem(reader);
            }
        }
        finally
        {
            if (shouldDispose)
            {
                connection.Dispose();
            }
        }

        return null;
    }

    public async Task<MediaItem?> GetByHashAsync(string hash)
    {
        var connection = GetConnection();
        var shouldDispose = ShouldDisposeConnection();

        try
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM media_items WHERE file_hash = $hash";
            command.Parameters.AddWithValue("$hash", hash);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return ReadMediaItem(reader);
            }
        }
        finally
        {
            if (shouldDispose)
            {
                connection.Dispose();
            }
        }

        return null;
    }

    public async Task<int> InsertAsync(MediaItem item)
    {
        var connection = GetConnection();
        var shouldDispose = ShouldDisposeConnection();

        try
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO media_items (file_path, file_hash, description, tags, media_type, file_size_bytes, created_at, updated_at, is_tagged)
                VALUES ($filePath, $fileHash, $description, $tags, $mediaType, $fileSizeBytes, $createdAt, $updatedAt, $isTagged)
                RETURNING id
            ";
            command.Parameters.AddWithValue("$filePath", item.FilePath);
            command.Parameters.AddWithValue("$fileHash", item.FileHash);
            command.Parameters.AddWithValue("$description", item.Description);
            command.Parameters.AddWithValue("$tags", item.Tags);
            command.Parameters.AddWithValue("$mediaType", item.MediaType);
            command.Parameters.AddWithValue("$fileSizeBytes", item.FileSizeBytes);
            command.Parameters.AddWithValue("$createdAt", item.CreatedAt.ToString("O"));
            command.Parameters.AddWithValue("$updatedAt", item.UpdatedAt.ToString("O"));
            command.Parameters.AddWithValue("$isTagged", item.IsTagged ? 1 : 0);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        finally
        {
            if (shouldDispose)
            {
                connection.Dispose();
            }
        }
    }

    public async Task UpdateAsync(MediaItem item)
    {
        var connection = GetConnection();
        var shouldDispose = ShouldDisposeConnection();

        try
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE media_items 
                SET file_path = $filePath,
                    file_hash = $fileHash,
                    description = $description,
                    tags = $tags,
                    media_type = $mediaType,
                    file_size_bytes = $fileSizeBytes,
                    updated_at = $updatedAt,
                    is_tagged = $isTagged
                WHERE id = $id
            ";
            command.Parameters.AddWithValue("$id", item.Id);
            command.Parameters.AddWithValue("$filePath", item.FilePath);
            command.Parameters.AddWithValue("$fileHash", item.FileHash);
            command.Parameters.AddWithValue("$description", item.Description);
            command.Parameters.AddWithValue("$tags", item.Tags);
            command.Parameters.AddWithValue("$mediaType", item.MediaType);
            command.Parameters.AddWithValue("$fileSizeBytes", item.FileSizeBytes);
            command.Parameters.AddWithValue("$updatedAt", item.UpdatedAt.ToString("O"));
            command.Parameters.AddWithValue("$isTagged", item.IsTagged ? 1 : 0);

            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            if (shouldDispose)
            {
                connection.Dispose();
            }
        }
    }

    public async Task<List<MediaItem>> SearchAsync(string query)
    {
        var items = new List<MediaItem>();
        var connection = GetConnection();
        var shouldDispose = ShouldDisposeConnection();

        try
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT * FROM media_items 
                WHERE description LIKE $query OR tags LIKE $query
                ORDER BY created_at DESC
            ";
            command.Parameters.AddWithValue("$query", $"%{query}%");

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(ReadMediaItem(reader));
            }
        }
        finally
        {
            if (shouldDispose)
            {
                connection.Dispose();
            }
        }

        return items;
    }

    private static MediaItem ReadMediaItem(SqliteDataReader reader)
    {
        return new MediaItem
        {
            Id = reader.GetInt32(0),
            FilePath = reader.GetString(1),
            FileHash = reader.GetString(2),
            Description = reader.GetString(3),
            Tags = reader.GetString(4),
            MediaType = reader.GetString(5),
            FileSizeBytes = reader.GetInt64(6),
            CreatedAt = DateTime.Parse(reader.GetString(7)),
            UpdatedAt = DateTime.Parse(reader.GetString(8)),
            IsTagged = reader.GetInt32(9) == 1
        };
    }
}
