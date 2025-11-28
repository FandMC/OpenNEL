using System.ComponentModel;
using System.Net;
using Downloader;
using DownloadProgressChangedEventArgs = Downloader.DownloadProgressChangedEventArgs;

namespace OpenNEL.Updater;

public static class Downloader
{
	public static Task CreateDownloadTaskAsync(string url, string filePath, Action<int> downloadProgress, Action<bool, Exception?> downloadFinishCallback)
	{
		DownloadService downloadService = new DownloadService(new DownloadConfiguration
		{
			ChunkCount = 8,
			ParallelDownload = true,
			RequestConfiguration = 
			{
				Accept = "*/*",
				KeepAlive = true,
				ProtocolVersion = HttpVersion.Version11
			}
		});
		downloadService.DownloadProgressChanged += delegate(object? _, DownloadProgressChangedEventArgs e)
		{
			downloadProgress((int)e.ProgressPercentage);
		};
		downloadService.DownloadFileCompleted += delegate(object? _, AsyncCompletedEventArgs e)
		{
			downloadFinishCallback(!e.Cancelled && e.Error == null, e.Error);
		};
		return downloadService.DownloadFileTaskAsync(url, filePath);
	}
}
