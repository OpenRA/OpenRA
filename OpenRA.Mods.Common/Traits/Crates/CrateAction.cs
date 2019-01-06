#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
	public class CrateActionInfo : ConditionalTraitInfo
	{
		[Desc("Chance of getting this crate, assuming the collector is compatible.")]
		public readonly int SelectionShares = 10;

		[Desc("Image containing the crate effect animation sequence.")]
		public readonly string Image = "crate-effects";

		[Desc("Animation sequence played when collected. Leave empty for no effect.")]
		[SequenceReference("Image")] public readonly string Sequence = null;

		[Desc("Palette to draw the animation in.")]
		[PaletteReference] public readonly string Palette = "effect";

		[Desc("Audio clip to play when the crate is collected.")]
		public readonly string Sound = null;

		[Desc("Notification to play when the crate is collected.")]
		[NotificationReference("Speech")] public readonly string Notification = null;

		[Desc("The earliest time (in ticks) that this crate action can occur on.")]
		public readonly int TimeDelay = 0;

		[Desc("Only allow this crate action when the collector has these prerequisites")]
		public readonly string[] Prerequisites = { };

		[Desc("Actor types that this crate action will not occur for.")]
		[ActorReference] public string[] ExcludedActorTypes = { };

		public override object Create(ActorInitializer init) { return new CrateAction(init.Self, this); }
	}

	public class CrateAction : ConditionalTrait<CrateActionInfo>
	{
		readonly Actor self;

		public CrateAction(Actor self, CrateActionInfo info)
			: base(info)
		{
			this.self = self;
		}

		public int GetSelectionSharesOuter(Actor collector)
		{
			if (IsTraitDisabled)
				return 0;

			if (self.World.WorldTick < Info.TimeDelay)
				return 0;

			if (Info.ExcludedActorTypes.Contains(collector.Info.Name))
				return 0;

			if (Info.Prerequisites.Any() && !collector.Owner.PlayerActor.Trait<TechTree>().HasPrerequisites(Info.Prerequisites))
				return 0;

			return GetSelectionShares(collector);
		}

		public virtual int GetSelectionShares(Actor collector)
		{
			return Info.SelectionShares;
		}

		public virtual void Activate(Actor collector)
		{
			Game.Sound.Play(SoundType.World, Info.Sound, self.CenterPosition);

			if (!string.IsNullOrEmpty(Info.Notification))
				Game.Sound.PlayNotification(self.World.Map.Rules, collector.Owner, "Speech",
					Info.Notification, collector.Owner.Faction.InternalName);

			if (Info.Image != null && Info.Sequence != null)
				collector.World.AddFrameEndTask(w => w.Add(new SpriteEffect(collector, w, Info.Image, Info.Sequence, Info.Palette)));
		}
	}
}
