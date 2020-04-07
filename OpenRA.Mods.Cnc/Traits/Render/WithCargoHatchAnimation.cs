#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits.Render
{
	public class WithCargoHatchAnimationInfo : ITraitInfo, Requires<IFacingInfo>, Requires<WithSpriteBodyInfo>
	{
		public readonly int RequiredFacing = 0;

		[SequenceReference]
		public readonly string OpenSequence = "open";

		[SequenceReference]
		public readonly string CloseSequence = "close";

		[SequenceReference]
		public readonly string UnloadSequence = "unload";

		[Desc("Which sprite body to play the animation on.")]
		public readonly string Body = "body";

		public object Create(ActorInitializer init) { return new WithCargoHatchAnimation(init, this); }
	}

	public class WithCargoHatchAnimation : ITick
	{
		readonly WithCargoHatchAnimationInfo info;
		readonly Actor self;
		readonly IFacing facing;
		readonly WithSpriteBody body;
		bool open;

		public WithCargoHatchAnimation(ActorInitializer init, WithCargoHatchAnimationInfo info)
		{
			this.info = info;
			self = init.Self;
			facing = self.Trait<IFacing>();
			body = init.Self.TraitsImplementing<WithSpriteBody>().Single(w => w.Info.Name == info.Body);
		}

		public bool ShouldBeOpen()
		{
			if (self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length > 0)
				return false;

			return facing.Facing == info.RequiredFacing;
		}

		void Open()
		{
			if (open || !body.DefaultAnimation.HasSequence(info.OpenSequence))
				return;

			open = true;
			body.PlayCustomAnimation(self, info.OpenSequence, () =>
			{
				if (body.DefaultAnimation.HasSequence(info.UnloadSequence))
					body.PlayCustomAnimationRepeating(self, info.UnloadSequence);
			});
		}

		void Close()
		{
			if (!open || !body.DefaultAnimation.HasSequence(info.CloseSequence))
				return;

			open = false;
			body.PlayCustomAnimation(self, info.CloseSequence);
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
