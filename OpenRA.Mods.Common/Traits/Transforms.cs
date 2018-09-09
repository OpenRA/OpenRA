#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Actor becomes a specified actor type when this trait is triggered.")]
	public class TransformsInfo : PausableConditionalTraitInfo
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

		public override object Create(ActorInitializer init) { return new Transforms(init, this); }
	}

	public class Transforms : PausableConditionalTrait<TransformsInfo>, IIssueOrder, IResolveOrder, IOrderVoice, IIssueDeployOrder
	{
		readonly Actor self;
		readonly ActorInfo actorInfo;
		readonly BuildingInfo buildingInfo;
		readonly string faction;

		public Transforms(ActorInitializer init, TransformsInfo info)
			: base(info)
		{
			self = init.Self;
			actorInfo = self.World.Map.Rules.Actors[info.IntoActor];
			buildingInfo = actorInfo.TraitInfoOrDefault<BuildingInfo>();
			faction = init.Contains<FactionInit>() ? init.Get<FactionInit, string>() : self.Owner.Faction.InternalName;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "DeployTransform") ? Info.Voice : null;
		}

		public bool CanDeploy()
		{
			if (IsTraitPaused || IsTraitDisabled)
				return false;

			var building = self.TraitOrDefault<Building>();
			if (building != null && building.Locked)
				return false;

			return buildingInfo == null || self.World.CanPlaceBuilding(self.Location + Info.Offset, actorInfo, buildingInfo, self);
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				if (!IsTraitDisabled)
					yield return new DeployOrderTargeter("DeployTransform", 5,
						() => CanDeploy() ? Info.DeployCursor : Info.DeployBlockedCursor);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "DeployTransform")
				return new Order(order.OrderID, self, queued);

			return null;
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
		{
			return new Order("DeployTransform", self, queued);
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self) { return !IsTraitPaused && !IsTraitDisabled; }

		public void DeployTransform(bool queued)
		{
			if (!queued && !CanDeploy())
			{
				// Only play the "Cannot deploy here" audio
				// for non-queued orders
				foreach (var s in Info.NoTransformSounds)
					Game.Sound.PlayToPlayer(SoundType.World, self.Owner, s);

				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.NoTransformNotification, self.Owner.Faction.InternalName);

				return;
			}

			if (!queued)
				self.CancelActivity();

			self.QueueActivity(new Transform(self, Info.IntoActor)
			{
				Offset = Info.Offset,
				Facing = Info.Facing,
				Sounds = Info.TransformSounds,
				Notification = Info.TransformNotification,
				Faction = faction
			});
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "DeployTransform" && !IsTraitPaused && !IsTraitDisabled)
				DeployTransform(order.Queued);
		}
	}
}
