#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

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

		protected override void OnFirstRun(Actor self)
		{
			if (self.Info.HasTraitInfo<IFacingInfo>())
				QueueChild(self, new Turn(self, Facing));

			if (self.Info.HasTraitInfo<AircraftInfo>())
				QueueChild(self, new Land(self));
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceling)
				return NextActivity;

			if (ChildActivity != null)
			{
				ActivityUtils.RunActivity(self, ChildActivity);
				return this;
			}

			// Prevent deployment in bogus locations
			var transforms = self.TraitOrDefault<Transforms>();
			if (transforms != null && !transforms.CanDeploy())
			{
				Cancel(self, true);
				return NextActivity;
			}

			foreach (var nt in self.TraitsImplementing<INotifyTransform>())
				nt.BeforeTransform(self);

			var makeAnimation = self.TraitOrDefault<WithMakeAnimation>();
			if (!SkipMakeAnims && makeAnimation != null)
			{
				// Once the make animation starts the activity must not be stopped anymore.
				IsInterruptible = false;

				// Wait forever
				QueueChild(self, new WaitFor(() => false));
				makeAnimation.Reverse(self, () => DoTransform(self));
				return this;
			}

			return NextActivity;
		}

		protected override void OnLastRun(Actor self)
		{
			if (!IsCanceling)
				DoTransform(self);
		}

		void DoTransform(Actor self)
		{
			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead || self.WillDispose)
					return;

				foreach (var nt in self.TraitsImplementing<INotifyTransform>())
					nt.OnTransform(self);

				var selected = w.Selection.Contains(self);
				var controlgroup = w.Selection.GetControlGroupForActor(self);

				self.Dispose();
				foreach (var s in Sounds)
					Game.Sound.PlayToPlayer(SoundType.World, self.Owner, s, self.CenterPosition);

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

				var health = self.TraitOrDefault<IHealth>();
				if (health != null)
				{
					// Cast to long to avoid overflow when multiplying by the health
					var newHP = ForceHealthPercentage > 0 ? ForceHealthPercentage : (int)(health.HP * 100L / health.MaxHP);
					init.Add(new HealthInit(newHP));
				}

				foreach (var modifier in self.TraitsImplementing<ITransformActorInitModifier>())
					modifier.ModifyTransformActorInit(self, init);

				var a = w.CreateActor(ToActor, init);
				foreach (var nt in self.TraitsImplementing<INotifyTransform>())
					nt.AfterTransform(a);

				self.ReplacedByActor = a;

				if (selected)
					w.Selection.Add(a);

				if (controlgroup.HasValue)
					w.Selection.AddToControlGroup(a, controlgroup.Value);
			});
		}
	}
}
