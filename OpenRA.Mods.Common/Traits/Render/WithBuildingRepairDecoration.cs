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

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Displays a custom UI overlay relative to the actor's mouseover bounds.")]
	public class WithBuildingRepairDecorationInfo : WithDecorationInfo, Requires<RepairableBuildingInfo>
	{
		public override object Create(ActorInitializer init) { return new WithBuildingRepairDecoration(init.Self, this); }
	}

	public class WithBuildingRepairDecoration : WithDecoration
	{
		readonly RepairableBuilding rb;
		readonly WithBuildingRepairDecorationInfo info;
		int shownPlayer = 0;

		public WithBuildingRepairDecoration(Actor self, WithBuildingRepairDecorationInfo info)
			: base(self, info)
		{
			this.info = info;
			rb = self.Trait<RepairableBuilding>();
			anim = new Animation(self.World, info.Image,
				() => !rb.RepairActive || rb.IsTraitDisabled || !ShouldRender(self));
			anim.PlayThen(info.Sequence, CycleRepairer);
			CycleRepairer();
		}

		protected override bool ShouldRender(Actor self)
		{
			if (rb.Repairers.Count == 0)
				return false;

			return base.ShouldRender(self);
		}

		protected override PaletteReference GetPalette(Actor self, WorldRenderer wr)
		{
			if (!info.IsPlayerPalette)
				return wr.Palette(info.Palette);

			return wr.Palette(info.Palette + rb.Repairers[shownPlayer % rb.Repairers.Count].InternalName);
		}

		void CycleRepairer()
		{
			anim.PlayThen(info.Sequence, CycleRepairer);

			if (++shownPlayer == rb.Repairers.Count)
				shownPlayer = 0;
		}
	}
}
