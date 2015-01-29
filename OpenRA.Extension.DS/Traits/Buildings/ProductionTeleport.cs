// #region Copyright & License Information
// /*
//  * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
//  * This file is part of OpenRA, which is free software. It is made
//  * available to you under the terms of the GNU General Public License
//  * as published by the Free Software Foundation. For more information,
//  * see COPYING.
//  */
// #endregion

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Graphics;

namespace OpenRA.Extension.DS
{
	[Desc("Deliver the unit in production instantly via some mechanism.")]
	public class ProductionTeleportInfo : ProductionInfo
	{
		public readonly string ReadyAudio = "Reinforce";
		public readonly string EffectAudio = "chrono2.aud";
		public readonly bool ScreenFlash = false;

		public override object Create(ActorInitializer init) { return new ProductionTeleport(this, init.Self); }
	}

	public class ProductionTeleport : Production
	{
		public ProductionTeleport(ProductionTeleportInfo info, Actor self)
			: base(info, self) { }

		public override bool Produce(Actor self, ActorInfo producee, string raceVariant)
		{
			var info = (ProductionTeleportInfo)Info;
			var exit = self.Info.Traits.WithInterface<ExitInfo>().First();

			Sound.Play(info.EffectAudio, self.CenterPosition);
			Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ReadyAudio, self.Owner.Country.Race);

			if (info.ScreenFlash)
				foreach (var a in self.World.ActorsWithTrait<ChronoshiftPaletteEffect>())
					a.Trait.Enable();

			DoProduction(self, producee, exit, raceVariant);
			return true;
		}
	}
}

