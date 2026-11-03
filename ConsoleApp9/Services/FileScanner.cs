using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AsyncFileScanner.Models;

namespace AsyncFileScanner.Services;

public class FileScanner
{
    private readonly FileProbe Probe;

    public FileScanner(FileProbe probe)
    {
        Probe = probe;
    }

    public async Task<ScanSummary> ScanAsync(ScanOptions options, CancellationToken token)
    {
        if (!Directory.Exists(options.DirectoryPath))
            throw new DirectoryNotFoundException("Папка не найдена");

        var results = new List<ScanResult>();
        var errors = new List<string>();
        int totalFiles = 0;
        long totalBytes = 0;

        string[] files = Directory.GetFiles(options.DirectoryPath);

        foreach (string file in files)
        {
            token.ThrowIfCancellationRequested();

            string ext = Path.GetExtension(file).ToLower();
            bool match = false;
            foreach (string allowed in options.Extensions)
            {
                string check = allowed.Trim().ToLower();
                if (!check.StartsWith(".")) check = "." + check;

                if (ext == check)
                {
                    match = true;
                    break;
                }
            }

            if (!match) continue;

            totalFiles++;
            string name = Path.GetFileName(file);

            try
            {
                long size = new FileInfo(file).Length;
                await Probe.ReadHeaderAsync(file, token);

                results.Add(new ScanResult(name, size, true));
                totalBytes += size;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
                results.Add(new ScanResult(name, 0, false, ex.Message));
            }
        }

        return new ScanSummary(totalFiles, results.Count(r => r.IsSuccess), totalBytes, results, errors);
    }
}