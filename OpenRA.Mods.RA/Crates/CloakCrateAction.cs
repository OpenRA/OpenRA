#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Mods.RA;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Crates
{
	public class CloakCrateActionInfo : CrateActionInfo
	{
		public readonly int InitialDelay = 10;
		public readonly int CloakDelay = 30;
		public readonly string CloakSound = "subshow1.aud";
		public readonly string UncloakSound = "subshow1.aud";

		public override object Create(ActorInitializer init) { return new CloakCrateAction(init.self, this); }
	}

	public class CloakCrateAction : CrateAction
	{
		CloakCrateActionInfo Info;
		public CloakCrateAction(Actor self, CloakCrateActionInfo info)
			: base(self, info) { Info = info; }

		public override int GetSelectionShares(Actor collector)
		{
			return collector.HasTrait<AcceptsCloakCrate>() && !collector.HasTrait<Cloak>()
				? base.GetSelectionShares(collector) : 0;
		}

		public override void Activate(Actor collector)
		{
			var cloakInfo = new CloakInfo()
			{
				InitialDelay = Info.InitialDelay,
				CloakDelay = Info.CloakDelay,
				CloakSound = Info.CloakSound,
				UncloakSound = Info.UncloakSound
			};
			var cloak = new Cloak(collector, cloakInfo);

			collector.World.AddFrameEndTask(w =>
				{
					w.Remove(collector);

					collector.AddTrait(cloak);
					var t = collector.TraitOrDefault<TargetableUnit>();
					if (t != null) t.ReceivedCloak(collector);

					w.Add(collector);
				});

			base.Activate(collector);
		}
	}

	public class AcceptsCloakCrateInfo : TraitInfo<AcceptsCloakCrate> {}
	public class AcceptsCloakCrate {}
}
