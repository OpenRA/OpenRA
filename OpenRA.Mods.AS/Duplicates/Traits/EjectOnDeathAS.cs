#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Eject a ground soldier or a paratrooper while in the air. Carries over the veterancy level.")]
	public class EjectOnDeathASInfo : EjectOnDeathInfo
	{
		[Desc("Only spawn the pilot when there is a veterancy to carry over?")]
		public readonly bool SpawnOnlyWhenPromoted = true;

		public new object Create(ActorInitializer init) { return new EjectOnDeathAS(init.Self, this); }
	}

	class EjectOnDeathAS : INotifyKilled
	{
		readonly EjectOnDeathASInfo info;

		public EjectOnDeathAS(Actor self, EjectOnDeathASInfo info)
		{
			this.info = info;
		}

		public void Killed(Actor self, AttackInfo e)
		{
			if (self.Owner.WinState == WinState.Lost || !self.World.Map.Contains(self.Location))
				return;

			foreach (var condition in self.TraitsImplementing<IPreventsEjectOnDeath>())
				if (condition.PreventsEjectOnDeath(self))
					return;

			var r = self.World.SharedRandom.Next(1, 100);

			if (r <= 100 - info.SuccessRate)
				return;

			var cp = self.CenterPosition;
			var inAir = !self.IsAtGroundLevel();
			if ((inAir && !info.EjectInAir) || (!inAir && !info.EjectOnGround))
				return;

			var ge = self.TraitOrDefault<GainsExperience>();
			if ((ge == null || ge.Level == 0) && info.SpawnOnlyWhenPromoted)
				return;

			var pilot = self.World.CreateActor(false, info.PilotActor.ToLowerInvariant(),
				new TypeDictionary { new OwnerInit(self.Owner), new LocationInit(self.Location) });

			if (ge != null)
			{
				var pge = pilot.TraitOrDefault<GainsExperience>();
				if (pge != null)
				{
					pge.GiveLevels(ge.Level, true);
				}
			}

			if (info.AllowUnsuitableCell || IsSuitableCell(self, pilot))
			{
				if (inAir)
				{
					self.World.AddFrameEndTask(w =>
					{
						w.Add(pilot);
						pilot.QueueActivity(new Parachute(pilot, cp));
					});
					Game.Sound.Play(SoundType.World, info.ChuteSound, cp);
				}
				else
				{
					self.World.AddFrameEndTask(w => w.Add(pilot));
					var pilotMobile = pilot.TraitOrDefault<Mobile>();
					if (pilotMobile != null)
						pilotMobile.Nudge(pilot, pilot, true);
				}
			}
			else
				pilot.Dispose();
		}

		static bool IsSuitableCell(Actor self, Actor actorToDrop)
		{
			return actorToDrop.Trait<IPositionable>().CanEnterCell(self.Location, self, true);
		}
	}
}
