using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Akvila.Launcher.Core.Exceptions;
using Akvila.Client;
using Splat;
using SQLite;

namespace Akvila.Launcher.Core.Services;

public class LocalStorageService : IStorageService {
    private const string DatabaseFileName = "data.db";
    private readonly SQLiteAsyncConnection _database;

    public LocalStorageService(ISystemService? systemService = null, IAkvilaClientManager? akvilaClient = null) {
        var akvilaClientManager = akvilaClient
                               ?? Locator.Current.GetService<IAkvilaClientManager>()
                               ?? throw new ServiceNotFoundException(typeof(IAkvilaClientManager));

        var systemServiceDependency = systemService
                                      ?? Locator.Current.GetService<ISystemService>()
                                      ?? throw new ServiceNotFoundException(typeof(ISystemService));

        var databasePath = Path.Combine(systemServiceDependency.GetGameFolder(akvilaClientManager.ProjectName, true),
            DatabaseFileName);

        _database = new SQLiteAsyncConnection(databasePath);

        InitializeTables();
    }

    public async Task SetAsync<T>(string key, T value, CancellationToken? token = default) {
        var serializedValue = JsonSerializer.Serialize(value);
        var storageItem = new StorageItem {
            Key = key,
            TypeName = typeof(T).FullName,
            Value = serializedValue
        };

        await _database.InsertOrReplaceAsync(storageItem);
    }

    public async Task<T?> GetAsync<T>(string key) {
        var storageItem = await _database.Table<StorageItem>()
            .Where(si => si.Key == key)
            .FirstOrDefaultAsync();

        if (storageItem != null) return JsonSerializer.Deserialize<T>(storageItem.Value);

        return default;
    }

    public Task<int> SaveRecord<T>(T record) {
        return _database.InsertOrReplaceAsync(record);
    }

    public async Task<string> GetLogsAsync(int rowCount = 100) {
        var logs = await _database.Table<LogsItem>().Take(rowCount).ToListAsync();

        return string.Join("\n", logs.Select(c => c.Message));
    }

    private void InitializeTables() {
        _database.CreateTableAsync<StorageItem>().Wait();
        _database.CreateTableAsync<LogsItem>().Wait();
    }

    [Table("StorageItems")]
    private class StorageItem {
        [PrimaryKey] public string Key { get; init; } = null!;
        public string? TypeName { get; set; }
        public string Value { get; init; } = null!;
    }

    [Table("Logs")]
    private class LogsItem {
        [PrimaryKey] public string Date { get; set; } = null!;
        public string? Message { get; }
        public string StackTrace { get; set; } = null!;
    }
}
