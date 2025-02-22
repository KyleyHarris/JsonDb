using System.Text.Json;
using System.Security.Cryptography;

namespace Hs.JsonDb;

public interface IJsonDbContextOptions
{
    string Path { get; }
}

internal class JsonDbContextOptions : IJsonDbContextOptions
{
    public JsonDbContextOptions(string path)
    {
        Path = path;
    }
    public string Path { get; }
}
/// <summary>
/// this class is intended to be a global singleton embedded server, not a scoped action.
/// </summary>
public class JsonDbContext
{
    protected bool IsInMemory => string.IsNullOrEmpty(Options.Path);

    public JsonDbContext(string path) : this(new JsonDbContextOptions(path))
    {
    }

    public JsonDbContext(IJsonDbContextOptions? options = null)
    {
        Options = options ?? new JsonDbContextOptions(null!);
        if (!IsInMemory)
        {
            ValidatePath();
        }
        ScanForCollections();
        if (!IsInMemory)
        {
            LoadCollections();
        }
        else
        {
            BackupCollectionsState();
        }
    }

    protected Dictionary<string, object> Collections { get; } = new();
    protected Dictionary<string, object> BackupCollections { get; } = new();
    protected Dictionary<string, string> Checksums { get; } = new();

    protected void ScanForCollections()
    {
        var collectionProperties = GetType().GetProperties()
            .Where(p => p.PropertyType.IsGenericType &&
                      (p.PropertyType.GetGenericTypeDefinition() == typeof(List<>) ||
                       p.PropertyType.GetGenericTypeDefinition() == typeof(IList<>)) &&
                      p.PropertyType.GetGenericArguments()[0].IsClass);

        foreach (var prop in collectionProperties)
        {
            if (!Collections.ContainsKey(prop.Name))
            {
                var listType = prop.PropertyType.GetGenericArguments()[0];
                var existingValue = prop.GetValue(this);
                var listInstance = existingValue ?? Activator.CreateInstance(typeof(List<>).MakeGenericType(listType));
                if (existingValue == null)
                {
                    prop.SetValue(this, listInstance);
                }

                if (listInstance != null)
                    Collections[prop.Name] = listInstance;

            }
        }
    }

    private void ValidatePath()
    {
        if (!Directory.Exists(Options.Path))
        {
            throw new DirectoryNotFoundException($"The specified database path does not exist: {Options.Path}");
        }
    }


    private void LoadCollections()
    {
        foreach (var collection in Collections)
        {
            var filePath = Path.Combine(Options.Path, $"{collection.Key}.json");
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                Checksums[collection.Key] = CalculateChecksumFromContent(json);

                var listType = collection.Value.GetType();
                var items = JsonSerializer.Deserialize(json, listType);

                // Get the Clear method via reflection
                var clearMethod = listType.GetMethod("Clear");
                clearMethod?.Invoke(collection.Value, null);

                if (items != null)
                {
                    // Get the AddRange method via reflection
                    var addRangeMethod = listType.GetMethod("AddRange");
                    addRangeMethod?.Invoke(collection.Value, new[] { items });
                }
            }
        }
    }

    private void BackupCollectionsState()
    {
        BackupCollections.Clear();
        foreach (var collection in Collections)
        {
            var serialized = SerializeCollection(collection.Value);
            var deserialized = JsonSerializer.Deserialize(serialized, collection.Value.GetType());
            if (deserialized != null)
            {
                BackupCollections[collection.Key] = deserialized;
            }
        }
    }

    private void RestoreFromBackup()
    {
        foreach (var collection in Collections.ToList())
        {
            var listType = collection.Value.GetType();
            var clearMethod = listType.GetMethod("Clear");
            clearMethod?.Invoke(collection.Value, null);

            if (BackupCollections.TryGetValue(collection.Key, out var backupCollection))
            {
                var addRangeMethod = listType.GetMethod("AddRange");
                var serialized = JsonSerializer.Serialize(backupCollection);
                var deserialized = JsonSerializer.Deserialize(serialized, backupCollection.GetType());
                addRangeMethod?.Invoke(collection.Value, new[] { deserialized });
            }
        }
    }

    public void Rollback()
    {
        if (IsInMemory)
        {
            RestoreFromBackup();
            return;
        }

        foreach (var collection in Collections)
        {
            var listType = collection.Value.GetType();

            // Get the Clear method via reflection
            var clearMethod = listType.GetMethod("Clear");
            clearMethod?.Invoke(collection.Value, null);
        }
        LoadCollections();
    }

    public IJsonDbContextOptions Options { get; }

    private string SerializeCollection(object collection)
    {
        return JsonSerializer.Serialize(collection, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private string CalculateChecksumFromContent(string content)
    {
        using var sha256 = SHA256.Create();
        var contentBytes = System.Text.Encoding.UTF8.GetBytes(content);
        var hash = sha256.ComputeHash(contentBytes);
        return Convert.ToBase64String(hash);
    }

    public void Commit()
    {
        if (IsInMemory)
        {
            BackupCollectionsState();
            return;
        }

        foreach (var collection in Collections)
        {
            var filePath = Path.Combine(Options.Path, $"{collection.Key}.json");
            var serializedData = SerializeCollection(collection.Value);
            var newChecksum = CalculateChecksumFromContent(serializedData);

            if (!File.Exists(filePath) ||
                !Checksums.ContainsKey(collection.Key) ||
                Checksums[collection.Key] != newChecksum)
            {
                File.WriteAllText(filePath, serializedData);
                Checksums[collection.Key] = newChecksum;
            }
        }
    }
}
