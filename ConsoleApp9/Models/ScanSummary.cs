namespace AsyncFileScanner.Models;

public sealed record ScanSummary(
    int TotalScanned,     
    int MatchedFiles,      
    long TotalBytes,       
    int ErrorCount,          
    int MaxObservedParallel, 
    System.TimeSpan Elapsed, 
    List<ScanResult> Results,
    List<string> Errors);