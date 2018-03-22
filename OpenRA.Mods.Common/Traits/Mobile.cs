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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Flags]
	public enum CellConditions
	{
		None = 0,
		TransientActors,
		BlockedByMovers,
		All = TransientActors | BlockedByMovers
	}

	public static class CellConditionsExts
	{
		public static bool HasCellCondition(this CellConditions c, CellConditions cellCondition)
		{
			// PERF: Enum.HasFlag is slower and requires allocations.
			return (c & cellCondition) == cellCondition;
		}
	}

	public static class CustomMovementLayerType
	{
		public const byte Tunnel = 1;
		public const byte Subterranean = 2;
		public const byte Jumpjet = 3;
		public const byte ElevatedBridge = 4;
	}

	[Desc("Unit is able to move.")]
	public class MobileInfo : ConditionalTraitInfo, IMoveInfo, IPositionableInfo, IFacingInfo,
		UsesInit<FacingInit>, UsesInit<LocationInit>, UsesInit<SubCellInit>, IActorPreviewInitInfo
	{
		[FieldLoader.LoadUsing("LoadSpeeds", true)]
		[Desc("Set Water: 0 for ground units and lower the value on rough terrain.")]
		public readonly Dictionary<string, TerrainInfo> TerrainSpeeds;

		[Desc("e.g. crate, wall, infantry")]
		public readonly HashSet<string> Crushes = new HashSet<string>();

		[Desc("Types of damage that are caused while crushing. Leave empty for no damage types.")]
		public readonly HashSet<string> CrushDamageTypes = new HashSet<string>();

		public readonly int WaitAverage = 5;

		public readonly int WaitSpread = 2;

		public readonly int InitialFacing = 0;

		[Desc("Speed at which the actor turns.")]
		public readonly int TurnSpeed = 255;

		public readonly int Speed = 1;

		[Desc("Allow multiple (infantry) units in one cell.")]
		public readonly bool SharesCell = false;

		[Desc("Can the actor be ordered to move in to shroud?")]
		public readonly bool MoveIntoShroud = true;

		public readonly string Cursor = "move";
		public readonly string BlockedCursor = "move-blocked";

		[VoiceReference] public readonly string Voice = "Action";

		[GrantedConditionReference]
		[Desc("The condition to grant to self while inside a tunnel.")]
		public readonly string TunnelCondition = null;

		[Desc("Can this unit move underground?")]
		public readonly bool Subterranean = false;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while underground.")]
		public readonly string SubterraneanCondition = null;

		[Desc("Pathfinding cost for submerging or reemerging.")]
		public readonly int SubterraneanTransitionCost = 0;

		[Desc("The terrain types that this actor can transition on. Leave empty to allow any.")]
		public readonly HashSet<string> SubterraneanTransitionTerrainTypes = new HashSet<string>();

		[Desc("Can this actor transition on slopes?")]
		public readonly bool SubterraneanTransitionOnRamps = false;

		[Desc("Depth at which the subterranian condition is applied.")]
		public readonly WDist SubterraneanTransitionDepth = new WDist(-1024);

		[Desc("Dig animation image to play when transitioning.")]
		public readonly string SubterraneanTransitionImage = null;

		[SequenceReference("SubterraneanTransitionImage")]
		[Desc("Dig animation image to play when transitioning.")]
		public readonly string SubterraneanTransitionSequence = null;

		[PaletteReference]
		public readonly string SubterraneanTransitionPalette = "effect";

		public readonly string SubterraneanTransitionSound = null;

		[Desc("Can this unit fly over obstacles?")]
		public readonly bool Jumpjet = false;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while flying.")]
		public readonly string JumpjetCondition = null;

		[Desc("Pathfinding cost for taking off or landing.")]
		public readonly int JumpjetTransitionCost = 0;

		[Desc("The terrain types that this actor can transition on. Leave empty to allow any.")]
		public readonly HashSet<string> JumpjetTransitionTerrainTypes = new HashSet<string>();

		[Desc("Can this actor transition on slopes?")]
		public readonly bool JumpjetTransitionOnRamps = true;

		[Desc("Facing to use for actor previews (map editor, color picker, etc)")]
		public readonly int PreviewFacing = 92;

		IEnumerable<object> IActorPreviewInitInfo.ActorPreviewInits(ActorInfo ai, ActorPreviewType type)
		{
			yield return new FacingInit(PreviewFacing);
		}

		public override object Create(ActorInitializer init) { return new Mobile(init, this); }

		static object LoadSpeeds(MiniYaml y)
		{
			var ret = new Dictionary<string, TerrainInfo>();
			foreach (var t in y.ToDictionary()["TerrainSpeeds"].Nodes)
			{
				var speed = FieldLoader.GetValue<int>("speed", t.Value.Value);
				var nodesDict = t.Value.ToDictionary();
				var cost = nodesDict.ContainsKey("PathingCost")
					? FieldLoader.GetValue<int>("cost", nodesDict["PathingCost"].Value)
					: 10000 / speed;
				ret.Add(t.Key, new TerrainInfo(speed, cost));
			}

			return ret;
		}

		TerrainInfo[] LoadTilesetSpeeds(TileSet tileSet)
		{
			var info = new TerrainInfo[tileSet.TerrainInfo.Length];
			for (var i = 0; i < info.Length; i++)
				info[i] = TerrainInfo.Impassable;

			foreach (var kvp in TerrainSpeeds)
			{
				byte index;
				if (tileSet.TryGetTerrainIndex(kvp.Key, out index))
					info[index] = kvp.Value;
			}

			return info;
		}

		public class TerrainInfo
		{
			public static readonly TerrainInfo Impassable = new TerrainInfo();

			public readonly int Cost;
			public readonly int Speed;

			public TerrainInfo()
			{
				Cost = int.MaxValue;
				Speed = 0;
			}

			public TerrainInfo(int speed, int cost)
			{
				Speed = speed;
				Cost = cost;
			}
		}

		public struct WorldMovementInfo
		{
			internal readonly World World;
			internal readonly TerrainInfo[] TerrainInfos;
			internal WorldMovementInfo(World world, MobileInfo info)
			{
				// PERF: This struct allows us to cache the terrain info for the tileset used by the world.
				// This allows us to speed up some performance-sensitive pathfinding calculations.
				World = world;
				TerrainInfos = info.TilesetTerrainInfo[world.Map.Rules.TileSet];
			}
		}

		public readonly Cache<TileSet, TerrainInfo[]> TilesetTerrainInfo;
		public readonly Cache<TileSet, int> TilesetMovementClass;

		public MobileInfo()
		{
			TilesetTerrainInfo = new Cache<TileSet, TerrainInfo[]>(LoadTilesetSpeeds);
			TilesetMovementClass = new Cache<TileSet, int>(CalculateTilesetMovementClass);
		}

		public int MovementCostForCell(World world, CPos cell)
		{
			return MovementCostForCell(world, TilesetTerrainInfo[world.Map.Rules.TileSet], cell);
		}

		int MovementCostForCell(World world, TerrainInfo[] terrainInfos, CPos cell)
		{
			if (!world.Map.Contains(cell))
				return int.MaxValue;

			var index = cell.Layer == 0 ? world.Map.GetTerrainIndex(cell) :
				world.GetCustomMovementLayers()[cell.Layer].GetTerrainIndex(cell);

			if (index == byte.MaxValue)
				return int.MaxValue;

			return terrainInfos[index].Cost;
		}

		public int CalculateTilesetMovementClass(TileSet tileset)
		{
			// collect our ability to cross *all* terraintypes, in a bitvector
			return TilesetTerrainInfo[tileset].Select(ti => ti.Cost < int.MaxValue).ToBits();
		}

		public int GetMovementClass(TileSet tileset)
		{
			return TilesetMovementClass[tileset];
		}

		static bool IsMovingInMyDirection(Actor self, Actor other)
		{
			var otherMobile = other.TraitOrDefault<Mobile>();
			if (otherMobile == null || !otherMobile.IsMoving)
				return false;

			var selfMobile = self.TraitOrDefault<Mobile>();
			if (selfMobile == null)
				return false;

			// Moving in the same direction if the facing delta is between +/- 90 degrees
			var delta = Util.NormalizeFacing(otherMobile.Facing - selfMobile.Facing);
			return delta < 64 || delta > 192;
		}

		public int TileSetMovementHash(TileSet tileSet)
		{
			var terrainInfos = TilesetTerrainInfo[tileSet];

			// Compute and return the hash using aggregate
			return terrainInfos.Aggregate(terrainInfos.Length,
				(current, terrainInfo) => unchecked(current * 31 + terrainInfo.Cost));
		}

		public bool CanEnterCell(World world, Actor self, CPos cell, Actor ignoreActor = null, bool checkTransientActors = true)
		{
			if (MovementCostForCell(world, cell) == int.MaxValue)
				return false;

			var check = checkTransientActors ? CellConditions.All : CellConditions.BlockedByMovers;
			return CanMoveFreelyInto(world, self, cell, ignoreActor, check);
		}

		// Determines whether the actor is blocked by other Actors
		public bool CanMoveFreelyInto(World world, Actor self, CPos cell, Actor ignoreActor, CellConditions check)
		{
			if (!check.HasCellCondition(CellConditions.TransientActors))
				return true;

			if (SharesCell && world.ActorMap.HasFreeSubCell(cell))
				return true;

			// PERF: Avoid LINQ.
			foreach (var otherActor in world.ActorMap.GetActorsAt(cell))
				if (IsBlockedBy(self, otherActor, ignoreActor, check))
					return false;

			return true;
		}

		bool IsBlockedBy(Actor self, Actor otherActor, Actor ignoreActor, CellConditions check)
		{
			// We are not blocked by the actor we are ignoring.
			if (otherActor == ignoreActor)
				return false;

			// If self is null, we don't have a real actor - we're just checking what would happen theoretically.
			// In such a scenario - we'll just assume any other actor in the cell will block us by default.
			// If we have a real actor, we can then perform the extra checks that allow us to avoid being blocked.
			if (self == null)
				return true;

			// If the check allows: we are not blocked by allied units moving in our direction.
			if (!check.HasCellCondition(CellConditions.BlockedByMovers) &&
				self.Owner.Stances[otherActor.Owner] == Stance.Ally &&
				IsMovingInMyDirection(self, otherActor))
				return false;

			// If there is a temporary blocker in our path, but we can remove it, we are not blocked.
			var temporaryBlocker = otherActor.TraitOrDefault<ITemporaryBlocker>();
			if (temporaryBlocker != null && temporaryBlocker.CanRemoveBlockage(otherActor, self))
				return false;

			// If we cannot crush the other actor in our way, we are blocked.
			if (Crushes == null || Crushes.Count == 0)
				return true;

			// If the other actor in our way cannot be crushed, we are blocked.
			// PERF: Avoid LINQ.
			var crushables = otherActor.TraitsImplementing<ICrushable>();
			foreach (var crushable in crushables)
				if (crushable.CrushableBy(otherActor, self, Crushes))
					return false;

			return true;
		}

		public WorldMovementInfo GetWorldMovementInfo(World world)
		{
			return new WorldMovementInfo(world, this);
		}

		public int MovementCostToEnterCell(WorldMovementInfo worldMovementInfo, Actor self, CPos cell, Actor ignoreActor = null, CellConditions check = CellConditions.All)
		{
			var cost = MovementCostForCell(worldMovementInfo.World, worldMovementInfo.TerrainInfos, cell);
			if (cost == int.MaxValue || !CanMoveFreelyInto(worldMovementInfo.World, self, cell, ignoreActor, check))
				return int.MaxValue;
			return cost;
		}

		public SubCell GetAvailableSubCell(
			World world, Actor self, CPos cell, SubCell preferredSubCell = SubCell.Any, Actor ignoreActor = null, CellConditions check = CellConditions.All)
		{
			if (MovementCostForCell(world, cell) == int.MaxValue)
				return SubCell.Invalid;

			if (check.HasCellCondition(CellConditions.TransientActors))
			{
				Func<Actor, bool> checkTransient = otherActor => IsBlockedBy(self, otherActor, ignoreActor, check);

				if (!SharesCell)
					return world.ActorMap.AnyActorsAt(cell, SubCell.FullCell, checkTransient) ? SubCell.Invalid : SubCell.FullCell;

				return world.ActorMap.FreeSubCell(cell, preferredSubCell, checkTransient);
			}

			if (!SharesCell)
				return world.ActorMap.AnyActorsAt(cell, SubCell.FullCell) ? SubCell.Invalid : SubCell.FullCell;

			return world.ActorMap.FreeSubCell(cell, preferredSubCell);
		}

		public int GetInitialFacing() { return InitialFacing; }

		public IReadOnlyDictionary<CPos, SubCell> OccupiedCells(ActorInfo info, CPos location, SubCell subCell = SubCell.Any)
		{
			return new ReadOnlyDictionary<CPos, SubCell>(new Dictionary<CPos, SubCell>() { { location, subCell } });
		}

		bool IOccupySpaceInfo.SharesCell { get { return SharesCell; } }
	}

	public class Mobile : ConditionalTrait<MobileInfo>, INotifyCreated, IIssueOrder, IResolveOrder, IOrderVoice, IPositionable, IMove,
		IFacing, IDeathActorInitModifier, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyBlockingMove, IActorPreviewInitModifier, INotifyBecomingIdle
	{
		const int AverageTicksBeforePathing = 5;
		const int SpreadTicksBeforePathing = 5;
		internal int TicksBeforePathing = 0;

		readonly Actor self;
		readonly Lazy<IEnumerable<int>> speedModifiers;
		public bool IsMoving { get; set; }
		public bool IsMovingVertically { get { return false; } set { } }

		int facing;
		CPos fromCell, toCell;
		public SubCell FromSubCell, ToSubCell;
		int tunnelToken = ConditionManager.InvalidConditionToken;
		int subterraneanToken = ConditionManager.InvalidConditionToken;
		int jumpjetToken = ConditionManager.InvalidConditionToken;
		ConditionManager conditionManager;

		[Sync] public int Facing
		{
			get { return facing; }
			set { facing = value; }
		}

		public int TurnSpeed { get { return Info.TurnSpeed; } }

		[Sync] public WPos CenterPosition { get; private set; }
		[Sync] public CPos FromCell { get { return fromCell; } }
		[Sync] public CPos ToCell { get { return toCell; } }

		[Sync] public int PathHash;	// written by Move.EvalPath, to temporarily debug this crap.

		// Sets only the location (fromCell, toCell, FromSubCell, ToSubCell)
		public void SetLocation(CPos from, SubCell fromSub, CPos to, SubCell toSub)
		{
			if (FromCell == from && ToCell == to && FromSubCell == fromSub && ToSubCell == toSub)
				return;

			RemoveInfluence();
			fromCell = from;
			toCell = to;
			FromSubCell = fromSub;
			ToSubCell = toSub;
			AddInfluence();

			// Tunnel condition is added/removed when starting the transition between layers
			if (toCell.Layer == CustomMovementLayerType.Tunnel && conditionManager != null &&
					!string.IsNullOrEmpty(Info.TunnelCondition) && tunnelToken == ConditionManager.InvalidConditionToken)
				tunnelToken = conditionManager.GrantCondition(self, Info.TunnelCondition);
			else if (toCell.Layer != CustomMovementLayerType.Tunnel && tunnelToken != ConditionManager.InvalidConditionToken)
				tunnelToken = conditionManager.RevokeCondition(self, tunnelToken);

			// Play submerging animation as soon as it starts to submerge (before applying the condition)
			if (toCell.Layer == CustomMovementLayerType.Subterranean && fromCell.Layer != CustomMovementLayerType.Subterranean)
			{
				if (!string.IsNullOrEmpty(Info.SubterraneanTransitionSequence))
					self.World.AddFrameEndTask(w => w.Add(new SpriteEffect(self.World.Map.CenterOfCell(fromCell), self.World, Info.SubterraneanTransitionImage,
						Info.SubterraneanTransitionSequence, Info.SubterraneanTransitionPalette)));

				if (!string.IsNullOrEmpty(Info.SubterraneanTransitionSound))
					Game.Sound.Play(SoundType.World, Info.SubterraneanTransitionSound);
			}

			// Grant the jumpjet condition as soon as the actor starts leaving the ground layer
			// The condition is revoked from FinishedMoving
			if (toCell.Layer == CustomMovementLayerType.Jumpjet && conditionManager != null &&
					!string.IsNullOrEmpty(Info.JumpjetCondition) && jumpjetToken == ConditionManager.InvalidConditionToken)
				jumpjetToken = conditionManager.GrantCondition(self, Info.JumpjetCondition);
		}

		public Mobile(ActorInitializer init, MobileInfo info)
			: base(info)
		{
			self = init.Self;

			speedModifiers = Exts.Lazy(() => self.TraitsImplementing<ISpeedModifier>().ToArray().Select(x => x.GetSpeedModifier()));

			ToSubCell = FromSubCell = info.SharesCell ? init.World.Map.Grid.DefaultSubCell : SubCell.FullCell;
			if (init.Contains<SubCellInit>())
				FromSubCell = ToSubCell = init.Get<SubCellInit, SubCell>();

			if (init.Contains<LocationInit>())
			{
				fromCell = toCell = init.Get<LocationInit, CPos>();
				SetVisualPosition(self, init.World.Map.CenterOfSubCell(FromCell, FromSubCell));
			}

			Facing = init.Contains<FacingInit>() ? init.Get<FacingInit, int>() : info.InitialFacing;

			// Sets the visual position to WPos accuracy
			// Use LocationInit if you want to insert the actor into the ActorMap!
			if (init.Contains<CenterPositionInit>())
				SetVisualPosition(self, init.Get<CenterPositionInit, WPos>());
		}

		protected override void Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();

			base.Created(self);
		}

		// Returns a valid sub-cell
		public SubCell GetValidSubCell(SubCell preferred = SubCell.Any)
		{
			// Try same sub-cell
			if (preferred == SubCell.Any)
				preferred = FromSubCell;

			// Fix sub-cell assignment
			if (Info.SharesCell)
			{
				if (preferred <= SubCell.FullCell)
					return self.World.Map.Grid.DefaultSubCell;
			}
			else
			{
				if (preferred != SubCell.FullCell)
					return SubCell.FullCell;
			}

			return preferred;
		}

		// Sets the location (fromCell, toCell, FromSubCell, ToSubCell) and visual position (CenterPosition)
		public void SetPosition(Actor self, CPos cell, SubCell subCell = SubCell.Any)
		{
			subCell = GetValidSubCell(subCell);
			SetLocation(cell, subCell, cell, subCell);

			var position = cell.Layer == 0 ? self.World.Map.CenterOfCell(cell) :
				self.World.GetCustomMovementLayers()[cell.Layer].CenterOfCell(cell);

			var subcellOffset = self.World.Map.Grid.OffsetOfSubCell(subCell);
			SetVisualPosition(self, position + subcellOffset);
			FinishedMoving(self);
		}

		// Sets the location (fromCell, toCell, FromSubCell, ToSubCell) and visual position (CenterPosition)
		public void SetPosition(Actor self, WPos pos)
		{
			var cell = self.World.Map.CellContaining(pos);
			SetLocation(cell, FromSubCell, cell, FromSubCell);
			SetVisualPosition(self, self.World.Map.CenterOfSubCell(cell, FromSubCell) + new WVec(0, 0, self.World.Map.DistanceAboveTerrain(pos).Length));
			FinishedMoving(self);
		}

		// Sets only the visual position (CenterPosition)
		public void SetVisualPosition(Actor self, WPos pos)
		{
			CenterPosition = pos;
			self.World.UpdateMaps(self, this);

			// HACK: The submerging conditions must be applied part way through a move, and this is the only method that gets called
			// at the right times to detect this
			if (toCell.Layer == CustomMovementLayerType.Subterranean)
			{
				var depth = self.World.Map.DistanceAboveTerrain(self.CenterPosition);
				if (subterraneanToken == ConditionManager.InvalidConditionToken && depth < Info.SubterraneanTransitionDepth && conditionManager != null
						&& !string.IsNullOrEmpty(Info.SubterraneanCondition))
					subterraneanToken = conditionManager.GrantCondition(self, Info.SubterraneanCondition);
			}
			else if (subterraneanToken != ConditionManager.InvalidConditionToken)
			{
				var depth = self.World.Map.DistanceAboveTerrain(self.CenterPosition);
				if (depth > Info.SubterraneanTransitionDepth)
				{
					subterraneanToken = conditionManager.RevokeCondition(self, subterraneanToken);

					// HACK: the submerging animation and sound won't play if a condition isn't defined
					if (!string.IsNullOrEmpty(Info.SubterraneanTransitionSound))
						Game.Sound.Play(SoundType.World, Info.SubterraneanTransitionSound);

					if (!string.IsNullOrEmpty(Info.SubterraneanTransitionSequence))
						self.World.AddFrameEndTask(w => w.Add(new SpriteEffect(self.World.Map.CenterOfCell(fromCell), self.World, Info.SubterraneanTransitionImage,
							Info.SubterraneanTransitionSequence, Info.SubterraneanTransitionPalette)));
				}
			}
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			self.World.AddToMaps(self, this);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.RemoveFromMaps(self, this);
		}

		public IEnumerable<IOrderTargeter> Orders { get { yield return new MoveOrderTargeter(self, this); } }

		// Note: Returns a valid order even if the unit can't move to the target
		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order is MoveOrderTargeter)
				return new Order("Move", self, target, queued);

			return null;
		}

		public CPos NearestMoveableCell(CPos target)
		{
			// Limit search to a radius of 10 tiles
			return NearestMoveableCell(target, 1, 10);
		}

		public CPos NearestMoveableCell(CPos target, int minRange, int maxRange)
		{
			// HACK: This entire method is a hack, and needs to be replaced with
			// a proper path search that can account for movement layer transitions.
			// HACK: Work around code that blindly tries to move to cells in invalid movement layers.
			// This will need to change (by removing this method completely as above) before we can
			// properly support user-issued orders on to elevated bridges or other interactable custom layers
			if (target.Layer != 0)
				target = new CPos(target.X, target.Y);

			if (CanEnterCell(target))
				return target;

			foreach (var tile in self.World.Map.FindTilesInAnnulus(target, minRange, maxRange))
				if (CanEnterCell(tile))
					return tile;

			// Couldn't find a cell
			return target;
		}

		public CPos NearestCell(CPos target, Func<CPos, bool> check, int minRange, int maxRange)
		{
			if (check(target))
				return target;

			foreach (var tile in self.World.Map.FindTilesInAnnulus(target, minRange, maxRange))
				if (check(tile))
					return tile;

			// Couldn't find a cell
			return target;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Move")
			{
				var loc = self.World.Map.Clamp(order.TargetLocation);

				if (!Info.MoveIntoShroud && !self.Owner.Shroud.IsExplored(loc))
					return;

				if (!order.Queued)
					self.CancelActivity();

				TicksBeforePathing = AverageTicksBeforePathing + self.World.SharedRandom.Next(-SpreadTicksBeforePathing, SpreadTicksBeforePathing);

				self.SetTargetLine(Target.FromCell(self.World, loc), Color.Green);
				self.QueueActivity(order.Queued, new Move(self, loc, WDist.FromCells(8), null, true));
			}

			if (order.OrderString == "Stop")
				self.CancelActivity();

			if (order.OrderString == "Scatter")
				Nudge(self, self, true);
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (!Info.MoveIntoShroud && !self.Owner.Shroud.IsExplored(order.TargetLocation))
				return null;

			switch (order.OrderString)
			{
				case "Move":
				case "Scatter":
				case "Stop":
					return Info.Voice;
				default:
					return null;
			}
		}

		public CPos TopLeft { get { return ToCell; } }

		public Pair<CPos, SubCell>[] OccupiedCells()
		{
			if (FromCell == ToCell)
				return new[] { Pair.New(FromCell, FromSubCell) };
			if (CanEnterCell(ToCell))
				return new[] { Pair.New(ToCell, ToSubCell) };
			return new[] { Pair.New(FromCell, FromSubCell), Pair.New(ToCell, ToSubCell) };
		}

		public bool IsLeavingCell(CPos location, SubCell subCell = SubCell.Any)
		{
			return ToCell != location && fromCell == location
				&& (subCell == SubCell.Any || FromSubCell == subCell || subCell == SubCell.FullCell || FromSubCell == SubCell.FullCell);
		}

		public SubCell GetAvailableSubCell(CPos a, SubCell preferredSubCell = SubCell.Any, Actor ignoreActor = null, bool checkTransientActors = true)
		{
			return Info.GetAvailableSubCell(self.World, self, a, preferredSubCell, ignoreActor, checkTransientActors ? CellConditions.All : CellConditions.None);
		}

		public bool CanEnterCell(CPos cell, Actor ignoreActor = null, bool checkTransientActors = true)
		{
			return Info.CanEnterCell(self.World, self, cell, ignoreActor, checkTransientActors);
		}

		public bool CanMoveFreelyInto(CPos cell, Actor ignoreActor = null, bool checkTransientActors = true)
		{
			return Info.CanMoveFreelyInto(self.World, self, cell, ignoreActor, checkTransientActors ? CellConditions.All : CellConditions.BlockedByMovers);
		}

		public void EnteringCell(Actor self)
		{
			// Only make actor crush if it is on the ground
			if (!self.IsAtGroundLevel())
				return;

			var actors = self.World.ActorMap.GetActorsAt(ToCell).Where(a => a != self).ToList();
			if (!AnyCrushables(actors))
				return;

			var notifiers = actors.SelectMany(a => a.TraitsImplementing<INotifyCrushed>().Select(t => new TraitPair<INotifyCrushed>(a, t)));
			foreach (var notifyCrushed in notifiers)
				notifyCrushed.Trait.WarnCrush(notifyCrushed.Actor, self, Info.Crushes);
		}

		public void FinishedMoving(Actor self)
		{
			// Need to check both fromCell and toCell because FinishedMoving is called multiple times during the move
			// and that condition guarantees that this only runs when the unit has finished landing.
			if (fromCell.Layer != CustomMovementLayerType.Jumpjet && toCell.Layer != CustomMovementLayerType.Jumpjet && jumpjetToken != ConditionManager.InvalidConditionToken)
				jumpjetToken = conditionManager.RevokeCondition(self, jumpjetToken);

			// Only make actor crush if it is on the ground
			if (!self.IsAtGroundLevel())
				return;

			var actors = self.World.ActorMap.GetActorsAt(ToCell).Where(a => a != self).ToList();
			if (!AnyCrushables(actors))
				return;

			var notifiers = actors.SelectMany(a => a.TraitsImplementing<INotifyCrushed>().Select(t => new TraitPair<INotifyCrushed>(a, t)));
			foreach (var notifyCrushed in notifiers)
				notifyCrushed.Trait.OnCrush(notifyCrushed.Actor, self, Info.Crushes);
		}

		bool AnyCrushables(List<Actor> actors)
		{
			var crushables = actors.SelectMany(a => a.TraitsImplementing<ICrushable>().Select(t => new TraitPair<ICrushable>(a, t))).ToList();
			if (crushables.Count == 0)
				return false;

			foreach (var crushes in crushables)
				if (crushes.Trait.CrushableBy(crushes.Actor, self, Info.Crushes))
					return true;

			return false;
		}

		public int MovementSpeedForCell(Actor self, CPos cell)
		{
			var index = cell.Layer == 0 ? self.World.Map.GetTerrainIndex(cell) :
				self.World.GetCustomMovementLayers()[cell.Layer].GetTerrainIndex(cell);

			if (index == byte.MaxValue)
				return 0;

			var terrainSpeed = Info.TilesetTerrainInfo[self.World.Map.Rules.TileSet][index].Speed;
			if (terrainSpeed == 0)
				return 0;

			var modifiers = speedModifiers.Value.Append(terrainSpeed);

			return Util.ApplyPercentageModifiers(Info.Speed, modifiers);
		}

		public void AddInfluence()
		{
			if (self.IsInWorld)
				self.World.ActorMap.AddInfluence(self, this);
		}

		public void RemoveInfluence()
		{
			if (self.IsInWorld)
				self.World.ActorMap.RemoveInfluence(self, this);
		}

		public void Nudge(Actor self, Actor nudger, bool force)
		{
			if (IsTraitDisabled)
				return;

			/* initial fairly braindead implementation. */
			if (!force && self.Owner.Stances[nudger.Owner] != Stance.Ally)
				return;		/* don't allow ourselves to be pushed around
							 * by the enemy! */

			if (!force && !self.IsIdle)
				return;		/* don't nudge if we're busy doing something! */

			// pick an adjacent available cell.
			var availCells = new List<CPos>();
			var notStupidCells = new List<CPos>();

			for (var i = -1; i < 2; i++)
				for (var j = -1; j < 2; j++)
				{
					var p = ToCell + new CVec(i, j);
					if (CanEnterCell(p))
						availCells.Add(p);
					else if (p != nudger.Location && p != ToCell)
						notStupidCells.Add(p);
				}

			var moveTo = availCells.Any() ? availCells.Random(self.World.SharedRandom) : (CPos?)null;

			if (moveTo.HasValue)
			{
				self.CancelActivity();
				self.SetTargetLine(Target.FromCell(self.World, moveTo.Value), Color.Green, false);
				self.QueueActivity(new Move(self, moveTo.Value, WDist.Zero));

				Log.Write("debug", "OnNudge #{0} from {1} to {2}",
					self.ActorID, self.Location, moveTo.Value);
			}
			else
			{
				var cellInfo = notStupidCells
					.SelectMany(c => self.World.ActorMap.GetActorsAt(c)
						.Where(a => a.IsIdle && a.Info.HasTraitInfo<MobileInfo>()),
						(c, a) => new { Cell = c, Actor = a })
					.RandomOrDefault(self.World.SharedRandom);

				if (cellInfo != null)
				{
					self.CancelActivity();
					var notifyBlocking = new CallFunc(() => self.NotifyBlocker(cellInfo.Cell));
					var waitFor = new WaitFor(() => CanEnterCell(cellInfo.Cell));
					var move = new Move(self, cellInfo.Cell);
					self.QueueActivity(ActivityUtils.SequenceActivities(notifyBlocking, waitFor, move));

					Log.Write("debug", "OnNudge (notify next blocking actor, wait and move) #{0} from {1} to {2}",
						self.ActorID, self.Location, cellInfo.Cell);
				}
				else
				{
					Log.Write("debug", "OnNudge #{0} refuses at {1}",
						self.ActorID, self.Location);
				}
			}
		}

		public bool CanInteractWithGroundLayer(Actor self)
		{
			// TODO: Think about extending this to support arbitrary layer-layer checks
			// in a way that is compatible with the other IMove types.
			// This would then allow us to e.g. have units attack other units inside tunnels.
			if (ToCell.Layer == 0)
				return true;

			ICustomMovementLayer layer;
			if (self.World.GetCustomMovementLayers().TryGetValue(ToCell.Layer, out layer))
				return layer.InteractsWithDefaultLayer;

			return true;
		}

		void IActorPreviewInitModifier.ModifyActorPreviewInit(Actor self, TypeDictionary inits)
		{
			if (!inits.Contains<DynamicFacingInit>() && !inits.Contains<FacingInit>())
				inits.Add(new DynamicFacingInit(() => facing));
		}

		class MoveOrderTargeter : IOrderTargeter
		{
			readonly Mobile mobile;
			readonly bool rejectMove;
			public bool TargetOverridesSelection(TargetModifiers modifiers)
			{
				return modifiers.HasModifier(TargetModifiers.ForceMove);
			}

			public MoveOrderTargeter(Actor self, Mobile unit)
			{
				mobile = unit;
				rejectMove = !self.AcceptsOrder("Move");
			}

			public string OrderID { get { return "Move"; } }
			public int OrderPriority { get { return 4; } }
			public bool IsQueued { get; protected set; }

			public bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, ref TargetModifiers modifiers, ref string cursor)
			{
				if (rejectMove || target.Type != TargetType.Terrain)
					return false;

				var location = self.World.Map.CellContaining(target.CenterPosition);
				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				var explored = self.Owner.Shroud.IsExplored(location);
				cursor = self.World.Map.Contains(location) ?
					(self.World.Map.GetTerrainInfo(location).CustomCursor ?? mobile.Info.Cursor) : mobile.Info.BlockedCursor;

				if (mobile.IsTraitDisabled
					|| (!explored && !mobile.Info.MoveIntoShroud)
					|| (explored && mobile.Info.MovementCostForCell(self.World, location) == int.MaxValue))
					cursor = mobile.Info.BlockedCursor;

				return true;
			}
		}

		public Activity ScriptedMove(CPos cell) { return new Move(self, cell); }
		public Activity MoveTo(CPos cell, int nearEnough) { return new Move(self, cell, WDist.FromCells(nearEnough)); }
		public Activity MoveTo(CPos cell, Actor ignoreActor) { return new Move(self, cell, WDist.Zero, ignoreActor); }
		public Activity MoveWithinRange(Target target, WDist range) { return new MoveWithinRange(self, target, WDist.Zero, range); }
		public Activity MoveWithinRange(Target target, WDist minRange, WDist maxRange) { return new MoveWithinRange(self, target, minRange, maxRange); }
		public Activity MoveFollow(Actor self, Target target, WDist minRange, WDist maxRange) { return new Follow(self, target, minRange, maxRange); }
		public Activity MoveTo(Func<List<CPos>> pathFunc) { return new Move(self, pathFunc); }

		void INotifyBlockingMove.OnNotifyBlockingMove(Actor self, Actor blocking)
		{
			if (self.IsIdle && self.AppearsFriendlyTo(blocking))
				Nudge(self, blocking, true);
		}

		public Activity MoveIntoWorld(Actor self, CPos cell, SubCell subCell = SubCell.Any)
		{
			var pos = self.CenterPosition;

			if (subCell == SubCell.Any)
				subCell = Info.SharesCell ? self.World.ActorMap.FreeSubCell(cell, subCell) : SubCell.FullCell;

			// TODO: solve/reduce cell is full problem
			if (subCell == SubCell.Invalid)
				subCell = self.World.Map.Grid.DefaultSubCell;

			// Reserve the exit cell
			SetPosition(self, cell, subCell);
			SetVisualPosition(self, pos);

			return VisualMove(self, pos, self.World.Map.CenterOfSubCell(cell, subCell), cell);
		}

		public Activity MoveToTarget(Actor self, Target target)
		{
			if (target.Type == TargetType.Invalid)
				return null;

			return new MoveAdjacentTo(self, target);
		}

		public Activity MoveIntoTarget(Actor self, Target target)
		{
			if (target.Type == TargetType.Invalid)
				return null;

			return VisualMove(self, self.CenterPosition, target.Positions.PositionClosestTo(self.CenterPosition));
		}

		public bool CanEnterTargetNow(Actor self, Target target)
		{
			return self.Location == self.World.Map.CellContaining(target.CenterPosition) || Util.AdjacentCells(self.World, target).Any(c => c == self.Location);
		}

		public Activity VisualMove(Actor self, WPos fromPos, WPos toPos)
		{
			return VisualMove(self, fromPos, toPos, self.Location);
		}

		public Activity VisualMove(Actor self, WPos fromPos, WPos toPos, CPos cell)
		{
			var speed = MovementSpeedForCell(self, cell);
			var length = speed > 0 ? (toPos - fromPos).Length / speed : 0;

			var delta = toPos - fromPos;
			var facing = delta.HorizontalLengthSquared != 0 ? delta.Yaw.Facing : Facing;
			return ActivityUtils.SequenceActivities(new Turn(self, facing), new Drag(self, fromPos, toPos, length));
		}

		public void ModifyDeathActorInit(Actor self, TypeDictionary init)
		{
			init.Add(new FacingInit(facing));

			// Allows the husk to drag to its final position
			if (CanEnterCell(self.Location, self, false))
				init.Add(new HuskSpeedInit(MovementSpeedForCell(self, self.Location)));
		}

		CPos? ClosestGroundCell()
		{
			var above = new CPos(TopLeft.X, TopLeft.Y);
			if (CanEnterCell(above))
				return above;

			var pathFinder = self.World.WorldActor.Trait<IPathFinder>();
			List<CPos> path;
			using (var search = PathSearch.Search(self.World, Info, self, true,
					loc => loc.Layer == 0 && CanEnterCell(loc))
				.FromPoint(self.Location))
				path = pathFinder.FindPath(search);

			if (path.Count > 0)
				return path[0];

			return null;
		}

		void INotifyBecomingIdle.OnBecomingIdle(Actor self)
		{
			if (TopLeft.Layer == 0)
				return;

			var moveTo = ClosestGroundCell();
			if (moveTo != null)
				self.QueueActivity(MoveTo(moveTo.Value, 0));
		}
	}
}
