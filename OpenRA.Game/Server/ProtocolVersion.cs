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

namespace OpenRA.Server
{
	public static class ProtocolVersion
	{
		// OpenRA's network protocol defines a packet structure:
		// - Int32 specifying the length of the packet, ignoring this length field
		//   The connection will be terminated if a packet with length > 128kB is received by the server
		// - Int32 specifying the client ID sending the orders (or 0 if the orders are created by the server)
		// - Int32 specifying the game network tick / "frame" the order belongs to
		// - Zero or more orders, encoded as:
		//   - Byte order type
		//   - Order-specific data
		//
		// Order types are:
		// - 0x65: World sync hash:
		//   - Int32 containing the sync hash value
		//   - UInt64 containing the current defeat state (a bit set
		//     to 1 means the corresponding player is defeated)
		// - 0xBF: Player disconnected
		// - 0xFE: Handshake (also used for ServerOrders for ProtocolVersion.Orders < 8)
		//   - Length-prefixed string specifying a name or key
		//   - Length-prefixed string specifying a value / data
		// - 0xFF: World order
		//   - Length-prefixed string specifying the order name
		//   - OrderFields enum encoded as a byte: specifies the data included in the rest of the order
		//   - Order-specific data - see OpenRA.Game/Server/Order.cs for details
		//
		// When the frame of a packet is 0, it is an immediate order, and may or may not be relayed to clients e.g. chat
		//
		// When the frame is not 0, it will always be relayed to all clients
		// and in UseNewNetcode mode, a packet with a number of orders messages to ack is returned to the sender
		//
		// A connection handshake begins when a client opens a connection to the server:
		// - Server sends:
		//   - Int32 specifying the handshake protocol version
		//   - Int32 specifying the new connection's client ID
		// - Server sends a packet that contains a single Handshake (0xFE) order that
		//   encodes a HandshakeRequest yaml blob containing at least:
		//   - Mod: Internal ID for the active mod
		//   - Version: Internal version string for the active mod
		//   - [optional] AuthToken: Blob of random data that the client can sign to verify their AuthID
		// - Client disconnects and optionally shows a switch-mod dialog if the Mod or Version do not match
		// - Client responds with a packet that contains a single Handshake (0xFE) order that
		//   encodes a HandshakeResponse yaml blob containing at least:
		//   - Mod: Internal ID for the active mod
		//   - Version: Internal version string for the active mod
		//   - Client: Yaml blob encoding client metadata:
		//     - Name: Client name
		//     - [optional] Color: Client's current color choice
		//     - [optional] PreferredColor: Client's preferred color choice
		//     - [optional] Password: Password to enter the server
		//   - [optional] Fingerprint: String used to query the players authentication public key
		//   - [optional] AuthSignature: AuthToken signature generated using the client's authentication private key
		//   - [optional] OrdersProtocol: ProtocolVersion.Orders that the client will use (assumed 7 if omitted)
		// - Server disconnects client if Mod or Version do not match or it does not accept the requested OrderProtocol
		// - Server checks password and sends an AuthenticationError order then disconnects the client if it fails

		// The protocol for the initial handshake request and response
		// Backwards incompatible changes will break runtime mod switching, so only change as a last resort!
		public const int Handshake = 7;

		// The protocol for server and world orders
		// This applies after the handshake has completed, and is provided to support
		// alternative server implementations that wish to support multiple versions in parallel
		public const int Orders = 11;
	}
}
