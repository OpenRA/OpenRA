#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.FileFormats;

using SGraphics = System.Drawing.Graphics;

namespace OpenRA.Editor
{
	class ActorTool : ITool
	{
		ActorTemplate actorTemplate;
		public ActorTool(ActorTemplate actor) { this.actorTemplate = actor; }

		public void Preview(Surface surface, SGraphics g)
		{
			surface.DrawActor(g, surface.GetBrushLocation(), actorTemplate,
				surface.GetPaletteForPlayer(surface.NewActorOwner));
		}

		public void Apply(Surface surface)
		{
			if (surface.Map.Actors.Value.Any(a => a.Value.Location() == surface.GetBrushLocation()))
				return;

			var owner = surface.NewActorOwner;
			var id = NextActorName(surface);
			surface.Map.Actors.Value[id] = new ActorReference(actorTemplate.Info.Name.ToLowerInvariant())
			{
				new LocationInit(surface.GetBrushLocation()),
				new OwnerInit(owner)
			};
		}

		static string NextActorName(Surface surface)
		{
			var id = 0;
			for (;;)
			{
				var possible = "Actor{0}".F(id++);
				if (!surface.Map.Actors.Value.ContainsKey(possible)) return possible;
			}
		}
	}
}
