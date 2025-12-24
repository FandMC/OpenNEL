using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Net.Http.Headers;
using Serilog;

namespace OpenNEL.GameLauncher.Utils;

public static class DownloadUtil
{
    private static readonly HttpClient HttpClient;

    static DownloadUtil()
    {
        HttpClient = new HttpClient(new HttpClientHandler
        {
            MaxConnectionsPerServer = 16
        })
        {
            Timeout = TimeSpan.FromMinutes(10)
        };
    }

    public static async Task<bool> DownloadAsync(string url, string destinationPath, Action<uint>? downloadProgress = null, int maxConcurrentSegments = 8, CancellationToken cancellationToken = default)
    {
        long totalSize;
        long totalRead;
        Stopwatch stopwatch;
        int lastReportedProgress;
        try
        {
            string? directoryName = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
            using HttpRequestMessage headReq = new(HttpMethod.Head, url);
            using HttpResponseMessage headResp = await HttpClient.SendAsync(headReq, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            headResp.EnsureSuccessStatusCode();
            long? contentLength = headResp.Content.Headers.ContentLength;
            if (contentLength.HasValue)
            {
                totalSize = contentLength.GetValueOrDefault();
                if (headResp.Headers.AcceptRanges.Contains("bytes") && maxConcurrentSegments >= 2 && totalSize >= 1048576)
                {
                    using MemoryMappedFile mmFile = MemoryMappedFile.CreateFromFile(destinationPath, FileMode.Create, null, totalSize, MemoryMappedFileAccess.ReadWrite);
                    ConcurrentBag<Exception> errors = new();
                    IEnumerable<(long, long)> source = CalculateRanges(maxConcurrentSegments * 3, totalSize);
                    totalRead = 0L;
                    stopwatch = Stopwatch.StartNew();
                    lastReportedProgress = -1;
                    using SemaphoreSlim semaphore = new(maxConcurrentSegments, maxConcurrentSegments);
                    await Task.WhenAll(source.Select<(long, long), Task>(async ((long Start, long End) range) =>
                    {
                        await semaphore.WaitAsync(cancellationToken);
                        try
                        {
                            for (int attempt = 1; attempt <= 3; attempt++)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                try
                                {
                                    using HttpRequestMessage req = new(HttpMethod.Get, url);
                                    req.Headers.Range = new RangeHeaderValue(range.Start, range.End);
                                    using HttpResponseMessage resp = await HttpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                                    resp.EnsureSuccessStatusCode();
                                    await using Stream netStream = await resp.Content.ReadAsStreamAsync(cancellationToken);
                                    await using MemoryMappedViewStream viewStream = mmFile.CreateViewStream(range.Start, range.End - range.Start + 1, MemoryMappedFileAccess.Write);
                                    byte[] buffer = new byte[8192];
                                    while (true)
                                    {
                                        int bytesRead = await netStream.ReadAsync(buffer, cancellationToken);
                                        if (bytesRead <= 0)
                                        {
                                            break;
                                        }
                                        await viewStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                                        ReportProgressThrottled(bytesRead);
                                    }
                                    break;
                                }
                                catch (Exception ex) when (attempt < 3 && ex is not OperationCanceledException)
                                {
                                    await Task.Delay(500 * attempt, cancellationToken);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            errors.Add(ex);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                    if (!errors.IsEmpty)
                    {
                        throw new AggregateException(errors);
                    }
                    downloadProgress?.Invoke(100u);
                    return true;
                }
            }
            return await SingleDownloadAsync(url, destinationPath, downloadProgress, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Log.Information("Download canceled: {Url}", url);
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Download failed for {Url}", url);
            return false;
        }

        void ReportProgressThrottled(long bytesRead)
        {
            long num = Interlocked.Add(ref totalRead, bytesRead);
            if (stopwatch.ElapsedMilliseconds > 150)
            {
                stopwatch.Restart();
                int percent = (int)((double)num * 100.0 / totalSize);
                if (percent > lastReportedProgress)
                {
                    lastReportedProgress = percent;
                    downloadProgress?.Invoke((uint)percent);
                }
            }
        }
    }

    private static async Task<bool> SingleDownloadAsync(string url, string destinationPath, Action<uint>? downloadProgress, CancellationToken cancellationToken)
    {
        using HttpResponseMessage resp = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        resp.EnsureSuccessStatusCode();
        long total = resp.Content.Headers.ContentLength.GetValueOrDefault();
        long read = 0L;
        await using Stream input = await resp.Content.ReadAsStreamAsync(cancellationToken);
        await using FileStream output = new(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true);
        byte[] buffer = new byte[8192];
        Stopwatch stopwatch = Stopwatch.StartNew();
        int lastReportedProgress = -1;
        while (true)
        {
            int n = await input.ReadAsync(buffer, cancellationToken);
            if (n <= 0)
            {
                break;
            }
            await output.WriteAsync(buffer.AsMemory(0, n), cancellationToken);
            if (total > 0)
            {
                read += n;
                if (stopwatch.ElapsedMilliseconds > 150)
                {
                    stopwatch.Restart();
                    int percent = (int)((double)read * 100.0 / total);
                    if (percent > lastReportedProgress)
                    {
                        lastReportedProgress = percent;
                        downloadProgress?.Invoke((uint)percent);
                    }
                }
            }
        }
        downloadProgress?.Invoke(100u);
        return true;
    }

    private static IEnumerable<(long Start, long End)> CalculateRanges(int segments, long totalSize)
    {
        long segmentSize = totalSize / segments;
        for (int i = 0; i < segments; i++)
        {
            long start = i * segmentSize;
            long end = (i == segments - 1) ? (totalSize - 1) : ((i + 1) * segmentSize - 1);
            yield return (start, end);
        }
    }
}
