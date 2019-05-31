#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

namespace OpenRA.Server
{
	public static class ProtocolVersion
	{
		// The protocol for the initial handshake request and response
		// Backwards incompatible changes will break runtime mod switching, so only change as a last resort!
		public const int Handshake = 7;

		// The protocol for server and world orders
		// This applies after the handshake has completed, and is provided to support
		// alternative server implementations that wish to support multiple versions in parallel
		public const int Orders = 8;
	}
}
