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

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Network
{
	public class HandshakeRequest
	{
		public string Mod;
		public string Version;
		public string AuthToken;

		public static HandshakeRequest Deserialize(string data)
		{
			var handshake = new HandshakeRequest();
			FieldLoader.Load(handshake, MiniYaml.FromString(data).First().Value);
			return handshake;
		}

		public string Serialize()
		{
			var data = new List<MiniYamlNode> { new MiniYamlNode("Handshake", FieldSaver.Save(this)) };
			return data.WriteToString();
		}
	}

	public class HandshakeResponse
	{
		public string Mod;
		public string Version;
		public string Password;

		// Default value is hardcoded to 7 so that newer servers
		// (which define OrdersProtocol > 7) can detect older clients
		public int OrdersProtocol = 7;

		// For player authentication
		public string Fingerprint;
		public string AuthSignature;

		[FieldLoader.Ignore]
		public Session.Client Client;

		public static HandshakeResponse Deserialize(string data)
		{
			var handshake = new HandshakeResponse
			{
				Client = new Session.Client()
			};

			var ys = MiniYaml.FromString(data);
			foreach (var y in ys)
			{
				switch (y.Key)
				{
					case "Handshake":
						FieldLoader.Load(handshake, y.Value);
						break;
					case "Client":
						FieldLoader.Load(handshake.Client, y.Value);
						break;
				}
			}

			return handshake;
		}

		public string Serialize()
		{
			var data = new List<MiniYamlNode>
			{
				new MiniYamlNode("Handshake", null,
					new[] { "Mod", "Version", "Password", "Fingerprint", "AuthSignature", "OrdersProtocol" }.Select(p => FieldSaver.SaveField(this, p)).ToList()),
				new MiniYamlNode("Client", FieldSaver.Save(Client))
			};

			return data.WriteToString();
		}
	}
}
