/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.ComponentModel;
using System.Net;
using System.Threading.Tasks;
using Downloader;
using DownloadProgressChangedEventArgs = Downloader.DownloadProgressChangedEventArgs;

namespace OpenNEL_WinUI.Updater;

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
