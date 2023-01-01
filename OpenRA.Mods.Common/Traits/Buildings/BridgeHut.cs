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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Allows bridges to be targeted for demolition and repair.")]
	public class BridgeHutInfo : TraitInfo, IDemolishableInfo
	{
		[Desc("Bridge types to act on")]
		public readonly string[] Types = { "GroundLevelBridge" };

		[Desc("Offsets to look for adjacent bridges to act on")]
		public readonly CVec[] NeighbourOffsets = Array.Empty<CVec>();

		[Desc("Delay between each segment repair step")]
		public readonly int RepairPropagationDelay = 20;

		[Desc("Delay between each segment demolish step")]
		public readonly int DemolishPropagationDelay = 5;

		[Desc("Hide the repair cursor if the bridge is only damaged (not destroyed)")]
		public readonly bool RequireForceAttackForHeal = false;

		public bool IsValidTarget(ActorInfo actorInfo, Actor saboteur) { return false; } // TODO: bridges don't support frozen under fog

		public override object Create(ActorInitializer init) { return new BridgeHut(init.World, this); }
	}

	public class BridgeHut : INotifyCreated, IDemolishable, ITick
	{
		public readonly BridgeHutInfo Info;
		readonly BridgeLayer bridgeLayer;

		// Fixed at map load
		readonly List<CPos[]> segmentLocations = new List<CPos[]>();

		// Changes as segments are killed and repaired
		readonly Dictionary<CPos, IBridgeSegment> segments = new Dictionary<CPos, IBridgeSegment>();
		readonly HashSet<CPos> dirtyLocations = new HashSet<CPos>();

		// Enabled during a repair action
		int repairStep;
		int repairDelay;
		Actor repairRepairer;

		// Enabled during a demolish action
		int demolishStep;
		int demolishDelay;
		Actor demolishSaboteur;
		BitSet<DamageType> demolishDamageTypes;

		public BridgeHut(World world, BridgeHutInfo info)
		{
			Info = info;
			bridgeLayer = world.WorldActor.Trait<BridgeLayer>();
		}

		void INotifyCreated.Created(Actor self)
		{
			self.World.AddFrameEndTask(w =>
			{
				// Bridge segments and huts are expected to be placed in the map
				// editor or spawned during the normal actor loading
				//
				// The number and location of bridge segments are calculated here,
				// and assumed to not change for the remaining lifetime of the world
				//
				// Bridge segment footprints and neighbour offsets are assumed to remain
				// the same when a segment is destroyed or repaired.
				var seed = Info.NeighbourOffsets.Select(v => self.Location + v);
				var processed = new HashSet<CPos>();
				while (true)
				{
					var step = NextNeighbourStep(seed, processed).ToList();
					if (step.Count == 0)
						break;

					foreach (var s in step)
						segments[s.Location] = s;

					segmentLocations.Add(step.Select(s => s.Location).ToArray());
					seed = step.SelectMany(s => s.NeighbourOffsets.Select(n => s.Location + n)).ToList();
				}

				repairStep = demolishStep = segmentLocations.Count;
			});
		}

		void ITick.Tick(Actor self)
		{
			// Update any dead segments
			dirtyLocations.Clear();
			foreach (var kv in segments)
				if (!kv.Value.Valid)
					dirtyLocations.Add(kv.Key);

			foreach (var c in dirtyLocations)
				segments[c] = bridgeLayer[c].TraitOrDefault<IBridgeSegment>();

			if (repairStep < segmentLocations.Count && --repairDelay <= 0)
				RepairStep();

			if (demolishStep < segmentLocations.Count && --demolishDelay <= 0)
				DemolishStep();
		}

		IEnumerable<IBridgeSegment> NextNeighbourStep(IEnumerable<CPos> seed, HashSet<CPos> processed)
		{
			foreach (var c in seed)
			{
				var bridge = bridgeLayer[c];
				if (bridge == null)
					continue;

				var segment = bridge.TraitOrDefault<IBridgeSegment>();
				if (segment != null && Info.Types.Contains(segment.Type) && processed.Add(segment.Location))
					yield return segment;
			}
		}

		public void Repair(Actor repairer)
		{
			if (Info.RepairPropagationDelay > 0)
			{
				repairStep = 0;
				repairRepairer = repairer;
				RepairStep();
			}
			else
				foreach (var s in segments.Values)
					s.Repair(repairer);
		}

		public void RepairStep()
		{
			// Find the next segment that needs to be repaired
			while (repairStep < segmentLocations.Count)
			{
				var stepDamage = segmentLocations[repairStep]
					.Select(c => segments[c])
					.Max(s => s.DamageState);

				if (stepDamage > DamageState.Undamaged)
					break;

				repairStep++;
			}

			if (repairStep < segmentLocations.Count)
				foreach (var c in segmentLocations[repairStep])
					segments[c].Repair(repairRepairer);

			repairDelay = Info.RepairPropagationDelay;
		}

		bool IDemolishable.IsValidTarget(Actor self, Actor saboteur)
		{
			return true;
		}

		void IDemolishable.Demolish(Actor self, Actor saboteur, int delay, BitSet<DamageType> damageTypes)
		{
			// TODO: Handle using ITick
			self.World.Add(new DelayedAction(delay, () =>
			{
				if (self.IsDead)
					return;

				var modifiers = self.TraitsImplementing<IDamageModifier>()
					.Concat(self.Owner.PlayerActor.TraitsImplementing<IDamageModifier>())
					.Select(t => t.GetDamageModifier(self, null));

				if (Util.ApplyPercentageModifiers(100, modifiers) > 0)
				{
					if (Info.DemolishPropagationDelay > 0)
					{
						demolishStep = 0;
						demolishSaboteur = saboteur;
						demolishDamageTypes = damageTypes;
						DemolishStep();
					}
					else
						foreach (var s in segments.Values)
							s.Demolish(saboteur, damageTypes);
				}
			}));
		}

		public void DemolishStep()
		{
			// Find the next segment to demolish
			while (demolishStep < segmentLocations.Count)
			{
				var stepDamage = segmentLocations[demolishStep]
					.Select(c => segments[c])
					.Max(s => s.DamageState);

				if (stepDamage < DamageState.Dead)
					break;

				demolishStep++;
			}

			if (demolishStep < segmentLocations.Count)
				foreach (var c in segmentLocations[demolishStep])
					segments[c].Demolish(demolishSaboteur, demolishDamageTypes);

			demolishDelay = Info.DemolishPropagationDelay;

			// Always advance at least one step (prevents sticking on placeholders)
			demolishStep++;
		}

		public DamageState BridgeDamageState
		{
			get
			{
				if (segments.Count == 0)
					return DamageState.Undamaged;

				return segments.Values.Max(s => s.DamageState);
			}
		}

		public bool Repairing => repairStep < segmentLocations.Count;
	}
}
