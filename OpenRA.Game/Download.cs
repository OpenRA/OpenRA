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
		readonly object syncObject = new object();
		WebClient wc;

		public static string FormatErrorMessage(Exception e)
		{
			var ex = e as WebException;
			if (ex == null)
				return e.Message;

			switch (ex.Status)
			{
				case WebExceptionStatus.RequestCanceled:
					return "Cancelled";
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

		public Download(string url, string path, Action<DownloadProgressChangedEventArgs> onProgress, Action<AsyncCompletedEventArgs> onComplete)
		{
			lock (syncObject)
			{
				wc = new WebClient { Proxy = null };
				wc.DownloadProgressChanged += (_, a) => onProgress(a);
				wc.DownloadFileCompleted += (_, a) => { DisposeWebClient(); onComplete(a); };
				wc.DownloadFileAsync(new Uri(url), path);
			}
		}

		public Download(string url, Action<DownloadProgressChangedEventArgs> onProgress, Action<DownloadDataCompletedEventArgs> onComplete)
		{
			lock (syncObject)
			{
				wc = new WebClient { Proxy = null };
				wc.DownloadProgressChanged += (_, a) => onProgress(a);
				wc.DownloadDataCompleted += (_, a) => { DisposeWebClient(); onComplete(a); };
				wc.DownloadDataAsync(new Uri(url));
			}
		}

		void DisposeWebClient()
		{
			lock (syncObject)
			{
				wc.Dispose();
				wc = null;
			}
		}

		public void CancelAsync()
		{
			lock (syncObject)
				if (wc != null)
					wc.CancelAsync();
		}
	}
}
