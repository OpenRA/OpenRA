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

using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Actor becomes a specified actor type when this trait is triggered.")]
	public class TransformsInfo : ITraitInfo
	{
		[Desc("Actor to transform into."), ActorReference, FieldLoader.Require]
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

		[Desc("Cursor to display when able to (un)deploy the actor.")]
		public readonly string DeployCursor = "deploy";

		[Desc("Cursor to display when unable to (un)deploy the actor.")]
		public readonly string DeployBlockedCursor = "deploy-blocked";

		[VoiceReference] public readonly string Voice = "Action";

		public virtual object Create(ActorInitializer init) { return new Transforms(init, this); }
	}

	public class Transforms : IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly Actor self;
		readonly TransformsInfo info;
		readonly BuildingInfo buildingInfo;
		readonly string faction;

		public Transforms(ActorInitializer init, TransformsInfo info)
		{
			self = init.Self;
			this.info = info;
			buildingInfo = self.World.Map.Rules.Actors[info.IntoActor].TraitInfoOrDefault<BuildingInfo>();
			faction = init.Contains<FactionInit>() ? init.Get<FactionInit, string>() : self.Owner.Faction.InternalName;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "DeployTransform") ? info.Voice : null;
		}

		bool CanDeploy()
		{
			var building = self.TraitOrDefault<Building>();
			if (building != null && building.Locked)
				return false;

			return buildingInfo == null || self.World.CanPlaceBuilding(info.IntoActor, buildingInfo, self.Location + info.Offset, self);
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new DeployOrderTargeter("DeployTransform", 5,
				() => CanDeploy() ? info.DeployCursor : info.DeployBlockedCursor); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "DeployTransform")
				return new Order(order.OrderID, self, queued);

			return null;
		}

		public void DeployTransform(bool queued)
		{
			if (!queued && !CanDeploy())
			{
				// Only play the "Cannot deploy here" audio
				// for non-queued orders
				foreach (var s in info.NoTransformSounds)
					Game.Sound.PlayToPlayer(self.Owner, s);

				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.NoTransformNotification, self.Owner.Faction.InternalName);

				return;
			}

			if (!queued)
				self.CancelActivity();

			if (self.Info.HasTraitInfo<IFacingInfo>())
				self.QueueActivity(new Turn(self, info.Facing));

			if (self.Info.HasTraitInfo<AircraftInfo>())
				self.QueueActivity(new HeliLand(self, true));

			self.QueueActivity(new CallFunc(() =>
			{
				// Prevent deployment in bogus locations
				var building = self.TraitOrDefault<Building>();
				if (!CanDeploy() || (building != null && !building.Lock()))
					return;

				foreach (var nt in self.TraitsImplementing<INotifyTransform>())
					nt.BeforeTransform(self);

				var transform = new Transform(self, info.IntoActor)
				{
					Offset = info.Offset,
					Facing = info.Facing,
					Sounds = info.TransformSounds,
					Notification = info.TransformNotification,
					Faction = faction
				};

				var makeAnimation = self.TraitOrDefault<WithMakeAnimation>();
				if (makeAnimation != null)
					makeAnimation.Reverse(self, transform);
				else
					self.QueueActivity(transform);
			}));
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "DeployTransform")
				DeployTransform(order.Queued);
		}
	}
}
