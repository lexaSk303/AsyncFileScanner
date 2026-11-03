namespace AsyncFileScanner.Models;

public sealed record ScanSummary(int TotalFiles, int SuccessCount, long TotalBytes, List<ScanResult> Results, List<string> Errors);