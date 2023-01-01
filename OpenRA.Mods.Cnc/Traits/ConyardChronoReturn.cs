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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Implements the special case handling for the Chronoshiftable return on a construction yard.",
		"If ReturnOriginalActorOnCondition evaluates true and the actor is not being sold then OriginalActor will be returned to the origin.",
		"Otherwise, a vortex animation is played and damage is dealt each tick, ignoring modifiers.")]
	public class ConyardChronoReturnInfo : TraitInfo, Requires<HealthInfo>, Requires<WithSpriteBodyInfo>, IObservesVariablesInfo
	{
		[SequenceReference]
		[Desc("Sequence name with the baked-in vortex animation")]
		public readonly string Sequence = "pdox";

		[Desc("Sprite body to play the vortex animation on.")]
		public readonly string Body = "body";

		[GrantedConditionReference]
		[Desc("Condition to grant while the vortex animation plays.")]
		public readonly string Condition = null;

		[Desc("Amount of damage to apply each tick while the vortex animation plays.")]
		public readonly int Damage = 1000;

		[Desc("Apply the damage using these damagetypes.")]
		public readonly BitSet<DamageType> DamageTypes = default;

		[ConsumedConditionReference]
		[Desc("Boolean expression defining the condition under which to teleport a replacement actor instead of triggering the vortex.")]
		public readonly BooleanExpression ReturnOriginalActorOnCondition = null;

		[ActorReference(typeof(MobileInfo))]
		[Desc("Replacement actor to create when ReturnOriginalActorOnCondition evaluates true.")]
		public readonly string OriginalActor = "mcv";

		[Desc("Facing of the returned actor.")]
		public readonly WAngle Facing = new WAngle(384);

		public readonly string ChronoshiftSound = "chrono2.aud";

		[Desc("The color the bar of the 'return-to-origin' logic has.")]
		public readonly Color TimeBarColor = Color.White;

		public override object Create(ActorInitializer init) { return new ConyardChronoReturn(init, this); }
	}

	public class ConyardChronoReturn : ITick, ISync, IObservesVariables, ISelectionBar, INotifySold,
		IDeathActorInitModifier, ITransformActorInitModifier
	{
		readonly ConyardChronoReturnInfo info;
		readonly WithSpriteBody wsb;
		readonly Health health;
		readonly Actor self;
		readonly string faction;

		int conditionToken = Actor.InvalidConditionToken;

		Actor chronosphere;
		readonly int duration;
		bool returnOriginal;
		bool selling;

		[Sync]
		int returnTicks = 0;

		[Sync]
		readonly CPos origin;

		[Sync]
		bool triggered;

		public ConyardChronoReturn(ActorInitializer init, ConyardChronoReturnInfo info)
		{
			this.info = info;
			self = init.Self;

			health = self.Trait<Health>();

			wsb = self.TraitsImplementing<WithSpriteBody>().Single(w => w.Info.Name == info.Body);
			faction = init.GetValue<FactionInit, string>(self.Owner.Faction.InternalName);

			var returnInit = init.GetOrDefault<ChronoshiftReturnInit>();
			if (returnInit != null)
			{
				returnTicks = returnInit.Ticks;
				duration = returnInit.Duration;
				origin = returnInit.Origin;

				// Defer to the end of tick as the lazy value may reference an actor that hasn't been created yet
				if (returnInit.Chronosphere != null)
					init.World.AddFrameEndTask(w => chronosphere = returnInit.Chronosphere.Actor(init.World).Value);
			}
		}

		IEnumerable<VariableObserver> IObservesVariables.GetVariableObservers()
		{
			if (info.ReturnOriginalActorOnCondition != null)
				yield return new VariableObserver(ReplacementConditionChanged, info.ReturnOriginalActorOnCondition.Variables);
		}

		void ReplacementConditionChanged(Actor self, IReadOnlyDictionary<string, int> conditions)
		{
			returnOriginal = info.ReturnOriginalActorOnCondition.Evaluate(conditions);
		}

		void TriggerVortex()
		{
			if (conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(info.Condition);

			triggered = true;

			// Don't override the selling animation
			if (selling)
				return;

			wsb.PlayCustomAnimation(self, info.Sequence, () =>
			{
				triggered = false;
				if (conditionToken != Actor.InvalidConditionToken)
					conditionToken = self.RevokeCondition(conditionToken);
			});
		}

		CPos? ChooseBestDestinationCell(MobileInfo mobileInfo, CPos destination)
		{
			if (chronosphere == null)
				return null;

			if (mobileInfo.CanEnterCell(self.World, null, destination))
				return destination;

			var max = chronosphere.World.Map.Grid.MaximumTileSearchRange;
			foreach (var tile in self.World.Map.FindTilesInCircle(destination, max))
				if (chronosphere.Owner.Shroud.IsExplored(tile) && mobileInfo.CanEnterCell(self.World, null, tile))
					return tile;

			return null;
		}

		void ReturnToOrigin()
		{
			var selected = self.World.Selection.Contains(self);
			var controlgroup = self.World.ControlGroups.GetControlGroupForActor(self);
			var mobileInfo = self.World.Map.Rules.Actors[info.OriginalActor].TraitInfo<MobileInfo>();
			var destination = ChooseBestDestinationCell(mobileInfo, origin);

			// Give up if there is no destination
			// There's not much else we can do.
			if (destination == null)
				return;

			foreach (var nt in self.TraitsImplementing<INotifyTransform>())
				nt.OnTransform(self);

			var init = new TypeDictionary
			{
				new LocationInit(destination.Value),
				new OwnerInit(self.Owner),
				new FacingInit(info.Facing),
				new FactionInit(faction),
				new HealthInit((int)(health.HP * 100L / health.MaxHP))
			};

			foreach (var modifier in self.TraitsImplementing<ITransformActorInitModifier>())
				modifier.ModifyTransformActorInit(self, init);

			var a = self.World.CreateActor(info.OriginalActor, init);
			foreach (var nt in self.TraitsImplementing<INotifyTransform>())
				nt.AfterTransform(a);

			if (selected)
				self.World.Selection.Add(a);

			if (controlgroup.HasValue)
				self.World.ControlGroups.AddToControlGroup(a, controlgroup.Value);

			Game.Sound.Play(SoundType.World, info.ChronoshiftSound, self.World.Map.CenterOfCell(destination.Value));
			self.Dispose();
		}

		void ITick.Tick(Actor self)
		{
			if (self.WillDispose)
				return;

			if (triggered)
				health.InflictDamage(self, chronosphere, new Damage(info.Damage, info.DamageTypes), true);

			if (returnTicks <= 0 || --returnTicks > 0)
				return;

			if (returnOriginal && !selling)
				ReturnToOrigin();
			else
				TriggerVortex();

			// Trigger screen desaturate effect
			foreach (var cpa in self.World.ActorsWithTrait<ChronoshiftPaletteEffect>())
				cpa.Trait.Enable();

			Game.Sound.Play(SoundType.World, info.ChronoshiftSound, self.CenterPosition);

			if (chronosphere != null && self != chronosphere && !chronosphere.Disposed)
			{
				var building = chronosphere.TraitOrDefault<WithSpriteBody>();
				if (building != null && building.DefaultAnimation.HasSequence("active"))
					building.PlayCustomAnimation(chronosphere, "active");
			}
		}

		void ModifyActorInit(TypeDictionary init)
		{
			if (returnTicks <= 0)
				return;

			init.Add(new ChronoshiftReturnInit(returnTicks, duration, origin, chronosphere));
		}

		void IDeathActorInitModifier.ModifyDeathActorInit(Actor self, TypeDictionary init) { ModifyActorInit(init); }
		void ITransformActorInitModifier.ModifyTransformActorInit(Actor self, TypeDictionary init) { ModifyActorInit(init); }

		void INotifySold.Sold(Actor self) { }
		void INotifySold.Selling(Actor self)
		{
			selling = true;
		}

		// Show the remaining time as a bar
		float ISelectionBar.GetValue()
		{
			// Otherwise an empty bar is rendered all the time
			if (returnTicks == 0 || !self.Owner.IsAlliedWith(self.World.RenderPlayer))
				return 0f;

			return (float)returnTicks / duration;
		}

		Color ISelectionBar.GetColor() { return info.TimeBarColor; }
		bool ISelectionBar.DisplayWhenEmpty => false;
	}
}
