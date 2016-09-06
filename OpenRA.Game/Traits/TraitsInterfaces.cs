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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using OpenRA.Activities;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Primitives;

namespace OpenRA.Traits
{
	public sealed class RequireExplicitImplementationAttribute : Attribute { }

	[Flags]
	public enum DamageState
	{
		Undamaged = 1,
		Light = 2,
		Medium = 4,
		Heavy = 8,
		Critical = 16,
		Dead = 32
	}

	public interface IHealth
	{
		DamageState DamageState { get; }
		int HP { get; }
		int MaxHP { get; }
		int DisplayHP { get; }
		bool IsDead { get; }

		void InflictDamage(Actor self, Actor attacker, Damage damage, bool ignoreModifiers);
		void Kill(Actor self, Actor attacker);
	}

	// depends on the order of pips in WorldRenderer.cs!
	public enum PipType { Transparent, Green, Yellow, Red, Gray, Blue, Ammo, AmmoEmpty }

	[Flags]
	public enum Stance
	{
		None = 0,
		Enemy = 1,
		Neutral = 2,
		Ally = 4,
	}

	public static class StanceExts
	{
		public static bool HasStance(this Stance s, Stance stance)
		{
			// PERF: Enum.HasFlag is slower and requires allocations.
			return (s & stance) == stance;
		}
	}

	[Flags]
	public enum ImpactType
	{
		None = 0,
		Ground = 1,
		GroundHit = 2,
		Water = 4,
		WaterHit = 8,
		Air = 16,
		AirHit = 32,
		TargetTerrain = 64,
		TargetHit = 128
	}

	public class AttackInfo
	{
		public Damage Damage;
		public Actor Attacker;
		public DamageState DamageState;
		public DamageState PreviousDamageState;
	}

	public class Damage
	{
		public readonly int Value;
		public readonly HashSet<string> DamageTypes;

		public Damage(int damage, HashSet<string> damageTypes)
		{
			Value = damage;
			DamageTypes = damageTypes;
		}

		public Damage(int damage)
		{
			Value = damage;
			DamageTypes = new HashSet<string>();
		}
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

	[Flags] public enum TargetModifiers { None = 0, ForceAttack = 1, ForceQueue = 2, ForceMove = 4 }

	public static class TargetModifiersExts
	{
		public static bool HasModifier(this TargetModifiers self, TargetModifiers m)
		{
			// PERF: Enum.HasFlag is slower and requires allocations.
			return (self & m) == m;
		}
	}

	public interface IOrderTargeter
	{
		string OrderID { get; }
		int OrderPriority { get; }
		bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, ref TargetModifiers modifiers, ref string cursor);
		bool IsQueued { get; }
		bool TargetOverridesSelection(TargetModifiers modifiers);
	}

	public interface IResolveOrder { void ResolveOrder(Actor self, Order order); }
	public interface IValidateOrder { bool OrderValidation(OrderManager orderManager, World world, int clientId, Order order); }
	public interface IOrderVoice { string VoicePhraseForOrder(Actor self, Order order); }
	public interface INotifyCreated { void Created(Actor self); }
	public interface INotifyAddedToWorld { void AddedToWorld(Actor self); }
	public interface INotifyRemovedFromWorld { void RemovedFromWorld(Actor self); }
	public interface INotifySold { void Selling(Actor self); void Sold(Actor self); }
	public interface INotifyDamage { void Damaged(Actor self, AttackInfo e); }
	public interface INotifyDamageStateChanged { void DamageStateChanged(Actor self, AttackInfo e); }
	public interface INotifyRepair { void Repairing(Actor self, Actor target); }
	public interface INotifyKilled { void Killed(Actor self, AttackInfo e); }
	public interface INotifyActorDisposing { void Disposing(Actor self); }
	public interface INotifyAppliedDamage { void AppliedDamage(Actor self, Actor damaged, AttackInfo e); }
	public interface INotifyBuildComplete { void BuildingComplete(Actor self); }
	public interface INotifyBuildingPlaced { void BuildingPlaced(Actor self); }
	public interface INotifyProduction { void UnitProduced(Actor self, Actor other, CPos exit); }
	public interface INotifyOtherProduction { void UnitProducedByOther(Actor self, Actor producer, Actor produced); }
	public interface INotifyDelivery { void IncomingDelivery(Actor self); void Delivered(Actor self); }
	public interface INotifyOwnerChanged { void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner); }
	public interface INotifyEffectiveOwnerChanged { void OnEffectiveOwnerChanged(Actor self, Player oldEffectiveOwner, Player newEffectiveOwner); }
	public interface INotifyCapture { void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner); }
	public interface INotifyInfiltrated { void Infiltrated(Actor self, Actor infiltrator); }
	public interface INotifyDiscovered { void OnDiscovered(Actor self, Player discoverer, bool playNotification); }

	public interface ISeedableResource { void Seed(Actor self); }

	public interface ISelectionDecorationsInfo : ITraitInfoInterface
	{
		int[] SelectionBoxBounds { get; }
	}

	public interface IVoiced
	{
		string VoiceSet { get; }
		bool PlayVoice(Actor self, string phrase, string variant);
		bool PlayVoiceLocal(Actor self, string phrase, string variant, float volume);
		bool HasVoice(Actor self, string voice);
	}

	public interface IDemolishableInfo : ITraitInfoInterface { bool IsValidTarget(ActorInfo actorInfo, Actor saboteur); }
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

	public interface ITooltip
	{
		ITooltipInfo TooltipInfo { get; }
		Player Owner { get; }
	}

	public interface ITooltipInfo : ITraitInfoInterface
	{
		string TooltipForPlayerStance(Stance stance);
		bool IsOwnerRowVisible { get; }
	}

	public interface IProvideTooltipInfo
	{
		bool IsTooltipVisible(Player forPlayer);
		string TooltipText { get; }
	}

	public interface IDisabledTrait { bool IsTraitDisabled { get; } }
	public interface IDisable { bool Disabled { get; } }
	public interface IExplodeModifier { bool ShouldExplode(Actor self); }
	public interface IHuskModifier { string HuskActor(Actor self); }

	public interface IRadarSignature
	{
		IEnumerable<Pair<CPos, Color>> RadarSignatureCells(Actor self);
	}

	public interface IDefaultVisibilityInfo : ITraitInfoInterface { }
	public interface IDefaultVisibility { bool IsVisible(Actor self, Player byPlayer); }
	public interface IVisibilityModifier { bool IsVisible(Actor self, Player byPlayer); }

	public interface IFogVisibilityModifier
	{
		bool IsVisible(Actor actor);
		bool HasFogVisibility();
	}

	public interface IRadarColorModifier { Color RadarColorOverride(Actor self, Color color); }

	public interface IOccupySpaceInfo : ITraitInfoInterface
	{
		IReadOnlyDictionary<CPos, SubCell> OccupiedCells(ActorInfo info, CPos location, SubCell subCell = SubCell.Any);
		bool SharesCell { get; }
	}

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
	public interface IDamageModifier { int GetDamageModifier(Actor attacker, Damage damage); }
	public interface ISpeedModifier { int GetSpeedModifier(); }
	public interface IFirepowerModifier { int GetFirepowerModifier(); }
	public interface IReloadModifier { int GetReloadModifier(); }
	public interface IInaccuracyModifier { int GetInaccuracyModifier(); }
	public interface IRangeModifier { int GetRangeModifier(); }
	public interface IRangeModifierInfo : ITraitInfoInterface { int GetRangeModifierDefault(); }
	public interface IPowerModifier { int GetPowerModifier(); }
	public interface ILoadsPalettes { void LoadPalettes(WorldRenderer wr); }
	public interface ILoadsPlayerPalettes { void LoadPlayerPalettes(WorldRenderer wr, string playerName, HSLColor playerColor, bool replaceExisting); }
	public interface IPaletteModifier { void AdjustPalette(IReadOnlyDictionary<string, MutablePalette> b); }
	public interface IPips { IEnumerable<PipType> GetPips(Actor self); }

	[RequireExplicitImplementation]
	public interface ISelectionBar { float GetValue(); Color GetColor(); bool DisplayWhenEmpty { get; } }

	public interface IPositionableInfo : ITraitInfoInterface { }
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

	public interface IMoveInfo : ITraitInfoInterface { }
	public interface IMove
	{
		Activity MoveTo(CPos cell, int nearEnough);
		Activity MoveTo(CPos cell, Actor ignoredActor);
		Activity MoveWithinRange(Target target, WDist range);
		Activity MoveWithinRange(Target target, WDist minRange, WDist maxRange);
		Activity MoveFollow(Actor self, Target target, WDist minRange, WDist maxRange);
		Activity MoveIntoWorld(Actor self, CPos cell, SubCell subCell = SubCell.Any);
		Activity MoveToTarget(Actor self, Target target);
		Activity MoveIntoTarget(Actor self, Target target);
		Activity VisualMove(Actor self, WPos fromPos, WPos toPos);
		CPos NearestMoveableCell(CPos target);
		bool IsMoving { get; set; }
		bool CanEnterTargetNow(Actor self, Target target);
	}

	[RequireExplicitImplementation]
	public interface ITemporaryBlocker
	{
		bool CanRemoveBlockage(Actor self, Actor blocking);
		bool IsBlocking(Actor self, CPos cell);
	}

	public interface INotifyBlockingMove { void OnNotifyBlockingMove(Actor self, Actor blocking); }

	public interface IFacing
	{
		int TurnSpeed { get; }
		int Facing { get; set; }
	}

	public interface IFacingInfo : ITraitInfoInterface { int GetInitialFacing(); }

	[RequireExplicitImplementation]
	public interface ICrushable
	{
		bool CrushableBy(Actor self, Actor crusher, HashSet<string> crushClasses);
	}

	[RequireExplicitImplementation]
	public interface INotifyCrushed
	{
		void OnCrush(Actor self, Actor crusher, HashSet<string> crushClasses);
		void WarnCrush(Actor self, Actor crusher, HashSet<string> crushClasses);
	}

	public interface ITraitInfoInterface { }
	public interface ITraitInfo : ITraitInfoInterface { object Create(ActorInitializer init); }

	public class TraitInfo<T> : ITraitInfo where T : new() { public virtual object Create(ActorInitializer init) { return new T(); } }
	public interface ILobbyCustomRulesIgnore { }

	[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1302:InterfaceNamesMustBeginWithI", Justification = "Not a real interface, but more like a tag.")]
	public interface Requires<T> where T : class, ITraitInfoInterface { }
	[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1302:InterfaceNamesMustBeginWithI", Justification = "Not a real interface, but more like a tag.")]
	public interface UsesInit<T> : ITraitInfo where T : IActorInit { }

	public interface INotifySelected { void Selected(Actor self); }
	public interface INotifySelection { void SelectionChanged(); }
	public interface IWorldLoaded { void WorldLoaded(World w, WorldRenderer wr); }
	public interface ICreatePlayers { void CreatePlayers(World w); }

	public interface IBotInfo : ITraitInfoInterface { string Name { get; } }
	public interface IBot
	{
		void Activate(Player p);
		IBotInfo Info { get; }
	}

	public interface IRenderOverlay { void Render(WorldRenderer wr); }
	public interface INotifyBecomingIdle { void OnBecomingIdle(Actor self); }
	public interface INotifyIdle { void TickIdle(Actor self); }

	public interface IRenderInfantrySequenceModifier
	{
		bool IsModifyingSequence { get; }
		string SequencePrefix { get; }
	}

	public interface IRenderAboveWorld { void RenderAboveWorld(Actor self, WorldRenderer wr); }
	public interface IRenderShroud { void RenderShroud(Shroud shroud, WorldRenderer wr); }
	public interface IRenderAboveShroud { IEnumerable<IRenderable> RenderAboveShroud(Actor self, WorldRenderer wr); }
	public interface IRenderAboveShroudWhenSelected { IEnumerable<IRenderable> RenderAboveShroud(Actor self, WorldRenderer wr); }

	public interface ITargetableInfo : ITraitInfoInterface
	{
		HashSet<string> GetTargetTypes();
	}

	public interface ITargetable
	{
		// Check IsTraitEnabled or !IsTraitDisabled first
		HashSet<string> TargetTypes { get; }
		bool TargetableBy(Actor self, Actor byActor);
		bool RequiresForceFire { get; }
	}

	public interface ITargetablePositions
	{
		IEnumerable<WPos> TargetablePositions(Actor self);
	}

	public interface ILintPass { void Run(Action<string> emitError, Action<string> emitWarning, ModData modData); }
	public interface ILintMapPass { void Run(Action<string> emitError, Action<string> emitWarning, Map map); }
	public interface ILintRulesPass { void Run(Action<string> emitError, Action<string> emitWarning, Ruleset rules); }

	public interface IObjectivesPanel
	{
		string PanelName { get; }
		int ExitDelay { get; }
	}

	public interface INotifyObjectivesUpdated
	{
		void OnPlayerWon(Player winner);
		void OnPlayerLost(Player loser);
		void OnObjectiveAdded(Player player, int objectiveID);
		void OnObjectiveCompleted(Player player, int objectiveID);
		void OnObjectiveFailed(Player player, int objectiveID);
	}

	public interface IGameOver { void GameOver(World world); }

	public interface IWarhead
	{
		int Delay { get; }
		bool IsValidAgainst(Actor victim, Actor firedBy);
		bool IsValidAgainst(FrozenActor victim, Actor firedBy);
		void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers);
	}

	public interface IRulesetLoaded<TInfo> { void RulesetLoaded(Ruleset rules, TInfo info); }
	public interface IRulesetLoaded : IRulesetLoaded<ActorInfo>, ITraitInfoInterface { }

	[RequireExplicitImplementation]
	public interface ILobbyOptions : ITraitInfoInterface
	{
		IEnumerable<LobbyOption> LobbyOptions(Ruleset rules);
	}

	public class LobbyOption
	{
		public readonly string Id;
		public readonly string Name;
		public readonly IReadOnlyDictionary<string, string> Values;
		public readonly string DefaultValue;
		public readonly bool Locked;

		public LobbyOption(string id, string name, IReadOnlyDictionary<string, string> values, string defaultValue, bool locked)
		{
			Id = id;
			Name = name;
			Values = values;
			DefaultValue = defaultValue;
			Locked = locked;
		}

		public virtual string ValueChangedMessage(string playerName, string newValue)
		{
			return playerName + " changed " + Name + " to " + Values[newValue] + ".";
		}
	}

	public class LobbyBooleanOption : LobbyOption
	{
		static readonly Dictionary<string, string> BoolValues = new Dictionary<string, string>()
		{
			{ true.ToString(), "enabled" },
			{ false.ToString(), "disabled" }
		};

		public LobbyBooleanOption(string id, string name, bool defaultValue, bool locked)
			: base(id, name, new ReadOnlyDictionary<string, string>(BoolValues), defaultValue.ToString(), locked) { }

		public override string ValueChangedMessage(string playerName, string newValue)
		{
			return playerName + " " + BoolValues[newValue] + " " + Name + ".";
		}
	}
}
