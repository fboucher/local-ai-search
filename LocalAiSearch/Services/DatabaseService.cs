namespace LocalAiSearch.Services;

/// <summary>
/// Manages Turso/libsql database operations for media items.
/// </summary>
public class DatabaseService
{
    // TODO: Implement per PRD spec
    // - Connection: libsql://file:./local.db
    // - Schema: media_items table (id, file_path, description, media_type, tags JSON, file_hash, created_at, updated_at)
    // - CRUD operations: SaveMediaItemAsync, GetAllMediaItemsAsync, SearchByTagsAsync
    // - Index on tags for fast search
}
