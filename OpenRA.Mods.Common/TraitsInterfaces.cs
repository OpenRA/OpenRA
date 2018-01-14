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
using System.Drawing;
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

	public interface IPlaceBuildingDecorationInfo : ITraitInfo
	{
		IEnumerable<IRenderable> Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition);
	}

	[RequireExplicitImplementation]
	public interface IBlocksProjectiles
	{
		WDist BlockingHeight { get; }
	}

	[RequireExplicitImplementation]
	public interface IBlocksProjectilesInfo : ITraitInfoInterface { }

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

	public interface INotifyDamage { void Damaged(Actor self, AttackInfo e); }
	public interface INotifyKilled { void Killed(Actor self, AttackInfo e); }
	public interface INotifyAppliedDamage { void AppliedDamage(Actor self, Actor damaged, AttackInfo e); }

	[RequireExplicitImplementation]
	public interface INotifyRepair
	{
		void BeforeRepair(Actor self, Actor target);
		void RepairTick(Actor self, Actor target);
		void AfterRepair(Actor self, Actor target);
	}

	[RequireExplicitImplementation]
	public interface INotifyPowerLevelChanged { void PowerLevelChanged(Actor self); }

	public interface INotifyBuildingPlaced { void BuildingPlaced(Actor self); }
	public interface INotifyNuke { void Launching(Actor self); }
	public interface INotifyBurstComplete { void FiredBurst(Actor self, Target target, Armament a); }
	public interface INotifyChat { bool OnChat(string from, string message); }
	public interface INotifyProduction { void UnitProduced(Actor self, Actor other, CPos exit); }
	public interface INotifyOtherProduction { void UnitProducedByOther(Actor self, Actor producer, Actor produced, string productionType); }
	public interface INotifyDelivery { void IncomingDelivery(Actor self); void Delivered(Actor self); }
	public interface INotifyDocking { void Docked(Actor self, Actor harvester); void Undocked(Actor self, Actor harvester); }
	public interface INotifyParachute { void OnParachute(Actor self); void OnLanded(Actor self, Actor ignore); }
	public interface INotifyCapture { void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner); }
	public interface INotifyDiscovered { void OnDiscovered(Actor self, Player discoverer, bool playNotification); }
	public interface IRenderActorPreviewInfo : ITraitInfo { IEnumerable<IActorPreview> RenderPreview(ActorPreviewInitializer init); }
	public interface ICruiseAltitudeInfo : ITraitInfo { WDist GetCruiseAltitude(); }

	public interface IExplodeModifier { bool ShouldExplode(Actor self); }
	public interface IHuskModifier { string HuskActor(Actor self); }

	public interface ISeedableResource { void Seed(Actor self); }

	[RequireExplicitImplementation]
	public interface INotifyInfiltrated { void Infiltrated(Actor self, Actor infiltrator, HashSet<string> types); }

	[RequireExplicitImplementation]
	public interface INotifyBlockingMove { void OnNotifyBlockingMove(Actor self, Actor blocking); }

	[RequireExplicitImplementation]
	public interface INotifyPassengerEntered { void OnPassengerEntered(Actor self, Actor passenger); }

	[RequireExplicitImplementation]
	public interface INotifyPassengerExited { void OnPassengerExited(Actor self, Actor passenger); }

	[RequireExplicitImplementation]
	public interface IObservesVariablesInfo : ITraitInfo { }

	public delegate void VariableObserverNotifier(Actor self, IReadOnlyDictionary<string, int> variables);
	public struct VariableObserver
	{
		public VariableObserverNotifier Notifier;
		public IEnumerable<string> Variables;
		public VariableObserver(VariableObserverNotifier notifier, IEnumerable<string> variables)
		{
			Notifier = notifier;
			Variables = variables;
		}
	}

	public interface IObservesVariables
	{
		IEnumerable<VariableObserver> GetVariableObservers();
	}

	public interface INotifyHarvesterAction
	{
		void MovingToResources(Actor self, CPos targetCell, Activity next);
		void MovingToRefinery(Actor self, Actor refineryActor, Activity next);
		void MovementCancelled(Actor self);
		void Harvested(Actor self, ResourceType resource);
		void Docked();
		void Undocked();
	}

	[RequireExplicitImplementation]
	public interface INotifyUnload
	{
		void Unloading(Actor self);
	}

	[RequireExplicitImplementation]
	public interface INotifyDemolition
	{
		void Demolishing(Actor self);
	}

	[RequireExplicitImplementation]
	public interface INotifyInfiltration
	{
		void Infiltrating(Actor self);
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

	public interface INotifyTransform
	{
		void BeforeTransform(Actor self);
		void OnTransform(Actor self);
		void AfterTransform(Actor toActor);
	}

	public interface INotifyDeployComplete
	{
		void FinishedDeploy(Actor self);
		void FinishedUndeploy(Actor self);
	}

	public interface INotifyDeployTriggered
	{
		void Deploy(Actor self, bool skipMakeAnim);
		void Undeploy(Actor self, bool skipMakeAnim);
	}

	public interface IAcceptResourcesInfo : ITraitInfo { }
	public interface IAcceptResources
	{
		void OnDock(Actor harv, DeliverResources dockOrder);
		void GiveResource(int amount);
		bool CanGiveResource(int amount);
		CVec DeliveryOffset { get; }
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

	// For traits that want to be exposed to the "Deploy" UI button / hotkey
	[RequireExplicitImplementation]
	public interface IIssueDeployOrder
	{
		Order IssueDeployOrder(Actor self);
		bool CanIssueDeployOrder(Actor self);
	}

	public enum ActorPreviewType { PlaceBuilding, ColorPicker, MapEditorSidebar }

	[RequireExplicitImplementation]
	public interface IActorPreviewInitInfo : ITraitInfo
	{
		IEnumerable<object> ActorPreviewInits(ActorInfo ai, ActorPreviewType type);
	}

	public interface IMoveInfo : ITraitInfoInterface { }

	public interface IMove
	{
		Activity MoveTo(CPos cell, int nearEnough);
		Activity MoveTo(CPos cell, Actor ignoreActor);
		Activity MoveWithinRange(Target target, WDist range);
		Activity MoveWithinRange(Target target, WDist minRange, WDist maxRange);
		Activity MoveFollow(Actor self, Target target, WDist minRange, WDist maxRange);
		Activity MoveIntoWorld(Actor self, CPos cell, SubCell subCell = SubCell.Any);
		Activity MoveToTarget(Actor self, Target target);
		Activity MoveIntoTarget(Actor self, Target target);
		Activity VisualMove(Actor self, WPos fromPos, WPos toPos);
		CPos NearestMoveableCell(CPos target);
		bool IsMoving { get; set; }
		bool IsMovingVertically { get; set; }
		bool CanEnterTargetNow(Actor self, Target target);
	}

	public interface IRadarSignature
	{
		void PopulateRadarSignatureCells(Actor self, List<Pair<CPos, Color>> destinationBuffer);
	}

	public interface IRadarColorModifier { Color RadarColorOverride(Actor self, Color color); }

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

	public interface INotifyCashTransfer
	{
		void OnAcceptingCash(Actor self, Actor donor);
		void OnDeliveringCash(Actor self, Actor acceptor);
	}

	[RequireExplicitImplementation]
	public interface ITargetableCells
	{
		Pair<CPos, SubCell>[] TargetableCells();
	}

	[RequireExplicitImplementation]
	public interface IPreventsShroudReset { bool PreventShroudReset(Actor self); }
}
