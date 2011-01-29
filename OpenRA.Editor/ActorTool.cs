#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.FileFormats;

using SGraphics = System.Drawing.Graphics;

namespace OpenRA.Editor
{
	class ActorTool : ITool
	{
		ActorTemplate Actor;
		public ActorTool(ActorTemplate actor) { this.Actor = actor; }

		public void Preview(Surface surface, SGraphics g)
		{
			/* todo: include the player 
				* in the brush so we can color new buildings too */

			surface.DrawActor(g, surface.GetBrushLocation(), Actor, null);
		}

		public void Apply(Surface surface)
		{
			if (surface.Map.Actors.Any(a => a.Value.Location() == surface.GetBrushLocation()))
				return;

			var owner = "Neutral";
			var id = surface.NextActorName();
			surface.Map.Actors[id] = new ActorReference(Actor.Info.Name.ToLowerInvariant())
			{
				new LocationInit( surface.GetBrushLocation() ),
				new OwnerInit( owner)
			};
		}
	}
}
