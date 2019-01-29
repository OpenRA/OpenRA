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

using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Actor becomes a specified actor type when this trait is triggered.")]
	public class TransformsInfo : PausableConditionalTraitInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Actor to transform into.")]
		public readonly string IntoActor = null;

		[Desc("Offset to spawn the transformed actor relative to the current cell.")]
		public readonly CVec Offset = CVec.Zero;

		[Desc("Facing that the actor must face before transforming.")]
		public readonly int Facing = 96;

		[Desc("Sounds to play when transforming.")]
		public readonly string[] TransformSounds = { };

		[Desc("Sounds to play when the transformation is blocked.")]
		public readonly string[] NoTransformSounds = { };

		[NotificationReference("Speech")]
		[Desc("Notification to play when transforming.")]
		public readonly string TransformNotification = null;

		[NotificationReference("Speech")]
		[Desc("Notification to play when the transformation is blocked.")]
		public readonly string NoTransformNotification = null;

		[Desc("Cursor to display when able to (un)deploy the actor.")]
		public readonly string DeployCursor = "deploy";

		[Desc("Cursor to display when unable to (un)deploy the actor.")]
		public readonly string DeployBlockedCursor = "deploy-blocked";

		[VoiceReference]
		public readonly string Voice = "Action";

		[Desc("Can this actor be ordered to move when deployed? [Always, ForceOnly, Never]")]
		public readonly UndeployOnMoveType UndeployOnMove = UndeployOnMoveType.Always;

		public override object Create(ActorInitializer init) { return new Transforms(init, this); }
	}

	public class Transforms : PausableConditionalTrait<TransformsInfo>, IIssueOrder, IResolveOrder, IOrderVoice, IIssueDeployOrder,
		INotifyCreated
	{
		readonly Actor self;
		readonly ActorInfo actorInfo;
		readonly BuildingInfo buildingInfo;
		public readonly string Faction;
		readonly Activity initialActivity;

		public Transforms(ActorInitializer init, TransformsInfo info)
			: base(info)
		{
			self = init.Self;
			actorInfo = self.World.Map.Rules.Actors[info.IntoActor];
			buildingInfo = actorInfo.TraitInfoOrDefault<BuildingInfo>();
			Faction = init.Contains<FactionInit>() ? init.Get<FactionInit, string>() : self.Owner.Faction.InternalName;
			if (init.Contains<ActivityInit>())
				initialActivity = init.Get<ActivityInit, Activity>();
		}

		protected override void Created(Actor self)
		{
			self.QueueActivity(initialActivity);
			base.Created(self);
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "DeployTransform" ? Info.Voice : null;
		}

		public bool CanDeploy()
		{
			if (IsTraitPaused || IsTraitDisabled)
				return false;

			return buildingInfo == null || self.World.CanPlaceBuilding(self.Location + Info.Offset, actorInfo, buildingInfo, self);
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				if (!IsTraitDisabled)
					yield return new MoveDeployOrderTargeter(self, actorInfo, this);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order is MoveDeployOrderTargeter)
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
		{
			return new Order("DeployTransform", self, queued);
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self) { return !IsTraitDisabled && !IsTraitPaused; }

		public void DeployTransform(bool queued, Target target)
		{
			if (!queued && !CanDeploy() && !(target.Type == TargetType.Terrain || (target.Type == TargetType.Actor && target.Actor != self)))
			{
				// Only play the "Cannot deploy here" audio
				// for non-queued orders
				foreach (var s in Info.NoTransformSounds)
					Game.Sound.PlayToPlayer(SoundType.World, self.Owner, s);

				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.NoTransformNotification, self.Owner.Faction.InternalName);

				return;
			}

			self.QueueActivity(queued, new Transform(self, target));
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "DeployTransform" || IsTraitDisabled || IsTraitPaused)
				return;

			self.SetTargetLine(order.Target, Color.Green);
			DeployTransform(order.Queued, order.Target);
		}

		class MoveDeployOrderTargeter : IOrderTargeter
		{
			readonly IMoveInfo info;
			readonly bool rejectMove;
			readonly Transforms unit;

			public bool TargetOverridesSelection(TargetModifiers modifiers)
			{
				return modifiers.HasModifier(TargetModifiers.ForceMove);
			}

			public MoveDeployOrderTargeter(Actor self, ActorInfo otherActorInfo, Transforms unit)
			{
				this.unit = unit;
				info = self.Info.TraitInfoOrDefault<IMoveInfo>();
				if (info == null)
					 info = otherActorInfo.TraitInfoOrDefault<IMoveInfo>();

				rejectMove = !self.AcceptsOrder("Move");
			}

			public string OrderID { get { return "DeployTransform"; } }
			public int OrderPriority { get { return 5; } }
			public bool IsQueued { get; protected set; }

			public bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, ref TargetModifiers modifiers, ref string cursor)
			{
				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				if (target.Type == TargetType.Actor)
				{
					cursor = unit.CanDeploy() ? unit.Info.DeployCursor : unit.Info.DeployBlockedCursor;
					return self == target.Actor;
				}

				if (rejectMove || unit.Info.UndeployOnMove == UndeployOnMoveType.Never ||
				    info == null || target.Type != TargetType.Terrain)
					return false;

				if (unit.Info.UndeployOnMove == UndeployOnMoveType.ForceOnly && !modifiers.HasModifier(TargetModifiers.ForceMove))
					return false;

				var location = self.World.Map.CellContaining(target.CenterPosition);
				var explored = self.Owner.Shroud.IsExplored(location);
				cursor = self.World.Map.Contains(location) ?
					(self.World.Map.GetTerrainInfo(location).CustomCursor ?? "move") : "move-blocked";

				if (unit.IsTraitPaused
					|| (!explored && info != null && !info.CanMoveIntoShroud())
					|| (explored && info != null && !info.CanMoveInCell(self.World, self, location, null, false)))
					cursor = "move-blocked";

				return true;
			}
		}
	}

	public class ActivityInit : IActorInit<Activity>
	{
		[FieldFromYamlKey] readonly Activity value = null;
		public ActivityInit() { }
		public ActivityInit(Activity init) { value = init; }
		public Activity Value(World world) { return value; }
	}
}
