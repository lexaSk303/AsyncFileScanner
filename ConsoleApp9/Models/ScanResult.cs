namespace AsyncFileScanner.Models;
public sealed record ScanResult(string FileName, long Size, bool IsSuccess, string? ErrorMessage = null);