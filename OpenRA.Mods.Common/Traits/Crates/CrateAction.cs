#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class CrateActionInfo : ITraitInfo
	{
		[Desc("Chance of getting this crate, assuming the collector is compatible.")]
		public readonly int SelectionShares = 10;

		[Desc("An animation defined in sequence yaml(s) to draw.")]
		public readonly string Effect = null;

		[Desc("Palette to draw the animation in.")]
		[PaletteReference] public readonly string Palette = "effect";

		[Desc("Audio clip to play when the crate is collected.")]
		public readonly string Notification = null;

		[Desc("The earliest time (in ticks) that this crate action can occur on.")]
		public readonly int TimeDelay = 0;

		[Desc("Only allow this crate action when the collector has these prerequisites")]
		public readonly string[] Prerequisites = { };

		[Desc("Actor types that this crate action will not occur for.")]
		[ActorReference] public string[] ExcludedActorTypes = { };

		public virtual object Create(ActorInitializer init) { return new CrateAction(init.Self, this); }
	}

	public class CrateAction
	{
		readonly Actor self;
		readonly CrateActionInfo info;

		public CrateAction(Actor self, CrateActionInfo info)
		{
			this.self = self;
			this.info = info;
		}

		public int GetSelectionSharesOuter(Actor collector)
		{
			if (self.World.WorldTick < info.TimeDelay)
				return 0;

			if (info.ExcludedActorTypes.Contains(collector.Info.Name))
				return 0;

			if (info.Prerequisites.Any() && !collector.Owner.PlayerActor.Trait<TechTree>().HasPrerequisites(info.Prerequisites))
				return 0;

			return GetSelectionShares(collector);
		}

		public virtual int GetSelectionShares(Actor collector)
		{
			return info.SelectionShares;
		}

		public virtual void Activate(Actor collector)
		{
			Game.Sound.PlayToPlayer(collector.Owner, info.Notification);

			if (info.Effect != null)
				collector.World.AddFrameEndTask(w => w.Add(new CrateEffect(collector, info.Effect, info.Palette)));
		}
	}
}
