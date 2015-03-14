#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Actor becomes a specified actor type when this trait is triggered.")]
	public class TransformsInfo : ITraitInfo
	{
		[Desc("Actor to transform into."), ActorReference]
		public readonly string IntoActor = null;

		[Desc("Offset to spawn the transformed actor relative to the current cell.")]
		public readonly CVec Offset = CVec.Zero;

		[Desc("Facing that the actor must face before transforming.")]
		public readonly int Facing = 96;

		[Desc("Sounds to play when transforming.")]
		public readonly string[] TransformSounds = { };

		[Desc("Sounds to play when the transformation is blocked.")]
		public readonly string[] NoTransformSounds = { };

		[Desc("Notification to play when transforming.")]
		public readonly string TransformNotification = null;

		[Desc("Notification to play when the transformation is blocked.")]
		public readonly string NoTransformNotification = null;

		public virtual object Create(ActorInitializer init) { return new Transforms(init, this); }
	}

	public class Transforms : IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly Actor self;
		readonly TransformsInfo info;
		readonly Building building;
		readonly BuildingInfo buildingInfo;
		readonly string race;
		readonly WithMakeAnimation makeAnimation;

		public Transforms(ActorInitializer init, TransformsInfo info)
		{
			self = init.Self;
			this.info = info;
			buildingInfo = self.World.Map.Rules.Actors[info.IntoActor].Traits.GetOrDefault<BuildingInfo>();
			building = self.TraitOrDefault<Building>();
			makeAnimation = self.TraitOrDefault<WithMakeAnimation>();
			race = init.Contains<RaceInit>() ? init.Get<RaceInit, string>() : self.Owner.Country.Race;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "DeployTransform") ? "Move" : null;
		}

		bool CanDeploy()
		{
			if (building != null && building.Locked)
				return false;

			return buildingInfo == null || self.World.CanPlaceBuilding(info.IntoActor, buildingInfo, self.Location + info.Offset, self);
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new DeployOrderTargeter("DeployTransform", 5, () => CanDeploy()); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "DeployTransform")
				return new Order(order.OrderID, self, queued);

			return null;
		}

		public void DeployTransform(bool queued)
		{
			if (!CanDeploy() || (building != null && !building.Lock()))
			{
				foreach (var s in info.NoTransformSounds)
					Sound.PlayToPlayer(self.Owner, s);

				Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.NoTransformNotification, self.Owner.Country.Race);

				return;
			}

			if (!queued)
				self.CancelActivity();

			if (self.HasTrait<IFacing>())
				self.QueueActivity(new Turn(self, info.Facing));

			foreach (var nt in self.TraitsImplementing<INotifyTransform>())
				nt.BeforeTransform(self);

			var transform = new Transform(self, info.IntoActor)
			{
				Offset = info.Offset,
				Facing = info.Facing,
				Sounds = info.TransformSounds,
				Notification = info.TransformNotification,
				Race = race
			};

			if (makeAnimation != null)
				makeAnimation.Reverse(self, transform);
			else
				self.QueueActivity(transform);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "DeployTransform")
				DeployTransform(order.Queued);
		}
	}
}
