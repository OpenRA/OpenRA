#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Activities
{
	public class ChronoResourceTeleport : Activity
	{
		CPos destination;
		ChronoResourceDeliveryInfo info;

		public ChronoResourceTeleport(CPos destination, ChronoResourceDeliveryInfo info)
		{
			this.destination = destination;
			this.info = info;
		}

		public override Activity Tick(Actor self)
		{
			var image = info.Image ?? self.Info.Name;

			var sourcepos = self.CenterPosition;

			if (info.ChronoInSequence != null)
				self.World.AddFrameEndTask(w => w.Add(new SpriteEffect(sourcepos, w, image, info.ChronoInSequence, info.Palette)));

			if (info.ChronoInSound != null)
				Game.Sound.Play(info.ChronoInSound, sourcepos);

			self.Trait<IPositionable>().SetPosition(self, destination);
			self.Generation++;

			var destpos = self.CenterPosition;

			if (info.ChronoOutSequence != null)
				self.World.AddFrameEndTask(w => w.Add(new SpriteEffect(destpos, w, image, info.ChronoOutSequence, info.Palette)));

			if (info.ChronoOutSound != null)
				Game.Sound.Play(info.ChronoOutSound, destpos);

			return NextActivity;
		}
	}
}
