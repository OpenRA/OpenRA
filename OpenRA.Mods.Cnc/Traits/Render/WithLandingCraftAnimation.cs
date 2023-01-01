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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits.Render
{
	public class WithLandingCraftAnimationInfo : TraitInfo, Requires<IMoveInfo>, Requires<WithSpriteBodyInfo>, Requires<CargoInfo>
	{
		public readonly HashSet<string> OpenTerrainTypes = new HashSet<string> { "Clear" };

		[SequenceReference]
		public readonly string OpenSequence = "open";

		[SequenceReference]
		public readonly string CloseSequence = "close";

		[SequenceReference]
		public readonly string UnloadSequence = "unload";

		[Desc("Which sprite body to play the animation on.")]
		public readonly string Body = "body";

		public override object Create(ActorInitializer init) { return new WithLandingCraftAnimation(init, this); }
	}

	public class WithLandingCraftAnimation : ITick
	{
		readonly WithLandingCraftAnimationInfo info;
		readonly Actor self;
		readonly Cargo cargo;
		readonly IMove move;
		readonly WithSpriteBody wsb;
		bool open;

		public WithLandingCraftAnimation(ActorInitializer init, WithLandingCraftAnimationInfo info)
		{
			this.info = info;
			self = init.Self;
			cargo = self.Trait<Cargo>();
			move = self.Trait<IMove>();
			wsb = init.Self.TraitsImplementing<WithSpriteBody>().Single(w => w.Info.Name == info.Body);
		}

		public bool ShouldBeOpen()
		{
			if (move.CurrentMovementTypes != MovementType.None || self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length > 0)
				return false;

			return cargo.CurrentAdjacentCells.Any(c => self.World.Map.Contains(c)
				&& info.OpenTerrainTypes.Contains(self.World.Map.GetTerrainInfo(c).Type));
		}

		void Open()
		{
			if (open || !wsb.DefaultAnimation.HasSequence(info.OpenSequence))
				return;

			open = true;
			wsb.PlayCustomAnimation(self, info.OpenSequence, () =>
			{
				if (wsb.DefaultAnimation.HasSequence(info.UnloadSequence))
					wsb.PlayCustomAnimationRepeating(self, info.UnloadSequence);
			});
		}

		void Close()
		{
			if (!open || !wsb.DefaultAnimation.HasSequence(info.CloseSequence))
				return;

			open = false;
			wsb.PlayCustomAnimation(self, info.CloseSequence);
		}

		void ITick.Tick(Actor self)
		{
			if (ShouldBeOpen())
				Open();
			else
				Close();
		}
	}
}
