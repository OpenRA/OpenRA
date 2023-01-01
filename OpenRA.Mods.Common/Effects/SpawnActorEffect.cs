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
using OpenRA.Activities;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Effects
{
	public class SpawnActorEffect : IEffect
	{
		readonly Actor actor;
		readonly CPos[] pathAfterSpawn;
		readonly Activity activityAtDestination;
		readonly IMove move;
		int remainingDelay;

		public SpawnActorEffect(Actor actor)
			: this(actor, 0, Array.Empty<CPos>(), null) { }

		public SpawnActorEffect(Actor actor, int delay)
			: this(actor, delay, Array.Empty<CPos>(), null) { }

		public SpawnActorEffect(Actor actor, int delay, CPos[] pathAfterSpawn, Activity activityAtDestination)
		{
			this.actor = actor;
			remainingDelay = delay;
			this.pathAfterSpawn = pathAfterSpawn;
			this.activityAtDestination = activityAtDestination;
			move = actor.TraitOrDefault<IMove>();
		}

		public void Tick(World world)
		{
			if (remainingDelay-- > 0)
				return;

			world.Add(actor);
			if (move != null)
				for (var j = 0; j < pathAfterSpawn.Length; j++)
					actor.QueueActivity(move.MoveTo(pathAfterSpawn[j], 2));

			if (activityAtDestination != null)
				actor.QueueActivity(activityAtDestination);

			world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr) { return SpriteRenderable.None; }
	}
}
