#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Network
{
	public class HandshakeResponse
	{
		public string Name;
		public Color Color1;
		public Color Color2;
		public string[] Mods = { "ra" };	// mod names
		public string Password;
		
		public string Serialize()
		{
			var data = new List<MiniYamlNode>();
			data.Add(new MiniYamlNode("Handshake", FieldSaver.Save(this)));
			System.Console.WriteLine("Serializing handshake response:");
			System.Console.WriteLine(data.WriteToString());
			
			return data.WriteToString();
		}

		public static HandshakeResponse Deserialize(string data)
		{
			System.Console.WriteLine("Deserializing handshake response:");
			System.Console.WriteLine(data);
			
			var handshake = new HandshakeResponse();
			var ys = MiniYaml.FromString(data);
			FieldLoader.Load(handshake, ys.First().Value);
			return handshake;
		}
	}
}