#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRA
{
	public delegate void OnProgress(long total, long totalRead, int progressPercentage);

	public static class HttpExtension
	{
		public static async Task ReadAsStreamWithProgress(this HttpResponseMessage response, Stream outputStream, OnProgress onProgress, CancellationToken token)
		{
			var total = response.Content.Headers.ContentLength ?? -1;
			var canReportProgress = total > 0;

#if NET5_0_OR_GREATER
			using (var contentStream = await response.Content.ReadAsStreamAsync(token))
#else
			using (var contentStream = await response.Content.ReadAsStreamAsync())
#endif
			{
				var totalRead = 0L;
				var buffer = new byte[8192];
				var hasMoreToRead = true;

				do
				{
					var read = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length), token);
					if (read == 0)
						hasMoreToRead = false;
					else
					{
						await outputStream.WriteAsync(buffer.AsMemory(0, read), token);

						totalRead += read;

						if (canReportProgress)
						{
							var progressPercentage = (int)((double)totalRead / total * 100);
							onProgress?.Invoke(total, totalRead, progressPercentage);
						}
					}
				}
				while (hasMoreToRead && !token.IsCancellationRequested);

				onProgress?.Invoke(total, totalRead, 100);
			}
		}
	}
}
