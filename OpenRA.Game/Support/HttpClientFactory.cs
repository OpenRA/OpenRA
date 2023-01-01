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
using System.Net.Http;

namespace OpenRA.Support
{
	public class HttpClientFactory
	{
#if NET5_0_OR_GREATER
		const int MaxConnectionPerServer = 20;
		static readonly TimeSpan ConnectionLifeTime = TimeSpan.FromMinutes(1);
#endif

		static readonly Lazy<HttpMessageHandler> Handler = new Lazy<HttpMessageHandler>(GetHandler);

		public static HttpClient Create()
		{
			return new HttpClient(Handler.Value, false);
		}

		static HttpMessageHandler GetHandler()
		{
#if NET5_0_OR_GREATER
			return new SocketsHttpHandler
			{
				// https://github.com/dotnet/corefx/issues/26895
				// https://github.com/dotnet/corefx/issues/26331
				// https://github.com/dotnet/corefx/pull/26839
				PooledConnectionLifetime = ConnectionLifeTime,
				PooledConnectionIdleTimeout = ConnectionLifeTime,
				MaxConnectionsPerServer = MaxConnectionPerServer
			};
#else
			return new HttpClientHandler();
#endif
		}
	}
}
