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
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	public class RenderLandingCraftInfo : RenderUnitInfo
	{
		public readonly string[] OpenTerrainTypes = { "Clear" };
		public readonly string OpenAnim = "open";
		public readonly string UnloadAnim = "unload";

		public override object Create(ActorInitializer init) { return new RenderLandingCraft(init.self, this); }
	}

	public class RenderLandingCraft : RenderUnit
	{
		readonly Actor self;
		readonly Cargo cargo;
		readonly RenderLandingCraftInfo info;
		bool open;

		public RenderLandingCraft(Actor self, RenderLandingCraftInfo info)
			: base(self)
		{
			this.self = self;
			cargo = self.Trait<Cargo>();
			this.info = info;
		}

		public bool ShouldBeOpen()
		{
			if (self.CenterPosition.Z > 0)
				return false;

			if (cargo.IsFull(self))
				return false;

			return cargo.CurrentAdjacentCells
				.Any(c => info.OpenTerrainTypes.Contains(self.World.GetTerrainType(c)));
		}

		void Open()
		{
			if (open || !anim.HasSequence(info.OpenAnim))
				return;

			open = true;
			PlayCustomAnimation(self, info.OpenAnim, () =>
			{
				if (anim.HasSequence(info.UnloadAnim))
					PlayCustomAnimRepeating(self, info.UnloadAnim);
			});
		}

		void Close()
		{
			if (!open || !anim.HasSequence(info.OpenAnim))
				return;

			open = false;
			PlayCustomAnimBackwards(self, info.OpenAnim, null);
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
