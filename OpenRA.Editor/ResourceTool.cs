#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.FileFormats;

using SGraphics = System.Drawing.Graphics;

namespace OpenRA.Editor
{
	class ResourceTool : ITool
	{
		ResourceTemplate Resource;

		public ResourceTool(ResourceTemplate resource) { Resource = resource; }

		public void Apply(Surface surface)
		{
			surface.Map.MapResources[surface.GetBrushLocation().X, surface.GetBrushLocation().Y]
				= new TileReference<byte, byte>
				{
					type = (byte)Resource.Info.ResourceType,
					index = (byte)surface.random.Next(Resource.Info.SpriteNames.Length),
					image = (byte)Resource.Value
				};

			var ch = new int2((surface.GetBrushLocation().X) / Surface.ChunkSize, 
				(surface.GetBrushLocation().Y) / Surface.ChunkSize);

			if (surface.Chunks.ContainsKey(ch))
			{
				surface.Chunks[ch].Dispose();
				surface.Chunks.Remove(ch);
			}
		}

		public void Preview(Surface surface, SGraphics g)
		{
			surface.DrawImage(g, Resource.Bitmap, surface.GetBrushLocation());
		}
	}
}
