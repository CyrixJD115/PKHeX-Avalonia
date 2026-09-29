using PKHeX.Application.Abstractions;
using PKHeX.Application.Services;
using PKHeX.Core;
using System.Threading;

namespace PKHeX.Infrastructure;

public sealed class SaveFileService : ISaveFileGateway
{
    private readonly ISaveBackupService _backupService;
    private readonly AppSettings _settings;
    private readonly object _stateLock = new();
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private readonly SemaphoreSlim _fileGate = new(1, 1);

    private SaveFile? _currentSave;
    private string? _currentPath;
    private long _sessionId;

    public SaveFile? CurrentSave
    {
        get
        {
            lock (_stateLock)
                return _currentSave;
        }
    }

    public bool HasSave => CurrentSave is not null;
    public string? CurrentPath
    {
        get
        {
            lock (_stateLock)
                return _currentPath;
        }
    }

    public event Action<SaveFile?>? SaveFileChanged;

    public SaveFileService(ISaveBackupService backupService, AppSettings settings)
    {
        _backupService = backupService;
        _settings = settings;
    }

    public async Task<bool> LoadSaveFileAsync(string path)
    {
        await _loadGate.WaitAsync();
        await _fileGate.WaitAsync();
        try
        {
            var sav = await Task.Run(() =>
            {
                try
                {
                    var obj = FileUtil.GetSupportedFile(path);
                    return obj as SaveFile;
                }
                catch
                {
                    return null;
                }
            });

            if (sav is null)
                return false;

            PublishSave(sav, path);
            return true;
        }
        finally
        {
            _fileGate.Release();
            _loadGate.Release();
        }
    }

    public void OpenLoadedSave(SaveFile sav, string? path = null)
    {
        ArgumentNullException.ThrowIfNull(sav);
        PublishSave(sav, path ?? sav.Metadata.FilePath);
    }

    public async Task<bool> SaveFileAsync(string? path = null)
    {
        SaveFile save;
        SaveFile originalSave;
        string savePath;
        long sessionId;

        try
        {
            lock (_stateLock)
            {
                if (_currentSave is null)
                    return false;

                savePath = path ?? _currentPath ?? string.Empty;
                if (string.IsNullOrEmpty(savePath))
                    return false;

                // Clone on the caller's thread, then serialize the detached copy on the worker.
                // The live save can be switched or edited while backup/file I/O runs, but the
                // worker can no longer observe a later session's mutable buffer.
                originalSave = _currentSave;
                save = originalSave.Clone();
                sessionId = _sessionId;
            }
        }
        catch
        {
            return false;
        }

        var result = await Task.Run(() => SaveSnapshot(save, originalSave, savePath, path is not null, sessionId));
        if (!result.Success)
            return false;

        // The await returns to the caller's synchronization context (the UI dispatcher in the
        // desktop app). Compare there so the worker never reads the mutable live save.
        lock (_stateLock)
        {
            if (_sessionId == sessionId && ReferenceEquals(_currentSave, originalSave))
            {
                try
                {
                    if (originalSave.Write().Span.SequenceEqual(result.Data))
                        originalSave.State.Edited = false;
                }
                catch
                {
                    // The disk write succeeded, but the live state could not be compared. Keep
                    // the warning active rather than falsely declaring the save clean.
                }
            }
        }
        return true;
    }

    private (bool Success, byte[] Data) SaveSnapshot(SaveFile save, SaveFile originalSave, string savePath, bool updatePath, long sessionId)
    {
        try
        {
            var data = save.Write().ToArray();
            _fileGate.Wait();
            try
            {
                // Automatic backup: snapshot the bytes currently on disk before overwriting them.
                if (_settings.Backup.BAKEnabled && File.Exists(savePath))
                {
                    try
                    {
                        var existing = File.ReadAllBytes(savePath);
                        var identity = SaveIdentity.Compute(savePath);
                        _backupService.CreateBackup(identity, existing, _settings.SaveBackup.MaxBackupsPerSave);
                    }
                    catch
                    {
                        // Backing up must never prevent the user from saving.
                    }
                }

                File.WriteAllBytes(savePath, data);

                lock (_stateLock)
                {
                    if (_sessionId == sessionId && ReferenceEquals(_currentSave, originalSave))
                    {
                        if (updatePath)
                            _currentPath = savePath;
                    }
                }

                return (true, data);
            }
            finally
            {
                _fileGate.Release();
            }
        }
        catch
        {
            return (false, []);
        }
    }

    private void PublishSave(SaveFile sav, string? path)
    {
        lock (_stateLock)
        {
            _currentSave = sav;
            _currentPath = path;
            _sessionId++;
        }

        SaveFileChanged?.Invoke(sav);
    }

    public void CloseSave()
    {
        lock (_stateLock)
        {
            _currentSave = null;
            _currentPath = null;
            _sessionId++;
        }

        SaveFileChanged?.Invoke(null);
    }
}
