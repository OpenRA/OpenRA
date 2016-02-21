#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Activities
{
	public class Transform : Activity
	{
		public readonly string ToActor;
		public CVec Offset = CVec.Zero;
		public int Facing = 96;
		public string[] Sounds = { };
		public string Notification = null;
		public int ForceHealthPercentage = 0;
		public bool SkipMakeAnims = false;
		public string Faction = null;

		public Transform(Actor self, string toActor)
		{
			ToActor = toActor;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead)
					return;

				foreach (var nt in self.TraitsImplementing<INotifyTransform>())
					nt.OnTransform(self);

				var selected = w.Selection.Contains(self);
				var controlgroup = w.Selection.GetControlGroupForActor(self);

				self.Dispose();
				foreach (var s in Sounds)
					Game.Sound.PlayToPlayer(self.Owner, s, self.CenterPosition);

				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Notification, self.Owner.Faction.InternalName);

				var init = new TypeDictionary
				{
					new LocationInit(self.Location + Offset),
					new OwnerInit(self.Owner),
					new FacingInit(Facing),
				};

				if (SkipMakeAnims)
					init.Add(new SkipMakeAnimsInit());

				if (Faction != null)
					init.Add(new FactionInit(Faction));

				var health = self.TraitOrDefault<Health>();
				if (health != null)
				{
					var newHP = ForceHealthPercentage > 0 ? ForceHealthPercentage : (health.HP * 100) / health.MaxHP;
					init.Add(new HealthInit(newHP));
				}

				var cargo = self.TraitOrDefault<Cargo>();
				if (cargo != null)
					init.Add(new RuntimeCargoInit(cargo.Passengers.ToArray()));

				var a = w.CreateActor(ToActor, init);
				foreach (var nt in self.TraitsImplementing<INotifyTransform>())
					nt.AfterTransform(a);

				if (selected)
					w.Selection.Add(w, a);
				if (controlgroup.HasValue)
					w.Selection.AddToControlGroup(a, controlgroup.Value);
			});

			return this;
		}
	}
}
