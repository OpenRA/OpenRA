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
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace OpenRA.Network
{
	public sealed class ConnectionTarget
	{
		readonly DnsEndPoint[] endpoints;

		public ConnectionTarget()
		{
			endpoints = new[] { new DnsEndPoint("invalid", 0) };
		}

		public ConnectionTarget(string host, int port)
		{
			endpoints = new[] { new DnsEndPoint(host, port) };
		}

		public ConnectionTarget(IEnumerable<DnsEndPoint> endpoints)
		{
			this.endpoints = endpoints.ToArray();
			if (this.endpoints.Length == 0)
				throw new ArgumentException("ConnectionTarget must have at least one address.");
		}

		public IEnumerable<IPEndPoint> GetConnectEndPoints()
		{
			return endpoints.SelectMany(e =>
			{
				try
				{
					return Dns.GetHostAddresses(e.Host)
						.Select(a => new IPEndPoint(a, e.Port));
				}
				catch (Exception)
				{
					return Enumerable.Empty<IPEndPoint>();
				}
			}).ToList();
		}

		public override string ToString()
		{
			return endpoints
				.Select(e => $"{e.Host}:{e.Port}")
				.JoinWith("/");
		}
	}
}
