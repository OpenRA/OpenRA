#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Add to a building to expose a move cursor that triggers Transforms and issues a move order to the transformed actor.")]
	public class TransformsIntoMobileInfo : ConditionalTraitInfo, Requires<TransformsInfo>
	{
		[LocomotorReference]
		[FieldLoader.Require]
		[Desc("Locomotor used by the transformed actor. Must be defined on the World actor.")]
		public readonly string Locomotor = null;

		[CursorReference]
		[Desc("Cursor to display when a move order can be issued at target location.")]
		public readonly string Cursor = "move";

		[CursorReference(dictionaryReference: LintDictionaryReference.Values)]
		[Desc("Cursor overrides to display for specific terrain types.",
			"A dictionary of [terrain type]: [cursor name].")]
		public readonly Dictionary<string, string> TerrainCursors = new Dictionary<string, string>();

		[CursorReference]
		[Desc("Cursor to display when a move order cannot be issued at target location.")]
		public readonly string BlockedCursor = "move-blocked";

		[VoiceReference]
		public readonly string Voice = "Action";

		[Desc("Color to use for the target line for regular move orders.")]
		public readonly Color TargetLineColor = Color.Green;

		[Desc("Require the force-move modifier to display the move cursor.")]
		public readonly bool RequiresForceMove = false;

		public override object Create(ActorInitializer init) { return new TransformsIntoMobile(init, this); }

		public LocomotorInfo LocomotorInfo { get; private set; }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			var locomotorInfos = rules.Actors[SystemActors.World].TraitInfos<LocomotorInfo>();
			LocomotorInfo = locomotorInfos.FirstOrDefault(li => li.Name == Locomotor);
			if (LocomotorInfo == null)
				throw new YamlException($"A locomotor named '{Locomotor}' doesn't exist.");
			else if (locomotorInfos.Count(li => li.Name == Locomotor) > 1)
				throw new YamlException($"There is more than one locomotor named '{Locomotor}'.");

			base.RulesetLoaded(rules, ai);
		}
	}

	public class TransformsIntoMobile : ConditionalTrait<TransformsIntoMobileInfo>, IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly Actor self;
		Transforms[] transforms;
		Locomotor locomotor;

		public TransformsIntoMobile(ActorInitializer init, TransformsIntoMobileInfo info)
			: base(info)
		{
			self = init.Self;
		}

		protected override void Created(Actor self)
		{
			transforms = self.TraitsImplementing<Transforms>().ToArray();
			locomotor = self.World.WorldActor.TraitsImplementing<Locomotor>()
				.Single(l => l.Info.Name == Info.Locomotor);
			base.Created(self);
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				if (!IsTraitDisabled)
					yield return new MoveOrderTargeter(self, this);
			}
		}

		// Note: Returns a valid order even if the unit can't move to the target
		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order is MoveOrderTargeter)
				return new Order("Move", self, target, queued);

			return null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (IsTraitDisabled)
				return;

			if (order.OrderString == "Move")
			{
				var cell = self.World.Map.Clamp(this.self.World.Map.CellContaining(order.Target.CenterPosition));
				if (!Info.LocomotorInfo.MoveIntoShroud && !self.Owner.Shroud.IsExplored(cell))
					return;

				var currentTransform = self.CurrentActivity as Transform;
				var transform = transforms.FirstOrDefault(t => !t.IsTraitDisabled && !t.IsTraitPaused);
				if (transform == null && currentTransform == null)
					return;

				// Manually manage the inner activity queue
				var activity = currentTransform ?? transform.GetTransformActivity();
				if (!order.Queued)
					activity.NextActivity?.Cancel(self);

				activity.Queue(new IssueOrderAfterTransform("Move", order.Target, Info.TargetLineColor));

				if (currentTransform == null)
					self.QueueActivity(order.Queued, activity);

				self.ShowTargetLines();
			}
			else if (order.OrderString == "Stop")
			{
				// We don't want Stop orders from traits other than Mobile or Aircraft to cancel Resupply activity.
				// Resupply is always either the main activity or a child of ReturnToBase.
				// TODO: This should generally only cancel activities queued by this trait.
				if (self.CurrentActivity == null || self.CurrentActivity is Resupply || self.CurrentActivity is ReturnToBase)
					return;

				self.CancelActivity();
			}
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			if (IsTraitDisabled)
				return null;

			switch (order.OrderString)
			{
				case "Move":
					if (!Info.LocomotorInfo.MoveIntoShroud && order.Target.Type != TargetType.Invalid)
					{
						var cell = self.World.Map.CellContaining(order.Target.CenterPosition);
						if (!self.Owner.Shroud.IsExplored(cell))
							return null;
					}

					return Info.Voice;
				case "Stop":
					return Info.Voice;
				default:
					return null;
			}
		}

		class MoveOrderTargeter : IOrderTargeter
		{
			readonly TransformsIntoMobile mobile;
			readonly bool rejectMove;
			public bool TargetOverridesSelection(Actor self, in Target target, List<Actor> actorsAt, CPos xy, TargetModifiers modifiers)
			{
				// Always prioritise orders over selecting other peoples actors or own actors that are already selected
				if (target.Type == TargetType.Actor && (target.Actor.Owner != self.Owner || self.World.Selection.Contains(target.Actor)))
					return true;

				return modifiers.HasModifier(TargetModifiers.ForceMove);
			}

			public MoveOrderTargeter(Actor self, TransformsIntoMobile mobile)
			{
				this.mobile = mobile;
				rejectMove = !self.AcceptsOrder("Move");
			}

			public string OrderID => "Move";
			public int OrderPriority => 4;
			public bool IsQueued { get; protected set; }

			public bool CanTarget(Actor self, in Target target, ref TargetModifiers modifiers, ref string cursor)
			{
				if (rejectMove || target.Type != TargetType.Terrain || (mobile.Info.RequiresForceMove && !modifiers.HasModifier(TargetModifiers.ForceMove)))
					return false;

				var location = self.World.Map.CellContaining(target.CenterPosition);
				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				var explored = self.Owner.Shroud.IsExplored(location);
				if (!self.World.Map.Contains(location) ||
				    !(self.CurrentActivity is Transform || mobile.transforms.Any(t => !t.IsTraitDisabled && !t.IsTraitPaused))
				    || (!explored && !mobile.locomotor.Info.MoveIntoShroud)
				    || (explored && !CanEnterCell(self, location)))
					cursor = mobile.Info.BlockedCursor;
				else if (!explored || !mobile.Info.TerrainCursors.TryGetValue(self.World.Map.GetTerrainInfo(location).Type, out cursor))
					cursor = mobile.Info.Cursor;

				return true;
			}

			bool CanEnterCell(Actor self, CPos cell)
			{
				return mobile.locomotor.MovementCostToEnterCell(
					self, cell, BlockedByActor.All, null) != PathGraph.MovementCostForUnreachableCell;
			}
		}
	}
}
