#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.ComponentModel;
using System.Net;

namespace OpenRA
{
	public class Download
	{
		WebClient wc;
		bool cancelled;

		public static string FormatErrorMessage(Exception e)
		{
			var ex = e as WebException;
			if (ex == null)
				return e.Message;

			switch (ex.Status)
			{
				case WebExceptionStatus.NameResolutionFailure:
					return "DNS lookup failed";
				case WebExceptionStatus.Timeout:
					return "Connection timeout";
				case WebExceptionStatus.ConnectFailure:
					return "Cannot connect to remote server";
				case WebExceptionStatus.ProtocolError:
					return "File not found on remote server";
				default:
					return ex.Message;
			}
		}

		public Download(string url, string path, Action<DownloadProgressChangedEventArgs> onProgress, Action<AsyncCompletedEventArgs, bool> onComplete)
		{
			wc = new WebClient();
			wc.Proxy = null;

			wc.DownloadProgressChanged += (_, a) => onProgress(a);
			wc.DownloadFileCompleted += (_, a) => onComplete(a, cancelled);

			Game.OnQuit += Cancel;
			wc.DownloadFileCompleted += (_, a) => { Game.OnQuit -= Cancel; };

			wc.DownloadFileAsync(new Uri(url), path);
		}

		public Download(string url, Action<DownloadProgressChangedEventArgs> onProgress, Action<DownloadDataCompletedEventArgs, bool> onComplete)
		{
			wc = new WebClient();
			wc.Proxy = null;

			wc.DownloadProgressChanged += (_, a) => onProgress(a);
			wc.DownloadDataCompleted += (_, a) => onComplete(a, cancelled);

			Game.OnQuit += Cancel;
			wc.DownloadDataCompleted += (_, a) => { Game.OnQuit -= Cancel; };

			wc.DownloadDataAsync(new Uri(url));
		}

		public void Cancel()
		{
			Game.OnQuit -= Cancel;
			wc.CancelAsync();
			wc.Dispose();
			cancelled = true;
		}
	}
}
