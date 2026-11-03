namespace AsyncFileScanner.Services;

public class FileProbe
{
    public async Task<int> ReadHeaderAsync(string path, CancellationToken token)
    {
        byte[] buffer = new byte[1024];
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 1024,
            useAsync: true);

        return await stream.ReadAsync(buffer.AsMemory(), token);
    }
}