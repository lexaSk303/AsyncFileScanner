using AsyncFileScanner.Models;
using AsyncFileScanner.Services;

Console.WriteLine("=== FILE SCANNER ===\n");

Console.Write("Путь к папке: ");
string path = Console.ReadLine()?.Trim() ?? "";

Console.Write("Расширения (через запятую): ");
string input = Console.ReadLine() ?? "";
string[] exts = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

var options = new ScanOptions(path, exts);
var scanner = new FileScanner(new FileProbe());

using var cts = new CancellationTokenSource();
Console.WriteLine("\nНажмите любую клавишу для отмены...\n");
_ = Task.Run(() => { Console.ReadKey(); cts.Cancel(); });

try
{
    var summary = await scanner.ScanAsync(options, cts.Token);

    Console.WriteLine("\n=== РЕЗУЛЬТАТЫ ===");
    foreach (var r in summary.Results)
    {
        if (r.IsSuccess)
            Console.WriteLine($"{r.FileName} - {r.Size} байт");
        else
            Console.WriteLine($"{r.FileName} - ОШИБКА");
    }

    Console.WriteLine($"\nВсего файлов: {summary.TotalFiles}");
    Console.WriteLine($"Общий размер: {summary.TotalBytes} байт");
}
catch (OperationCanceledException)
{
    Console.WriteLine("\nОтменено пользователем");
}
catch (Exception ex)
{
    Console.WriteLine($"\nОшибка: {ex.Message}");
}