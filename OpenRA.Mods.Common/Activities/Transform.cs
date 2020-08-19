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

using System.Collections.Generic;
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
		public WAngle Facing = new WAngle(384);
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
				QueueChild(new Turn(self, Facing));

			if (self.Info.HasTraitInfo<AircraftInfo>())
				QueueChild(new Land(self));
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling)
				return true;

			// Prevent deployment in bogus locations
			var transforms = self.TraitOrDefault<Transforms>();
			if (transforms != null && !transforms.CanDeploy())
				return true;

			foreach (var nt in self.TraitsImplementing<INotifyTransform>())
				nt.BeforeTransform(self);

			var makeAnimation = self.TraitOrDefault<WithMakeAnimation>();
			if (!SkipMakeAnims && makeAnimation != null)
			{
				// Once the make animation starts the activity must not be stopped anymore.
				IsInterruptible = false;

				// Wait forever
				QueueChild(new WaitFor(() => false));
				makeAnimation.Reverse(self, () => DoTransform(self));
				return false;
			}

			DoTransform(self);
			return true;
		}

		void DoTransform(Actor self)
		{
			// This activity may be buried as a child within one or more parents
			// We need to consider the top-level activities when transferring orders to the new actor!
			var currentActivity = self.CurrentActivity;

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

				// Use self.CurrentActivity to capture the parent activity if Transform is a child
				foreach (var transfer in currentActivity.ActivitiesImplementing<IssueOrderAfterTransform>(false))
				{
					if (transfer.IsCanceling)
						continue;

					var order = transfer.IssueOrderForTransformedActor(a);
					foreach (var t in a.TraitsImplementing<IResolveOrder>())
						t.ResolveOrder(a, order);
				}

				self.ReplacedByActor = a;

				if (selected)
					w.Selection.Add(a);

				if (controlgroup.HasValue)
					w.Selection.AddToControlGroup(a, controlgroup.Value);
			});
		}
	}

	class IssueOrderAfterTransform : Activity
	{
		readonly string orderString;
		readonly Target target;
		readonly Color? targetLineColor;

		public IssueOrderAfterTransform(string orderString, in Target target, Color? targetLineColor = null)
		{
			this.orderString = orderString;
			this.target = target;
			this.targetLineColor = targetLineColor;
		}

		public Order IssueOrderForTransformedActor(Actor newActor)
		{
			return new Order(orderString, newActor, target, true);
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (targetLineColor != null)
				yield return new TargetLineNode(target, targetLineColor.Value);
		}
	}
}
