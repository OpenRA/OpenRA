#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Primitives;

namespace OpenRA.Traits
{
	// depends on the order of pips in WorldRenderer.cs!
	public enum PipType { Transparent, Green, Yellow, Red, Gray, Blue, Ammo, AmmoEmpty };
	public enum TagType { None, Fake, Primary };
	
	[Flags]
	public enum Stance
	{
		Enemy = 1,
		Neutral = 2,
		Ally = 4,
	}

	[Flags]
	public enum ImpactType
	{
		None = 0,
		Ground = 1,
		Water = 2,
		Air = 4,
		GroundHit = 8,
		WaterHit = 16,
		AirHit = 32
	}

	public class AttackInfo
	{
		public Actor Attacker;
		public DamageWarhead Warhead;
		public int Damage;
		public DamageState DamageState;
		public DamageState PreviousDamageState;
	}

	public interface ITick { void Tick(Actor self); }
	public interface ITickRender { void TickRender(WorldRenderer wr, Actor self); }
	public interface IRender { IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr); }
	public interface IAutoSelectionSize { int2 SelectionSize(Actor self); }

	public interface IIssueOrder
	{
		IEnumerable<IOrderTargeter> Orders { get; }
		Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued);
	}

	[Flags] public enum TargetModifiers { None = 0, ForceAttack = 1, ForceQueue = 2, ForceMove = 4 };

	public static class TargetModifiersExts
	{
		public static bool HasModifier(this TargetModifiers self, TargetModifiers m) { return (self & m) == m; }
	}

	public interface IOrderTargeter
	{
		string OrderID { get; }
		int OrderPriority { get; }
		bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, TargetModifiers modifiers, ref string cursor);
		bool IsQueued { get; }
	}

	public interface IResolveOrder { void ResolveOrder(Actor self, Order order); }
	public interface IValidateOrder { bool OrderValidation(OrderManager orderManager, World world, int clientId, Order order); }
	public interface IOrderVoice { string VoicePhraseForOrder(Actor self, Order order); }
	public interface INotify { void Play(Player p, string notification); }
	public interface INotifyCreated { void Created(Actor self); }
	public interface INotifyAddedToWorld { void AddedToWorld(Actor self); }
	public interface INotifyRemovedFromWorld { void RemovedFromWorld(Actor self); }
	public interface INotifySold { void Selling(Actor self); void Sold(Actor self); }
	public interface INotifyDamage { void Damaged(Actor self, AttackInfo e); }
	public interface INotifyDamageStateChanged { void DamageStateChanged(Actor self, AttackInfo e); }
	public interface INotifyRepair { void Repairing(Actor self, Actor host); }
	public interface INotifyKilled { void Killed(Actor self, AttackInfo e); }
	public interface INotifyAppliedDamage { void AppliedDamage(Actor self, Actor damaged, AttackInfo e); }
	public interface INotifyBuildComplete { void BuildingComplete(Actor self); }
	public interface INotifyBuildingPlaced { void BuildingPlaced(Actor self); }
	public interface INotifyProduction { void UnitProduced(Actor self, Actor other, CPos exit); }
	public interface INotifyOtherProduction { void UnitProducedByOther(Actor self, Actor producer, Actor produced); }
	public interface INotifyDelivery { void IncomingDelivery(Actor self); void Delivered(Actor self); }
	public interface INotifyOwnerChanged { void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner); }
	public interface INotifyEffectiveOwnerChanged { void OnEffectiveOwnerChanged(Actor self, Player oldEffectiveOwner, Player newEffectiveOwner); }
	public interface INotifyCapture { void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner); }
	public interface INotifyHarvest { void Harvested(Actor self, ResourceType resource); }
	public interface INotifyInfiltrated { void Infiltrated(Actor self, Actor infiltrator); }

	public interface IUpgradable
	{
		bool AcceptsUpgrade(string type);
		void UpgradeAvailable(Actor self, string type, bool available);
	}

	public interface ISeedableResource { void Seed(Actor self); }

	public interface IDemolishableInfo { bool IsValidTarget(ActorInfo actorInfo, Actor saboteur); }
	public interface IDemolishable
	{
		void Demolish(Actor self, Actor saboteur);
		bool IsValidTarget(Actor self, Actor saboteur);
	}

	public interface IStoreResources { int Capacity { get; } }
	public interface INotifyDocking { void Docked(Actor self, Actor harvester); void Undocked(Actor self, Actor harvester); }

	public interface IEffectiveOwner
	{
		bool Disguised { get; }
		Player Owner { get; }
	}
	public interface IToolTip
	{
		string Name();
		Player Owner();
	}

	public interface IDisable { bool Disabled { get; } }
	public interface IExplodeModifier { bool ShouldExplode(Actor self); }
	public interface IHuskModifier { string HuskActor(Actor self); }

	public interface IRadarSignature
	{
		IEnumerable<CPos> RadarSignatureCells(Actor self);
		Color RadarSignatureColor(Actor self);
	}

	public interface IVisibilityModifier { bool IsVisible(Actor self, Player byPlayer); }
	public interface IFogVisibilityModifier { bool HasFogVisibility(Player byPlayer); }

	public interface IRadarColorModifier { Color RadarColorOverride(Actor self); }

	public interface IOccupySpaceInfo : ITraitInfo { }
	public interface IOccupySpace
	{
		WPos CenterPosition { get; }
		CPos TopLeft { get; }
		IEnumerable<Pair<CPos, SubCell>> OccupiedCells();
	}

	public static class IOccupySpaceExts
	{
		public static CPos NearestCellTo(this IOccupySpace ios, CPos other)
		{
			var nearest = ios.TopLeft;
			var nearestDistance = int.MaxValue;
			foreach (var cell in ios.OccupiedCells())
			{
				var dist = (other - cell.First).LengthSquared;
				if (dist < nearestDistance)
				{
					nearest = cell.First;
					nearestDistance = dist;
				}
			}
			return nearest;
		}
	}

	public interface IRenderModifier { IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r); }
	public interface IDamageModifier { int GetDamageModifier(Actor attacker, DamageWarhead warhead); }
	public interface ISpeedModifier { int GetSpeedModifier(); }
	public interface IFirepowerModifier { int GetFirepowerModifier(); }
	public interface IReloadModifier { int GetReloadModifier(); }
	public interface IInaccuracyModifier { int GetInaccuracyModifier(); }
	public interface IPowerModifier { int GetPowerModifier(); }
	public interface ILoadsPalettes { void LoadPalettes(WorldRenderer wr); }
	public interface IPaletteModifier { void AdjustPalette(IReadOnlyDictionary<string, MutablePalette> b); }
	public interface IPips { IEnumerable<PipType> GetPips(Actor self); }
	public interface ITags { IEnumerable<TagType> GetTags(); }
	public interface ISelectionBar { float GetValue(); Color GetColor(); }

	public interface IPositionable : IOccupySpace
	{
		bool IsLeavingCell(CPos location, SubCell subCell = SubCell.Any);
		bool CanEnterCell(CPos location, Actor ignoreActor = null, bool checkTransientActors = true);
		SubCell GetValidSubCell(SubCell preferred = SubCell.Any);
		SubCell GetAvailableSubCell(CPos location, SubCell preferredSubCell = SubCell.Any, Actor ignoreActor = null, bool checkTransientActors = true);
		void SetPosition(Actor self, CPos cell, SubCell subCell = SubCell.Any);
		void SetPosition(Actor self, WPos pos);
		void SetVisualPosition(Actor self, WPos pos);
	}

	public interface IMoveInfo : ITraitInfo { }
	public interface IMove
	{
		Activity MoveTo(CPos cell, int nearEnough);
		Activity MoveTo(CPos cell, Actor ignoredActor);
		Activity MoveWithinRange(Target target, WRange range);
		Activity MoveWithinRange(Target target, WRange minRange, WRange maxRange);
		Activity MoveFollow(Actor self, Target target, WRange minRange, WRange maxRange);
		Activity MoveIntoWorld(Actor self, CPos cell, SubCell subCell = SubCell.Any);
		Activity VisualMove(Actor self, WPos fromPos, WPos toPos);
		CPos NearestMoveableCell(CPos target);
		bool IsMoving { get; set; }
	}

	public interface INotifyBlockingMove { void OnNotifyBlockingMove(Actor self, Actor blocking); }

	public interface IFacing
	{
		int ROT { get; }
		int Facing { get; set; }
	}

	public interface IFacingInfo : ITraitInfo { int GetInitialFacing(); }

	public interface ICrushable
	{
		void OnCrush(Actor crusher);
		void WarnCrush(Actor crusher);
		bool CrushableBy(string[] crushClasses, Player owner);
	}

	public interface ITraitInfo { object Create(ActorInitializer init); }

	public class TraitInfo<T> : ITraitInfo where T : new() { public virtual object Create(ActorInitializer init) { return new T(); } }

	public interface Requires<T> where T : class, ITraitInfo { }
	public interface UsesInit<T> where T : IActorInit { }

	public interface INotifySelected { void Selected(Actor self); }
	public interface INotifySelection { void SelectionChanged(); }
	public interface IWorldLoaded { void WorldLoaded(World w, WorldRenderer wr); }
	public interface ICreatePlayers { void CreatePlayers(World w); }

	public interface IBotInfo { string Name { get; } }
	public interface IBot
	{
		void Activate(Player p);
		IBotInfo Info { get; }
	}

	public interface IRenderOverlay { void Render(WorldRenderer wr); }
	public interface INotifyBecomingIdle { void OnBecomingIdle(Actor self); } 
	public interface INotifyIdle { void TickIdle(Actor self); }

	public interface IBlocksBullets { }

	public interface IRenderInfantrySequenceModifier
	{
		bool IsModifyingSequence { get; }
		string SequencePrefix { get; }
	}

	public interface IPostRender { void RenderAfterWorld(WorldRenderer wr, Actor self); }
	public interface IRenderShroud { void RenderShroud(WorldRenderer wr, Shroud shroud); }

	public interface IPostRenderSelection { IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr); }

	public interface IBodyOrientation
	{
		WAngle CameraPitch { get; }
		int QuantizedFacings { get; }
		WVec LocalToWorld(WVec vec);
		WRot QuantizeOrientation(Actor self, WRot orientation);
	}

	public interface IBodyOrientationInfo : ITraitInfo
	{
		WVec LocalToWorld(WVec vec);
		WRot QuantizeOrientation(WRot orientation, int facings);
	}

	public interface IQuantizeBodyOrientationInfo { int QuantizedBodyFacings(SequenceProvider sequenceProvider, ActorInfo ai); }

	public interface ITargetableInfo
	{
		string[] GetTargetTypes();
	}

	public interface ITargetable
	{
		string[] TargetTypes { get; }
		IEnumerable<WPos> TargetablePositions(Actor self);
		bool TargetableBy(Actor self, Actor byActor);
		bool RequiresForceFire { get; }
	}

	public interface INotifyStanceChanged
	{
		void StanceChanged(Actor self, Player a, Player b,
			Stance oldStance, Stance newStance);
	}

	public interface ILintPass { void Run(Action<string> emitError, Action<string> emitWarning, Map map); }

	public interface IObjectivesPanel { string PanelName { get; } }

	public interface INotifyObjectivesUpdated
	{
		void OnPlayerWon(Player winner);
		void OnPlayerLost(Player loser);
		void OnObjectiveAdded(Player player, int objectiveID);
		void OnObjectiveCompleted(Player player, int objectiveID);
		void OnObjectiveFailed(Player player, int objectiveID);
	}

	public static class DisableExts
	{
		public static bool IsDisabled(this Actor a)
		{
			return a.TraitsImplementing<IDisable>().Any(d => d.Disabled);
		}
	}

	public interface ILegacyEditorRenderInfo
	{
		string EditorPalette { get; }
		string EditorImage(ActorInfo actor);
	}
}
