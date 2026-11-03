using System.Diagnostics;
using AsyncFileScanner.Models;
namespace AsyncFileScanner.Services;

public class FileScanner
{
    private readonly FileProbe Pr;

    public FileScanner(FileProbe pr)
    {
        Pr = pr;
    }
    private class ScanState
    {
        public int ActiveOperations = 0;
        public int MaxObserved = 0;
        public int MatchedFiles = 0;
        public long TotalBytes = 0;
        public List<ScanResult> Results = new();
        public List<string> Errors = new();
        private readonly object Lock = new object();

        public void AddResult(ScanResult result)
        {
            lock (Lock) 
            { 
                Results.Add(result); 
            }
        }

        public void AddError(string error)
        {
            lock (Lock) 
            {
                Errors.Add(error); 
            }
        }
    }

    public async Task<ScanSummary> ScanAsync(ScanOptions options, CancellationToken token)
    {
        if (!Directory.Exists(options.DirectoryPath))
            throw new DirectoryNotFoundException("Папка не найдена");

        var stopwatch = Stopwatch.StartNew();
        var state = new ScanState();

        using var semaphore = new SemaphoreSlim(options.MaxConcurrency, options.MaxConcurrency);
        string[] files = Directory.GetFiles(options.DirectoryPath);
        var tasks = new List<Task>();
        int totalScanned = 0;

        foreach (string filePath in files)
        {
            token.ThrowIfCancellationRequested();
            string ext = Path.GetExtension(filePath).ToLower();
            bool matchExt = false;

            foreach (string allowed in options.Extensions)
            {
                string check = allowed.Trim().ToLower();
                if (!check.StartsWith(".")) check = "." + check;
                if (ext == check)
                {
                    matchExt = true;
                    break;
                }
            }

            if (!matchExt) 
                continue;

            totalScanned++;
            tasks.Add(ProcessFileLimitedAsync(
                filePath,
                options.MinSizeBytes,
                options.MaxSizeBytes,
                semaphore,
                state,
                token));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        return new ScanSummary(
            totalScanned,
            state.MatchedFiles,
            state.TotalBytes,
            state.Errors.Count,
            state.MaxObserved,
            stopwatch.Elapsed,
            state.Results,
            state.Errors);
    }

    private async Task ProcessFileLimitedAsync(
        string filePath,
        long minBytes,
        long maxBytes,
        SemaphoreSlim semaphore,
        ScanState state,
        CancellationToken token)
    {
        await semaphore.WaitAsync(token);

        try
        {
            int current = Interlocked.Increment(ref state.ActiveOperations);
            int oldMax = state.MaxObserved;
            while (current > oldMax)
            {
                int prev = Interlocked.CompareExchange(ref state.MaxObserved, current, oldMax);
                if (prev == oldMax) break;
                oldMax = prev;
            }

            await ProcessFileAsync(filePath, minBytes, maxBytes, state, token);
        }
        finally
        {
            Interlocked.Decrement(ref state.ActiveOperations);
            semaphore.Release();
        }
    }

    private async Task ProcessFileAsync(
        string filePath,
        long minBytes,
        long maxBytes,
        ScanState state,
        CancellationToken token)
    {
        string fileName = Path.GetFileName(filePath);

        try
        {
            long size = new FileInfo(filePath).Length;

            if (size < minBytes || size > maxBytes)
            {
                return; 
            }

            await Pr.ReadHeaderAsync(filePath, token);
            Interlocked.Increment(ref state.MatchedFiles);
            Interlocked.Add(ref state.TotalBytes, size);
            state.AddResult(new ScanResult(fileName, size, true));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            state.AddError($"{fileName}: {ex.Message}");
            state.AddResult(new ScanResult(fileName, 0, false, ex.Message));
        }
    }
}