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
using System.Drawing;
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
	public class ConyardChronoReturnInfo : IObservesVariablesInfo, Requires<HealthInfo>, Requires<WithSpriteBodyInfo>
	{
		[Desc("Sequence name with the baked-in vortex animation"), SequenceReference]
		public readonly string Sequence = "pdox";

		[Desc("Sprite body to play the vortex animation on.")]
		public readonly string Body = "body";

		[GrantedConditionReference]
		[Desc("Condition to grant while the vortex animation plays.")]
		public readonly string Condition = null;

		[Desc("Amount of damage to apply each tick while the vortex animation plays.")]
		public readonly int Damage = 1000;

		[Desc("Apply the damage using these damagetypes.")]
		public readonly BitSet<DamageType> DamageTypes = default(BitSet<DamageType>);

		[ConsumedConditionReference]
		[Desc("Boolean expression defining the condition under which to teleport a replacement actor instead of triggering the vortex.")]
		public readonly BooleanExpression ReturnOriginalActorOnCondition = null;

		[ActorReference(typeof(MobileInfo))]
		[Desc("Replacement actor to create when ReturnOriginalActorOnCondition evaluates true.")]
		public readonly string OriginalActor = "mcv";

		[Desc("Facing of the returned actor.")]
		public readonly int Facing = 96;

		public readonly string ChronoshiftSound = "chrono2.aud";

		[Desc("The color the bar of the 'return-to-origin' logic has.")]
		public readonly Color TimeBarColor = Color.White;

		public object Create(ActorInitializer init) { return new ConyardChronoReturn(init, this); }
	}

	public class ConyardChronoReturn : INotifyCreated, ITick, ISync, IObservesVariables, ISelectionBar, INotifySold,
		IDeathActorInitModifier, ITransformActorInitModifier
	{
		readonly ConyardChronoReturnInfo info;
		readonly WithSpriteBody wsb;
		readonly Health health;
		readonly Actor self;
		readonly string faction;

		ConditionManager conditionManager;
		int conditionToken = ConditionManager.InvalidConditionToken;

		Actor chronosphere;
		int duration;
		bool returnOriginal;
		bool selling;

		[Sync]
		int returnTicks = 0;

		[Sync]
		CPos origin;

		[Sync]
		bool triggered;

		public ConyardChronoReturn(ActorInitializer init, ConyardChronoReturnInfo info)
		{
			this.info = info;
			self = init.Self;

			health = self.Trait<Health>();

			wsb = self.TraitsImplementing<WithSpriteBody>().Single(w => w.Info.Name == info.Body);
			faction = init.Contains<FactionInit>() ? init.Get<FactionInit, string>() : self.Owner.Faction.InternalName;

			if (init.Contains<ChronoshiftReturnInit>())
				returnTicks = init.Get<ChronoshiftReturnInit, int>();

			if (init.Contains<ChronoshiftDurationInit>())
				duration = init.Get<ChronoshiftDurationInit, int>();

			if (init.Contains<ChronoshiftOriginInit>())
				origin = init.Get<ChronoshiftOriginInit, CPos>();

			if (init.Contains<ChronoshiftChronosphereInit>())
				chronosphere = init.Get<ChronoshiftChronosphereInit, Actor>();
		}

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
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
			if (conditionManager != null && !string.IsNullOrEmpty(info.Condition) && conditionToken == ConditionManager.InvalidConditionToken)
				conditionToken = conditionManager.GrantCondition(self, info.Condition);

			triggered = true;

			// Don't override the selling animation
			if (selling)
				return;

			wsb.PlayCustomAnimation(self, info.Sequence, () =>
			{
				triggered = false;
				if (conditionToken != ConditionManager.InvalidConditionToken)
					conditionToken = conditionManager.RevokeCondition(self, conditionToken);
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
			var controlgroup = self.World.Selection.GetControlGroupForActor(self);
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
				self.World.Selection.Add(self.World, a);

			if (controlgroup.HasValue)
				self.World.Selection.AddToControlGroup(a, controlgroup.Value);

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

			init.Add(new ChronoshiftOriginInit(origin));
			init.Add(new ChronoshiftReturnInit(returnTicks));
			init.Add(new ChronoshiftDurationInit(duration));
			if (chronosphere != self)
				init.Add(new ChronoshiftChronosphereInit(chronosphere));
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
		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }
	}
}
