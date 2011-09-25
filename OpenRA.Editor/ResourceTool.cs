#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
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
			surface.Map.MapResources.Value[surface.GetBrushLocation().X, surface.GetBrushLocation().Y]
				= new TileReference<byte, byte>
				{
					type = (byte)Resource.Info.ResourceType,
					index = (byte)random.Next(Resource.Info.SpriteNames.Length)
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
			surface.DrawImage(g, Resource.Bitmap, surface.GetBrushLocation(), false, null);
		}

		Random random = new Random();
	}
}
