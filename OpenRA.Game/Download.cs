#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRA
{
	public delegate void OnProgress(long total, long totalRead, int progress);

	public static class HttpExtension
	{
		public static async Task ReadAsStreamWithProgress(this HttpResponseMessage response, Stream stream, OnProgress progress, CancellationToken token)
		{
			var total = response.Content.Headers.ContentLength.HasValue
				? response.Content.Headers.ContentLength.Value
				: -1L;
			var canReportProgress = total != -1;

			using (var contentStream = await response.Content.ReadAsStreamAsync())
			{
				var totalRead = 0L;
				var buffer = new byte[8192];
				var isMoreToRead = true;

				do
				{
					var read = await contentStream.ReadAsync(buffer, 0, buffer.Length, token);
					if (read == 0)
						isMoreToRead = false;
					else
					{
						await stream.WriteAsync(buffer, 0, read, token);

						totalRead += read;

						if (canReportProgress)
						{
							var progressPercentage = (int)((double)totalRead / total * 100);
							progress?.Invoke(total, totalRead, progressPercentage);
						}
					}
				}
				while (isMoreToRead && !token.IsCancellationRequested);

				progress?.Invoke(total, totalRead, 100);
			}
		}
	}
}
