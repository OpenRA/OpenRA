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

using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public enum VisibilityType { Footprint, CenterPosition, GroundPosition }

	public enum AttackDelayType { Preparation, Attack }

	public interface IQuantizeBodyOrientationInfo : ITraitInfo
	{
		int QuantizedBodyFacings(ActorInfo ai, SequenceProvider sequenceProvider, string race);
	}

	public interface INotifyResourceClaimLost
	{
		void OnNotifyResourceClaimLost(Actor self, ResourceClaim claim, Actor claimer);
	}

	public interface IPlaceBuildingDecorationInfo : ITraitInfo
	{
		IEnumerable<IRenderable> Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition);
	}

	[RequireExplicitImplementation]
	public interface INotifySold
	{
		void Selling(Actor self);
		void Sold(Actor self);
	}

	public interface IDemolishableInfo : ITraitInfoInterface { bool IsValidTarget(ActorInfo actorInfo, Actor saboteur); }
	public interface IDemolishable
	{
		void Demolish(Actor self, Actor saboteur);
		bool IsValidTarget(Actor self, Actor saboteur);
	}

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

	[RequireExplicitImplementation]
	public interface INotifyAttack
	{
		void Attacking(Actor self, Target target, Armament a, Barrel barrel);
		void PreparingAttack(Actor self, Target target, Armament a, Barrel barrel);
	}

	[RequireExplicitImplementation]
	public interface INotifyBuildComplete { void BuildingComplete(Actor self); }

	[RequireExplicitImplementation]
	public interface INotifyDamageStateChanged { void DamageStateChanged(Actor self, AttackInfo e); }

	public interface INotifyBuildingPlaced { void BuildingPlaced(Actor self); }
	public interface INotifyRepair { void Repairing(Actor self, Actor target); }
	public interface INotifyBurstComplete { void FiredBurst(Actor self, Target target, Armament a); }
	public interface INotifyChat { bool OnChat(string from, string message); }
	public interface INotifyProduction { void UnitProduced(Actor self, Actor other, CPos exit); }
	public interface INotifyOtherProduction { void UnitProducedByOther(Actor self, Actor producer, Actor produced); }
	public interface INotifyDelivery { void IncomingDelivery(Actor self); void Delivered(Actor self); }
	public interface INotifyDocking { void Docked(Actor self, Actor client); void Undocked(Actor self, Actor client); }
	public interface INotifyParachute { void OnParachute(Actor self); void OnLanded(Actor self, Actor ignore); }
	public interface INotifyCapture { void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner); }
	public interface INotifyDiscovered { void OnDiscovered(Actor self, Player discoverer, bool playNotification); }
	public interface IRenderActorPreviewInfo : ITraitInfo { IEnumerable<IActorPreview> RenderPreview(ActorPreviewInitializer init); }
	public interface ICruiseAltitudeInfo : ITraitInfo { WDist GetCruiseAltitude(); }
	public interface INotifyCashTransfer { void OnCashTransfer(Actor self, Actor donor); }

	[RequireExplicitImplementation]
	public interface INotifyInfiltrated { void Infiltrated(Actor self, Actor infiltrator); }

	[RequireExplicitImplementation]
	public interface INotifyBlockingMove { void OnNotifyBlockingMove(Actor self, Actor blocking); }

	[RequireExplicitImplementation]
	public interface INotifyPassengerEntered { void OnPassengerEntered(Actor self, Actor passenger); }

	[RequireExplicitImplementation]
	public interface INotifyPassengerExited { void OnPassengerExited(Actor self, Actor passenger); }

	[RequireExplicitImplementation]
	public interface IConditionConsumerInfo : ITraitInfo { }

	public interface IConditionConsumer
	{
		IEnumerable<string> Conditions { get; }
		void ConditionsChanged(Actor self, IReadOnlyDictionary<string, int> conditions);
	}

	public interface INotifyHarvesterAction
	{
		void MovingToResources(Actor self, CPos targetCell, Activity next);
		void MovingToRefinery(Actor self, CPos targetCell, Activity next);
		void MovementCancelled(Actor self);
		void Harvested(Actor self, ResourceType resource);
		void Docked();
		void Undocked();
	}

	public interface ITechTreePrerequisiteInfo : ITraitInfo { }
	public interface ITechTreePrerequisite
	{
		IEnumerable<string> ProvidesPrerequisites { get; }
	}

	public interface ITechTreeElement
	{
		void PrerequisitesAvailable(string key);
		void PrerequisitesUnavailable(string key);
		void PrerequisitesItemHidden(string key);
		void PrerequisitesItemVisible(string key);
	}

	public interface IProductionIconOverlay
	{
		Sprite Sprite { get; }
		string Palette { get; }
		float2 Offset(float2 iconSize);
		bool IsOverlayActive(ActorInfo ai);
	}

	public interface INotifyTransform { void BeforeTransform(Actor self); void OnTransform(Actor self); void AfterTransform(Actor toActor); }

	public interface IAcceptDockInfo : ITraitInfo { }
	public interface IAcceptDock
	{
		// postUndockActivity: after undock, we start doing this, after OnDockActivity is done.
		void QueueOnDockActivity(Actor client, Dock dock);
		void OnUndock(Actor harv, Dock dock);
		void OnArrival(Actor harv, Dock dock);
		void GiveResource(int amount);
		bool CanGiveResource(int amount);

		// ReserveDock should queue activities that make the client come to a valid dock and do dock activity
		// (or wait activity, depending on the situation)
		void ReserveDock(Actor client, Activity postDockOrder);

		IEnumerable<CPos> DockLocations { get; }
		bool AllowDocking { get; }
	}

	public interface IProvidesAssetBrowserPalettes
	{
		IEnumerable<string> PaletteNames { get; }
	}

	public interface ICallForTransport
	{
		WDist MinimumDistance { get; }
		bool WantsTransport { get; }
		void MovementCancelled(Actor self);
		void RequestTransport(Actor self, CPos destination, Activity afterLandActivity);
	}

	public interface IDeathActorInitModifier
	{
		void ModifyDeathActorInit(Actor self, TypeDictionary init);
	}

	public interface IPreventsAutoTarget
	{
		bool PreventsAutoTarget(Actor self, Actor attacker);
	}

	[RequireExplicitImplementation]
	interface IWallConnector
	{
		bool AdjacentWallCanConnect(Actor self, CPos wallLocation, string wallType, out CVec facing);
		void SetDirty();
	}

	[RequireExplicitImplementation]
	public interface IActorPreviewInitModifier
	{
		void ModifyActorPreviewInit(Actor self, TypeDictionary inits);
	}

	[RequireExplicitImplementation]
	public interface INotifyRearm { void Rearming(Actor host, Actor other); }

	[RequireExplicitImplementation]
	public interface IRenderInfantrySequenceModifier
	{
		bool IsModifyingSequence { get; }
		string SequencePrefix { get; }
	}

	[RequireExplicitImplementation]
	public interface IDamageModifier { int GetDamageModifier(Actor attacker, Damage damage); }

	[RequireExplicitImplementation]
	public interface ISpeedModifier { int GetSpeedModifier(); }

	[RequireExplicitImplementation]
	public interface IFirepowerModifier { int GetFirepowerModifier(); }

	[RequireExplicitImplementation]
	public interface IReloadModifier { int GetReloadModifier(); }

	[RequireExplicitImplementation]
	public interface IInaccuracyModifier { int GetInaccuracyModifier(); }

	[RequireExplicitImplementation]
	public interface IRangeModifier { int GetRangeModifier(); }

	[RequireExplicitImplementation]
	public interface IRangeModifierInfo : ITraitInfoInterface { int GetRangeModifierDefault(); }

	[RequireExplicitImplementation]
	public interface IPowerModifier { int GetPowerModifier(); }

	[RequireExplicitImplementation]
	public interface IGivesExperienceModifier { int GetGivesExperienceModifier(); }

	[RequireExplicitImplementation]
	public interface IGainsExperienceModifier { int GetGainsExperienceModifier(); }

	[RequireExplicitImplementation]
	public interface ICustomMovementLayer
	{
		byte Index { get; }
		bool InteractsWithDefaultLayer { get; }

		bool EnabledForActor(ActorInfo a, MobileInfo mi);
		int EntryMovementCost(ActorInfo a, MobileInfo mi, CPos cell);
		int ExitMovementCost(ActorInfo a, MobileInfo mi, CPos cell);

		byte GetTerrainIndex(CPos cell);
		WPos CenterOfCell(CPos cell);
	}
}
