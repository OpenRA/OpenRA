#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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

	[RequireExplicitImplementation]
	public interface ITick { void Tick(Actor self); }
	[RequireExplicitImplementation]
	public interface ITickRender { void TickRender(WorldRenderer wr, Actor self); }
	public interface IRender
	{
		IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr);
		IEnumerable<Rectangle> ScreenBounds(Actor self, WorldRenderer wr);
	}

	// TODO: Replace Rectangle with an int2[] polygon
	public interface IMouseBounds { Rectangle MouseoverBounds(Actor self, WorldRenderer wr); }
	public interface IMouseBoundsInfo : ITraitInfoInterface { }
	public interface IAutoMouseBounds { Rectangle AutoMouseoverBounds(Actor self, WorldRenderer wr); }

	// HACK: This provides a shim for legacy code until it can be rewritten
	public interface IDecorationBounds { Rectangle DecorationBounds(Actor self, WorldRenderer wr); }
	public interface IDecorationBoundsInfo : ITraitInfoInterface { }

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

	[RequireExplicitImplementation]
	public interface INotifyCreated { void Created(Actor self); }

	[RequireExplicitImplementation]
	public interface INotifyAddedToWorld { void AddedToWorld(Actor self); }
	[RequireExplicitImplementation]
	public interface INotifyRemovedFromWorld { void RemovedFromWorld(Actor self); }

	[RequireExplicitImplementation]
	public interface INotifyActorDisposing { void Disposing(Actor self); }
	public interface INotifyOwnerChanged { void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner); }
	public interface INotifyEffectiveOwnerChanged { void OnEffectiveOwnerChanged(Actor self, Player oldEffectiveOwner, Player newEffectiveOwner); }

	public interface IVoiced
	{
		string VoiceSet { get; }
		bool PlayVoice(Actor self, string phrase, string variant);
		bool PlayVoiceLocal(Actor self, string phrase, string variant, float volume);
		bool HasVoice(Actor self, string voice);
	}

	public interface IStoreResources { int Capacity { get; } }

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

	public interface IDefaultVisibilityInfo : ITraitInfoInterface { }
	public interface IDefaultVisibility { bool IsVisible(Actor self, Player byPlayer); }
	public interface IVisibilityModifier { bool IsVisible(Actor self, Player byPlayer); }

	public interface IOccupySpaceInfo : ITraitInfoInterface
	{
		IReadOnlyDictionary<CPos, SubCell> OccupiedCells(ActorInfo info, CPos location, SubCell subCell = SubCell.Any);
		bool SharesCell { get; }
	}

	public interface IOccupySpace
	{
		WPos CenterPosition { get; }
		CPos TopLeft { get; }
		Pair<CPos, SubCell>[] OccupiedCells();
	}

	public enum SubCell { Invalid = int.MinValue, Any = int.MinValue / 2, FullCell = 0, First = 1 }
	public interface IActorMap
	{
		IEnumerable<Actor> GetActorsAt(CPos a);
		IEnumerable<Actor> GetActorsAt(CPos a, SubCell sub);
		bool HasFreeSubCell(CPos cell, bool checkTransient = true);
		SubCell FreeSubCell(CPos cell, SubCell preferredSubCell = SubCell.Any, bool checkTransient = true);
		SubCell FreeSubCell(CPos cell, SubCell preferredSubCell, Func<Actor, bool> checkIfBlocker);
		bool AnyActorsAt(CPos a);
		bool AnyActorsAt(CPos a, SubCell sub, bool checkTransient = true);
		bool AnyActorsAt(CPos a, SubCell sub, Func<Actor, bool> withCondition);
		void AddInfluence(Actor self, IOccupySpace ios);
		void RemoveInfluence(Actor self, IOccupySpace ios);
		int AddCellTrigger(CPos[] cells, Action<Actor> onEntry, Action<Actor> onExit);
		void RemoveCellTrigger(int id);
		int AddProximityTrigger(WPos pos, WDist range, WDist vRange, Action<Actor> onEntry, Action<Actor> onExit);
		void RemoveProximityTrigger(int id);
		void UpdateProximityTrigger(int id, WPos newPos, WDist newRange, WDist newVRange);
		void AddPosition(Actor a, IOccupySpace ios);
		void RemovePosition(Actor a, IOccupySpace ios);
		void UpdatePosition(Actor a, IOccupySpace ios);
		IEnumerable<Actor> ActorsInBox(WPos a, WPos b);
	}

	public interface IRenderModifier
	{
		IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r);

		// HACK: This is here to support the WithShadow trait.
		// That trait should be rewritten using standard techniques, and then this interface method removed
		IEnumerable<Rectangle> ModifyScreenBounds(Actor self, WorldRenderer wr, IEnumerable<Rectangle> r);
	}

	public interface ILoadsPalettes { void LoadPalettes(WorldRenderer wr); }
	public interface ILoadsPlayerPalettes { void LoadPlayerPalettes(WorldRenderer wr, string playerName, HSLColor playerColor, bool replaceExisting); }
	public interface IPaletteModifier { void AdjustPalette(IReadOnlyDictionary<string, MutablePalette> b); }
	public interface IPips { IEnumerable<PipType> GetPips(Actor self); }

	[RequireExplicitImplementation]
	public interface ISelectionBar { float GetValue(); Color GetColor(); bool DisplayWhenEmpty { get; } }

	public interface IPositionableInfo : IOccupySpaceInfo
	{
		bool CanEnterCell(World world, Actor self, CPos cell, Actor ignoreActor = null, bool checkTransientActors = true);
	}

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

	[RequireExplicitImplementation]
	public interface ITemporaryBlocker
	{
		bool CanRemoveBlockage(Actor self, Actor blocking);
		bool IsBlocking(Actor self, CPos cell);
	}

	public interface IFacing
	{
		int TurnSpeed { get; }
		int Facing { get; set; }
	}

	public interface IFacingInfo : ITraitInfoInterface { int GetInitialFacing(); }

	public interface ITraitInfoInterface { }
	public interface ITraitInfo : ITraitInfoInterface { object Create(ActorInitializer init); }

	public class TraitInfo<T> : ITraitInfo where T : new() { public virtual object Create(ActorInitializer init) { return new T(); } }
	public interface ILobbyCustomRulesIgnore { }

	[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1302:InterfaceNamesMustBeginWithI", Justification = "Not a real interface, but more like a tag.")]
	public interface Requires<T> where T : class, ITraitInfoInterface { }
	[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1302:InterfaceNamesMustBeginWithI", Justification = "Not a real interface, but more like a tag.")]
	public interface UsesInit<T> : ITraitInfo where T : IActorInit { }

	[RequireExplicitImplementation]
	public interface INotifySelected { void Selected(Actor self); }
	[RequireExplicitImplementation]
	public interface INotifySelection { void SelectionChanged(); }

	public interface IWorldLoaded { void WorldLoaded(World w, WorldRenderer wr); }

	[RequireExplicitImplementation]
	public interface ICreatePlayers { void CreatePlayers(World w); }

	public interface IBotInfo : ITraitInfoInterface
	{
		string Type { get; }
		string Name { get; }
	}

	public interface IBot
	{
		void Activate(Player p);
		IBotInfo Info { get; }
	}

	[RequireExplicitImplementation]
	public interface IRenderOverlay { void Render(WorldRenderer wr); }

	[RequireExplicitImplementation]
	public interface INotifyBecomingIdle { void OnBecomingIdle(Actor self); }
	[RequireExplicitImplementation]
	public interface INotifyIdle { void TickIdle(Actor self); }

	public interface IRenderAboveWorld { void RenderAboveWorld(Actor self, WorldRenderer wr); }
	public interface IRenderShroud { void RenderShroud(Shroud shroud, WorldRenderer wr); }

	public interface IRenderAboveShroud
	{
		IEnumerable<IRenderable> RenderAboveShroud(Actor self, WorldRenderer wr);
		bool SpatiallyPartitionable { get; }
	}

	public interface IRenderAboveShroudWhenSelected
	{
		IEnumerable<IRenderable> RenderAboveShroud(Actor self, WorldRenderer wr);
		bool SpatiallyPartitionable { get; }
	}

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

	[RequireExplicitImplementation]
	public interface ITargetablePositions
	{
		IEnumerable<WPos> TargetablePositions(Actor self);
	}

	[RequireExplicitImplementation]
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
		public readonly string Description;
		public readonly IReadOnlyDictionary<string, string> Values;
		public readonly string DefaultValue;
		public readonly bool IsLocked;
		public readonly bool IsVisible;
		public readonly int DisplayOrder;

		public LobbyOption(string id, string name, string description, bool visible, int displayorder,
			IReadOnlyDictionary<string, string> values, string defaultValue, bool locked)
		{
			Id = id;
			Name = name;
			Description = description;
			IsVisible = visible;
			DisplayOrder = displayorder;
			Values = values;
			DefaultValue = defaultValue;
			IsLocked = locked;
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

		public LobbyBooleanOption(string id, string name, string description, bool visible, int displayorder, bool defaultValue, bool locked)
			: base(id, name, description, visible, displayorder, new ReadOnlyDictionary<string, string>(BoolValues), defaultValue.ToString(), locked) { }

		public override string ValueChangedMessage(string playerName, string newValue)
		{
			return playerName + " " + BoolValues[newValue] + " " + Name + ".";
		}
	}
}
