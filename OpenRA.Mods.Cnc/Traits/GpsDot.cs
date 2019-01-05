#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Cnc.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Show an indicator revealing the actor underneath the fog when a GPSWatcher is activated.")]
	class GpsDotInfo : ConditionalTraitInfo
	{
		[Desc("Sprite collection for symbols.")]
		public readonly string Image = "gpsdot";

		[Desc("Sprite used for this actor.")]
		[SequenceReference("Image")] public readonly string String = "Infantry";

		[PaletteReference(true)] public readonly string IndicatorPalettePrefix = "player";

		public override object Create(ActorInitializer init) { return new GpsDot(this); }
	}

	class GpsDot : ConditionalTrait<GpsDotInfo>, INotifyCreated, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		GpsDotEffect effect;

		public GpsDot(GpsDotInfo info)
			: base(info) { }

		void INotifyCreated.Created(Actor self)
		{
			effect = new GpsDotEffect(self, Info);
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			self.World.AddFrameEndTask(w =>
			{
				if (!IsTraitDisabled)
					w.Add(effect);
			});
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.AddFrameEndTask(w => w.Remove(effect));
		}

		protected override void TraitEnabled(Actor self)
		{
			self.World.AddFrameEndTask(w =>
			{
				if (self.IsInWorld)
					w.Add(effect);
			});
		}

		protected override void TraitDisabled(Actor self)
		{
			self.World.AddFrameEndTask(w => w.Remove(effect));
		}
	}
}
