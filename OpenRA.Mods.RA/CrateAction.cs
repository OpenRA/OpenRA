#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.RA.Effects;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class CrateActionInfo : ITraitInfo
	{
		[Desc("Chance of getting this crate, assuming the collector is compatible.")]
		public int SelectionShares = 10;
		[Desc("An animation defined in sequence yaml(s) to draw.")]
		public string Effect = null;
		[Desc("Palette to draw the animation in.")]
		public string Palette = "effect";
		[Desc("Audio clip to play when the crate is collected.")]
		public string Notification = null;
		[ActorReference]
		public string[] ExcludedActorTypes = { };

		public virtual object Create(ActorInitializer init) { return new CrateAction(init.self, this); }
	}

	public class CrateAction
	{
		public Actor self;
		public CrateActionInfo info;

		public CrateAction(Actor self, CrateActionInfo info)
		{
			this.self = self;
			this.info = info;
		}

		public int GetSelectionSharesOuter(Actor collector)
		{
			if (info.ExcludedActorTypes.Contains(collector.Info.Name))
				return 0;

			return GetSelectionShares(collector);
		}

		public virtual int GetSelectionShares(Actor collector)
		{
			return info.SelectionShares;
		}

		public virtual void Activate(Actor collector)
		{
			Sound.PlayToPlayer(collector.Owner, info.Notification);

			if (info.Effect != null)
				collector.World.AddFrameEndTask(
					w => w.Add(new CrateEffect(collector, info.Effect, info.Palette)));
		}
	}
}
