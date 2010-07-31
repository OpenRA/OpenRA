#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

namespace OpenRA.FileFormats
{
	public class ActorReference
	{
		public string Id { get; private set; }
		public readonly string Type;
		public readonly int2 Location;
		public readonly string Owner;
		
		public ActorReference( string id, MiniYaml my)
		{
			Id = id;
			FieldLoader.Load(this, my);
		}
		
		// Legacy construtor for old format maps
		public ActorReference(string id, string type, int2 location, string owner )
		{
			Id = id;
			Type = type;
			Location = location;
			Owner = owner;
		}
	}
}
