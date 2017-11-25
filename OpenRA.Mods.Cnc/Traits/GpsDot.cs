#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Show an indicator revealing the actor underneath the fog when a GPSWatcher is activated.")]
	class GpsDotInfo : ITraitInfo
	{
		[Desc("Sprite collection for symbols.")]
		public readonly string Image = "gpsdot";

		[Desc("Sprite used for this actor.")]
		[SequenceReference("Image")] public readonly string String = "Infantry";

		[PaletteReference(true)] public readonly string IndicatorPalettePrefix = "player";

		public object Create(ActorInitializer init) { return new GpsDot(this); }
	}

	class GpsDot : INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly GpsDotInfo info;
		GpsDotEffect effect;

		public GpsDot(GpsDotInfo info)
		{
			this.info = info;
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			effect = new GpsDotEffect(self, info);
			self.World.AddFrameEndTask(w => w.Add(effect));
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.AddFrameEndTask(w => w.Remove(effect));
		}
	}
}
