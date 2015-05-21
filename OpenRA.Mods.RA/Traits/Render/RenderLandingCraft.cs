#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	public class RenderLandingCraftInfo : RenderUnitInfo, Requires<IMoveInfo>, Requires<CargoInfo>
	{
		public readonly string[] OpenTerrainTypes = { "Clear" };
		public readonly string OpenAnim = "open";
		public readonly string UnloadAnim = "unload";

		public override object Create(ActorInitializer init) { return new RenderLandingCraft(init, this); }
	}

	public class RenderLandingCraft : RenderUnit
	{
		readonly RenderLandingCraftInfo info;
		readonly Actor self;
		readonly Cargo cargo;
		readonly IMove move;
		bool open;

		public RenderLandingCraft(ActorInitializer init, RenderLandingCraftInfo info)
			: base(init, info)
		{
			this.info = info;
			self = init.Self;
			cargo = self.Trait<Cargo>();
			move = self.Trait<IMove>();
		}

		public bool ShouldBeOpen()
		{
			if (self.CenterPosition.Z > 0 || move.IsMoving)
				return false;

			return cargo.CurrentAdjacentCells.Any(c => self.World.Map.Contains(c)
				&& info.OpenTerrainTypes.Contains(self.World.Map.GetTerrainInfo(c).Type));
		}

		void Open()
		{
			if (open || !DefaultAnimation.HasSequence(info.OpenAnim))
				return;

			open = true;
			PlayCustomAnimation(self, info.OpenAnim, () =>
			{
				if (DefaultAnimation.HasSequence(info.UnloadAnim))
					PlayCustomAnimationRepeating(self, info.UnloadAnim);
			});
		}

		void Close()
		{
			if (!open || !DefaultAnimation.HasSequence(info.OpenAnim))
				return;

			open = false;
			PlayCustomAnimationBackwards(self, info.OpenAnim, null);
		}

		public override void Tick(Actor self)
		{
			if (ShouldBeOpen())
				Open();
			else
				Close();

			base.Tick(self);
		}
	}
}
