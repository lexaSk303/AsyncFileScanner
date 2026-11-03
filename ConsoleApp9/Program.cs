using AsyncFileScanner.Models;
using AsyncFileScanner.Services;

Console.WriteLine("=== FILE SCANNER 2.0 ===\n");

Console.Write("Путь к папке: ");
string path = Console.ReadLine()?.Trim() ?? "";

Console.Write("Расширения: ");
string input = Console.ReadLine() ?? "";
string[] exts = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

Console.Write("Минимальный размер: ");
int minKb = int.Parse(Console.ReadLine() ?? "0");

Console.Write("Максимальный размер: ");
int maxKb = int.Parse(Console.ReadLine() ?? "0");

Console.Write("Максимум одновременных операций: ");
int maxConcurrency = int.Parse(Console.ReadLine() ?? "4");

long minBytes = minKb > 0 ? minKb * 1024 : 0;
long maxBytes = maxKb > 0 ? maxKb * 1024 : long.MaxValue;

var options = new ScanOptions(path, exts, minBytes, maxBytes, maxConcurrency);
var scanner = new FileScanner(new FileProbe());

using var cts = new CancellationTokenSource();
Console.WriteLine("\nНажмите любую клавишу для отмены...\n");
_ = Task.Run(() => { Console.ReadKey(); cts.Cancel(); });

try
{
    var summary = await scanner.ScanAsync(options, cts.Token);

    Console.WriteLine("\n=== РЕЗУЛЬТАТ СКАНИРОВАНИЯ ===\n");
    foreach (var r in summary.Results)
    {
        if (r.IsSuccess)
            Console.WriteLine($"[OK] {r.FileName,-25} {r.Size} байт");
        else
            Console.WriteLine($"[ERR] {r.FileName,-25} {r.ErrorMessage}");
    }

    Console.WriteLine($"\nПросмотрено файлов: {summary.TotalScanned}");
    Console.WriteLine($"Подходящих файлов:  {summary.MatchedFiles}");
    Console.WriteLine($"Общий размер:       {FormatBytes(summary.TotalBytes)}");
    Console.WriteLine($"Ошибок:             {summary.ErrorCount}");
    Console.WriteLine($"Время выполнения:   {summary.Elapsed.TotalSeconds:F2} сек.");
    Console.WriteLine($"\nОграничение: {maxConcurrency}");
    Console.WriteLine($"Максимум одновременно обрабатываемых файлов: {summary.MaxObservedParallel}");
}
catch (OperationCanceledException)
{
    Console.WriteLine("\nОтменено пользователем");
}
catch (Exception ex)
{
    Console.WriteLine($"\nОшибка: {ex.Message}");
}
static string FormatBytes(long bytes)
{
    if (bytes >= 1024 * 1024)
        return $"{bytes / (1024.0 * 1024.0):F1} МБ";
    if (bytes >= 1024)
        return $"{bytes / 1024.0:F1} КБ";
    return $"{bytes} байт";
}