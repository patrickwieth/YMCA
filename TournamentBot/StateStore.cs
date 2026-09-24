using System.Text.Json;

namespace Ymca.TournamentBot;

public sealed class StateStore
{
    readonly string path;
    readonly SemaphoreSlim gate = new(1, 1);
    TournamentState state = new();

    public StateStore(string path)
    {
        this.path = path;
    }

    public async Task LoadAsync()
    {
        await gate.WaitAsync();
        try
        {
            if (!File.Exists(path))
                return;

            await using var stream = File.OpenRead(path);
            state = await JsonSerializer.DeserializeAsync<TournamentState>(stream, JsonOptions.Default)
                ?? new TournamentState();
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<T> ReadAsync<T>(Func<TournamentState, T> read)
    {
        await gate.WaitAsync();
        try
        {
            return read(state);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<T> UpdateAsync<T>(Func<TournamentState, T> update)
    {
        await gate.WaitAsync();
        try
        {
            var result = update(state);
            await SaveUnsafeAsync();
            return result;
        }
        finally
        {
            gate.Release();
        }
    }

    public Task UpdateAsync(Action<TournamentState> update) => UpdateAsync(state =>
    {
        update(state);
        return true;
    });

    async Task SaveUnsafeAsync()
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var temporaryPath = path + ".tmp";
        await using (var stream = File.Create(temporaryPath))
            await JsonSerializer.SerializeAsync(stream, state, JsonOptions.Default);

        File.Move(temporaryPath, path, true);
    }
}
