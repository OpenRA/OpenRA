#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Effects;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Effects;
using OpenRA.Primitives;

namespace OpenRA.Mods.RA
{
	public class SonarPulsePowerInfo : SupportPowerInfo
	{
		[Desc("Actor to spawn to reveal the submarines")]
		public readonly string SonarActor = "sonar";

		[Desc("Amount of time to keep the actor alive")]
		public readonly int SonarDuration = 250;

		public readonly string SonarPing = "sonpulse.aud";

		public override object Create(ActorInitializer init) { return new SonarPulsePower(init.self, this); }
	}

	public class SonarPulsePower : SupportPower
	{
		public SonarPulsePower(Actor self, SonarPulsePowerInfo info) : base(self, info) { }
		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			var info = Info as SonarPulsePowerInfo;

			if (info.SonarActor != null)
			{
				self.World.AddFrameEndTask(w =>
				{
					var sonar = w.CreateActor(info.SonarActor, new TypeDictionary
					{
						new LocationInit(order.TargetLocation),
						new OwnerInit(self.Owner),
					});
					Sound.Play(info.SonarPing, sonar.CenterPosition);
					w.Add(new SonarRipple(sonar.CenterPosition, w));
					sonar.QueueActivity(new Wait(info.SonarDuration));
					sonar.QueueActivity(new RemoveSelf());
				});
			}
		}
	}
}
