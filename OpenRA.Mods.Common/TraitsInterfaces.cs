#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public enum VisibilityType { Footprint, CenterPosition, GroundPosition }

	public enum AttackDelayType { Preparation, Attack }

	[Flags]
	public enum ResupplyType
	{
		None = 0,
		Rearm = 1,
		Repair = 2
	}

	public interface IQuantizeBodyOrientationInfo : ITraitInfoInterface
	{
		int QuantizedBodyFacings(ActorInfo ai, SequenceProvider sequenceProvider, string race);
	}

	public interface IPlaceBuildingDecorationInfo : ITraitInfoInterface
	{
		IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition);
	}

	[RequireExplicitImplementation]
	public interface IBlocksProjectiles
	{
		WDist BlockingHeight { get; }

		PlayerRelationship ValidRelationships { get; }
	}

	[RequireExplicitImplementation]
	public interface IBlocksProjectilesInfo : ITraitInfoInterface { }

	[RequireExplicitImplementation]
	public interface INotifyOrderIssued
	{
		bool OrderIssued(World world, Target target);
	}

	[RequireExplicitImplementation]
	public interface INotifySold
	{
		void Selling(Actor self);
		void Sold(Actor self);
	}

	[RequireExplicitImplementation]
	public interface INotifyCustomLayerChanged
	{
		void CustomLayerChanged(Actor self, byte oldLayer, byte newLayer);
	}

	[RequireExplicitImplementation]
	public interface INotifyCenterPositionChanged
	{
		void CenterPositionChanged(Actor self, byte oldLayer, byte newLayer);
	}

	[RequireExplicitImplementation]
	public interface INotifyFinishedMoving
	{
		void FinishedMoving(Actor self, byte oldLayer, byte newLayer);
	}

	public interface IDemolishableInfo : ITraitInfoInterface { bool IsValidTarget(ActorInfo actorInfo, Actor saboteur); }
	public interface IDemolishable
	{
		bool IsValidTarget(Actor self, Actor saboteur);
		void Demolish(Actor self, Actor saboteur, int delay, BitSet<DamageType> damageTypes);
	}

	// Type tag for crush class bits
	public class CrushClass { }

	[RequireExplicitImplementation]
	public interface ICrushable
	{
		bool CrushableBy(Actor self, Actor crusher, BitSet<CrushClass> crushClasses);
		LongBitSet<PlayerBitMask> CrushableBy(Actor self, BitSet<CrushClass> crushClasses);
	}

	[RequireExplicitImplementation]
	public interface INotifyCrushed
	{
		void OnCrush(Actor self, Actor crusher, BitSet<CrushClass> crushClasses);
		void WarnCrush(Actor self, Actor crusher, BitSet<CrushClass> crushClasses);
	}

	[RequireExplicitImplementation]
	public interface INotifyAiming
	{
		void StartedAiming(Actor self, AttackBase attack);
		void StoppedAiming(Actor self, AttackBase attack);
	}

	[RequireExplicitImplementation]
	public interface INotifyAttack
	{
		void Attacking(Actor self, in Target target, Armament a, Barrel barrel);
		void PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel);
	}

	[RequireExplicitImplementation]
	public interface INotifyDamageStateChanged { void DamageStateChanged(Actor self, AttackInfo e); }

	[RequireExplicitImplementation]
	public interface INotifyDamage { void Damaged(Actor self, AttackInfo e); }
	[RequireExplicitImplementation]
	public interface INotifyKilled { void Killed(Actor self, AttackInfo e); }
	[RequireExplicitImplementation]
	public interface INotifyAppliedDamage { void AppliedDamage(Actor self, Actor damaged, AttackInfo e); }

	[RequireExplicitImplementation]
	public interface INotifyResupply
	{
		void BeforeResupply(Actor host, Actor target, ResupplyType types);
		void ResupplyTick(Actor host, Actor target, ResupplyType types);
	}

	[RequireExplicitImplementation]
	public interface INotifyBeingResupplied
	{
		void StartingResupply(Actor self, Actor host);
		void StoppingResupply(Actor self, Actor host);
	}

	[RequireExplicitImplementation]
	public interface INotifyTakeOff { void TakeOff(Actor self); }
	[RequireExplicitImplementation]
	public interface INotifyLanding { void Landing(Actor self); }

	[RequireExplicitImplementation]
	public interface INotifyPowerLevelChanged { void PowerLevelChanged(Actor self); }
	public interface INotifySupportPower { void Charged(Actor self); void Activated(Actor self); }

	public interface INotifyBuildingPlaced { void BuildingPlaced(Actor self); }
	public interface INotifyBurstComplete { void FiredBurst(Actor self, in Target target, Armament a); }
	public interface INotifyChat { bool OnChat(string from, string message); }
	public interface INotifyProduction { void UnitProduced(Actor self, Actor other, CPos exit); }
	public interface INotifyOtherProduction { void UnitProducedByOther(Actor self, Actor producer, Actor produced, string productionType, TypeDictionary init); }
	public interface INotifyDelivery { void IncomingDelivery(Actor self); void Delivered(Actor self); }
	public interface INotifyDocking { void Docked(Actor self, Actor harvester); void Undocked(Actor self, Actor harvester); }

	[RequireExplicitImplementation]
	public interface INotifyResourceAccepted { void OnResourceAccepted(Actor self, Actor refinery, string resourceType, int count, int value); }
	public interface INotifyParachute { void OnParachute(Actor self); void OnLanded(Actor self); }

	[RequireExplicitImplementation]
	public interface INotifyCapture { void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner, BitSet<CaptureType> captureTypes); }
	public interface INotifyDiscovered { void OnDiscovered(Actor self, Player discoverer, bool playNotification); }
	public interface IRenderActorPreviewInfo : ITraitInfoInterface { IEnumerable<IActorPreview> RenderPreview(ActorPreviewInitializer init); }
	public interface ICruiseAltitudeInfo : ITraitInfoInterface { WDist GetCruiseAltitude(); }

	public interface IHuskModifier { string HuskActor(Actor self); }

	public interface ISeedableResource { void Seed(Actor self); }

	[RequireExplicitImplementation]
	public interface INotifyInfiltrated { void Infiltrated(Actor self, Actor infiltrator, BitSet<TargetableType> types); }

	[RequireExplicitImplementation]
	public interface INotifyBlockingMove { void OnNotifyBlockingMove(Actor self, Actor blocking); }

	[RequireExplicitImplementation]
	public interface INotifyPassengerEntered { void OnPassengerEntered(Actor self, Actor passenger); }

	[RequireExplicitImplementation]
	public interface INotifyPassengerExited { void OnPassengerExited(Actor self, Actor passenger); }

	[RequireExplicitImplementation]
	public interface INotifyEnteredCargo { void OnEnteredCargo(Actor self, Actor cargo); }

	[RequireExplicitImplementation]
	public interface INotifyExitedCargo { void OnExitedCargo(Actor self, Actor cargo); }

	public interface INotifyHarvesterAction
	{
		void MovingToResources(Actor self, CPos targetCell);
		void MovingToRefinery(Actor self, Actor refineryActor);
		void MovementCancelled(Actor self);
		void Harvested(Actor self, string resourceType);
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

	public interface ITechTreePrerequisiteInfo : ITraitInfoInterface
	{
		IEnumerable<string> Prerequisites(ActorInfo info);
	}

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

	public interface IAcceptResourcesInfo : ITraitInfoInterface { }
	public interface IAcceptResources
	{
		void OnDock(Actor harv, DeliverResources dockOrder);
		int AcceptResources(string resourceType, int count = 1);
		CVec DeliveryOffset { get; }
		bool AllowDocking { get; }
	}

	public interface IProvidesAssetBrowserPalettes
	{
		IEnumerable<string> PaletteNames { get; }
	}

	[RequireExplicitImplementation]
	public interface IProvidesAssetBrowserColorPickerPalettes
	{
		IEnumerable<string> ColorPickerPaletteNames { get; }
	}

	public interface ICallForTransport
	{
		WDist MinimumDistance { get; }
		bool WantsTransport { get; }
		void MovementCancelled(Actor self);
		void RequestTransport(Actor self, CPos destination);
	}

	public interface IDeathActorInitModifier
	{
		void ModifyDeathActorInit(Actor self, TypeDictionary init);
	}

	public interface ITransformActorInitModifier
	{
		void ModifyTransformActorInit(Actor self, TypeDictionary init);
	}

	[RequireExplicitImplementation]
	public interface IDisableEnemyAutoTarget
	{
		bool DisableEnemyAutoTarget(Actor self, Actor attacker);
	}

	[RequireExplicitImplementation]
	public interface IOverrideAutoTarget
	{
		bool TryGetAutoTargetOverride(Actor self, out Target target);
	}

	[RequireExplicitImplementation]
	public interface IWallConnector
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
	public interface INotifyRearm
	{
		void RearmingStarted(Actor host, Actor other);
		void Rearming(Actor host, Actor other);
		void RearmingFinished(Actor host, Actor other);
	}

	[RequireExplicitImplementation]
	public interface IRenderInfantrySequenceModifier
	{
		bool IsModifyingSequence { get; }
		string SequencePrefix { get; }
	}

	[RequireExplicitImplementation]
	public interface IProductionCostModifierInfo : ITraitInfoInterface { int GetProductionCostModifier(TechTree techTree, string queue); }

	[RequireExplicitImplementation]
	public interface IProductionTimeModifierInfo : ITraitInfoInterface { int GetProductionTimeModifier(TechTree techTree, string queue); }

	[RequireExplicitImplementation]
	public interface ICashTricklerModifier { int GetCashTricklerModifier(); }

	[RequireExplicitImplementation]
	public interface IDamageModifier { int GetDamageModifier(Actor attacker, Damage damage); }

	[RequireExplicitImplementation]
	public interface ISpeedModifier { int GetSpeedModifier(); }

	[RequireExplicitImplementation]
	public interface IFirepowerModifier { int GetFirepowerModifier(); }

	[RequireExplicitImplementation]
	public interface IReloadModifier { int GetReloadModifier(); }

	[RequireExplicitImplementation]
	public interface IReloadAmmoModifier { int GetReloadAmmoModifier(); }

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
	public interface ICreatesShroudModifier { int GetCreatesShroudModifier(); }

	[RequireExplicitImplementation]
	public interface IRevealsShroudModifier { int GetRevealsShroudModifier(); }

	[RequireExplicitImplementation]
	public interface IDetectCloakedModifier { int GetDetectCloakedModifier(); }

	[RequireExplicitImplementation]
	public interface IResourceValueModifier { int GetResourceValueModifier(); }

	[RequireExplicitImplementation]
	public interface ICustomMovementLayer
	{
		byte Index { get; }
		bool InteractsWithDefaultLayer { get; }
		bool ReturnToGroundLayerOnIdle { get; }

		bool EnabledForLocomotor(LocomotorInfo li);
		short EntryMovementCost(LocomotorInfo li, CPos cell);
		short ExitMovementCost(LocomotorInfo li, CPos cell);

		byte GetTerrainIndex(CPos cell);
		WPos CenterOfCell(CPos cell);
	}

	public interface ICustomMovementLayerInfo : ITraitInfoInterface { }

	// For traits that want to be exposed to the "Deploy" UI button / hotkey
	[RequireExplicitImplementation]
	public interface IIssueDeployOrder
	{
		Order IssueDeployOrder(Actor self, bool queued);
		bool CanIssueDeployOrder(Actor self, bool queued);
	}

	public enum ActorPreviewType { PlaceBuilding, ColorPicker, MapEditorSidebar }

	[RequireExplicitImplementation]
	public interface IActorPreviewInitInfo : ITraitInfoInterface
	{
		IEnumerable<ActorInit> ActorPreviewInits(ActorInfo ai, ActorPreviewType type);
	}

	public interface IMove
	{
		Activity MoveTo(CPos cell, int nearEnough = 0, Actor ignoreActor = null,
		 	bool evaluateNearestMovableCell = false, Color? targetLineColor = null);
		Activity MoveWithinRange(in Target target, WDist range,
			WPos? initialTargetPosition = null, Color? targetLineColor = null);
		Activity MoveWithinRange(in Target target, WDist minRange, WDist maxRange,
			WPos? initialTargetPosition = null, Color? targetLineColor = null);
		Activity MoveFollow(Actor self, in Target target, WDist minRange, WDist maxRange,
			WPos? initialTargetPosition = null, Color? targetLineColor = null);
		Activity MoveToTarget(Actor self, in Target target,
			WPos? initialTargetPosition = null, Color? targetLineColor = null);
		Activity ReturnToCell(Actor self);
		Activity MoveIntoTarget(Actor self, in Target target);
		Activity LocalMove(Actor self, WPos fromPos, WPos toPos);
		int EstimatedMoveDuration(Actor self, WPos fromPos, WPos toPos);
		CPos NearestMoveableCell(CPos target);
		MovementType CurrentMovementTypes { get; set; }
		bool CanEnterTargetNow(Actor self, in Target target);
	}

	public interface IWrapMove
	{
		Activity WrapMove(Activity moveInner);
	}

	public interface IAircraftCenterPositionOffset
	{
		WVec PositionOffset { get; }
	}

	public interface IOverrideAircraftLanding
	{
		HashSet<string> LandableTerrainTypes { get; }
	}

	public interface IRadarSignature
	{
		void PopulateRadarSignatureCells(Actor self, List<(CPos Cell, Color Color)> destinationBuffer);
	}

	public interface IRadarColorModifier { Color RadarColorOverride(Actor self, Color color); }

	public interface IObjectivesPanel
	{
		string PanelName { get; }
		int ExitDelay { get; }
	}

	[RequireExplicitImplementation]
	public interface INotifyObjectivesUpdated
	{
		void OnObjectiveAdded(Player player, int objectiveID);
		void OnObjectiveCompleted(Player player, int objectiveID);
		void OnObjectiveFailed(Player player, int objectiveID);
	}

	[RequireExplicitImplementation]
	public interface INotifyWinStateChanged
	{
		void OnPlayerWon(Player winner);
		void OnPlayerLost(Player loser);
	}

	public interface INotifyCashTransfer
	{
		void OnAcceptingCash(Actor self, Actor donor);
		void OnDeliveringCash(Actor self, Actor acceptor);
	}

	[RequireExplicitImplementation]
	public interface ITargetableCells
	{
		(CPos Cell, SubCell SubCell)[] TargetableCells();
	}

	[RequireExplicitImplementation]
	public interface IPreventsShroudReset { bool PreventShroudReset(Actor self); }

	[RequireExplicitImplementation]
	public interface IBotEnabled { void BotEnabled(IBot bot); }

	[RequireExplicitImplementation]
	public interface IBotTick { void BotTick(IBot bot); }

	[RequireExplicitImplementation]
	public interface IBotRespondToAttack { void RespondToAttack(IBot bot, Actor self, AttackInfo e); }

	[RequireExplicitImplementation]
	public interface IBotPositionsUpdated
	{
		void UpdatedBaseCenter(CPos newLocation);
		void UpdatedDefenseCenter(CPos newLocation);
	}

	[RequireExplicitImplementation]
	public interface IBotNotifyIdleBaseUnits
	{
		void UpdatedIdleBaseUnits(List<Actor> idleUnits);
	}

	[RequireExplicitImplementation]
	public interface IBotRequestUnitProduction
	{
		void RequestUnitProduction(IBot bot, string requestedActor);
		int RequestedProductionCount(IBot bot, string requestedActor);
	}

	[RequireExplicitImplementation]
	public interface IBotRequestPauseUnitProduction
	{
		bool PauseUnitProduction { get; }
	}

	[RequireExplicitImplementation]
	public interface IEditorActorOptions : ITraitInfoInterface
	{
		IEnumerable<EditorActorOption> ActorOptions(ActorInfo ai, World world);
	}

	public abstract class EditorActorOption
	{
		public readonly string Name;
		public readonly int DisplayOrder;

		public EditorActorOption(string name, int displayOrder)
		{
			Name = name;
			DisplayOrder = displayOrder;
		}
	}

	public class EditorActorCheckbox : EditorActorOption
	{
		public readonly Func<EditorActorPreview, bool> GetValue;
		public readonly Action<EditorActorPreview, bool> OnChange;

		public EditorActorCheckbox(string name, int displayOrder,
			Func<EditorActorPreview, bool> getValue,
			Action<EditorActorPreview, bool> onChange)
			: base(name, displayOrder)
		{
			GetValue = getValue;
			OnChange = onChange;
		}
	}

	public class EditorActorSlider : EditorActorOption
	{
		public readonly float MinValue;
		public readonly float MaxValue;
		public readonly int Ticks;
		public readonly Func<EditorActorPreview, float> GetValue;
		public readonly Action<EditorActorPreview, float> OnChange;

		public EditorActorSlider(string name, int displayOrder,
			float minValue, float maxValue, int ticks,
			Func<EditorActorPreview, float> getValue,
			Action<EditorActorPreview, float> onChange)
			: base(name, displayOrder)
		{
			MinValue = minValue;
			MaxValue = maxValue;
			Ticks = ticks;
			GetValue = getValue;
			OnChange = onChange;
		}
	}

	public class EditorActorDropdown : EditorActorOption
	{
		public readonly Dictionary<string, string> Labels;
		public readonly Func<EditorActorPreview, string> GetValue;
		public readonly Action<EditorActorPreview, string> OnChange;

		public EditorActorDropdown(string name, int displayOrder,
			Dictionary<string, string> labels,
			Func<EditorActorPreview, string> getValue,
			Action<EditorActorPreview, string> onChange)
			: base(name, displayOrder)
		{
			Labels = labels;
			GetValue = getValue;
			OnChange = onChange;
		}
	}

	[RequireExplicitImplementation]
	public interface INotifyEditorPlacementInfo : ITraitInfoInterface
	{
		object AddedToEditor(EditorActorPreview preview, World editorWorld);
		void RemovedFromEditor(EditorActorPreview preview, World editorWorld, object data);
	}

	[RequireExplicitImplementation]
	public interface IPreventMapSpawn
	{
		bool PreventMapSpawn(World world, ActorReference actorReference);
	}

	[Flags]
	public enum MovementType
	{
		None = 0,
		Horizontal = 1,
		Vertical = 2,
		Turn = 4
	}

	[RequireExplicitImplementation]
	public interface INotifyMoving
	{
		void MovementTypeChanged(Actor self, MovementType type);
	}

	[RequireExplicitImplementation]
	public interface INotifyTimeLimit
	{
		void NotifyTimerExpired(Actor self);
	}

	[RequireExplicitImplementation]
	public interface ISelectable
	{
		string Class { get; }
	}

	public interface IDecoration
	{
		bool RequiresSelection { get; }

		IEnumerable<IRenderable> RenderDecoration(Actor self, WorldRenderer wr, ISelectionDecorations container);
	}

	[RequireExplicitImplementation]
	public interface ITiledTerrainRendererInfo : ITraitInfoInterface
	{
		bool ValidateTileSprites(ITemplatedTerrainInfo terrainInfo, Action<string> onError);
	}

	[RequireExplicitImplementation]
	public interface ITiledTerrainRenderer
	{
		Sprite MissingTile { get; }
		Sprite TileSprite(TerrainTile r, int? variant = null);
		Rectangle TemplateBounds(TerrainTemplateInfo template);
		IEnumerable<IRenderable> RenderUIPreview(WorldRenderer wr, TerrainTemplateInfo template, int2 origin, float scale);
		IEnumerable<IRenderable> RenderPreview(WorldRenderer wr, TerrainTemplateInfo template, WPos origin);
	}

	public interface IResourceLayerInfo : ITraitInfoInterface
	{
		bool TryGetTerrainType(string resourceType, out string terrainType);
		bool TryGetResourceIndex(string resourceType, out byte index);
	}

	[RequireExplicitImplementation]
	public interface IResourceLayer
	{
		event Action<CPos, string> CellChanged;
		ResourceLayerContents GetResource(CPos cell);
		int GetMaxDensity(string resourceType);
		bool CanAddResource(string resourceType, CPos cell, int amount = 1);
		int AddResource(string resourceType, CPos cell, int amount = 1);
		int RemoveResource(string resourceType, CPos cell, int amount = 1);
		void ClearResources(CPos cell);

		bool IsVisible(CPos cell);
		bool IsEmpty { get; }
		IResourceLayerInfo Info { get; }
	}

	[RequireExplicitImplementation]
	public interface IResourceRenderer
	{
		IEnumerable<string> ResourceTypes { get; }
		string GetRenderedResourceType(CPos cell);
		string GetRenderedResourceTooltip(CPos cell);
		IEnumerable<IRenderable> RenderUIPreview(WorldRenderer wr, string resourceType, int2 origin, float scale);
		IEnumerable<IRenderable> RenderPreview(WorldRenderer wr, string resourceType, WPos origin);
	}

	[RequireExplicitImplementation]
	public interface IRadarTerrainLayer
	{
		event Action<CPos> CellEntryChanged;
		bool TryGetTerrainColorPair(MPos uv, out (Color Left, Color Right) value);
	}

	/// <summary>
	/// When performing locomotion or pathfinding related checks,
	/// determines whether the blocking rules will be applied when encountering other actors.
	/// </summary>
	public enum BlockedByActor
	{
		/// <summary>
		/// Actors on the map are ignored, as if they were not present.
		/// An actor can only be blocked by impassable terrain.
		/// An actor can never be blocked by other actors. The blocking rules will never be evaluated.
		/// </summary>
		None,

		/// <summary>
		/// Actors on the map that are moving, or moveable &amp; allied are ignored.
		/// An actor is Immovable is any of the following applies:
		/// <list type="bullet">
		/// <item>Lacks the <see cref="Mobile"/> trait.</item>
		/// <item>The <see cref="Mobile"/> trait has <see cref="ConditionalTrait{MobileInfo}.IsTraitDisabled"/> or
		/// <see cref="PausableConditionalTrait{MobileInfo}.IsTraitPaused"/> as true.</item>
		/// <item>The <see cref="Mobile"/> trait has <see cref="Mobile.IsImmovable"/> as true.</item>
		/// </list>
		/// Note the above definition means an actor can be Movable, but may not be Moving, i.e. it is Stationary.
		/// Actors are allied if their owners have the <see cref="PlayerRelationship.Ally"/> relationship.
		/// An actor can be blocked by impassable terrain.
		/// An actor can be blocked by immovable actors *if* they are deemed as blocking by the blocking rules.
		/// An actor can be blocked by an actor capable of moving, if it is not an ally and *if* they are deemed as
		/// blocking by the blocking rules.
		/// An actor can never be blocked by an allied actor capable of moving, even if the other actor is stationary.
		/// An actor can never be blocked by a moving actor.
		/// </summary>
		Immovable,

		/// <summary>
		/// Actors on the map that are moving are ignored.
		/// An actor is moving if both of the following apply:
		/// <list type="bullet">
		/// <item>It is a Moveable actor (see <see cref="Immovable"/>).</item>
		/// <item><see cref="Mobile.CurrentMovementTypes"/> contains the flag <see cref="MovementType.Horizontal"/>.</item>
		/// </list>
		/// Otherwise the actor is deemed to be Stationary.
		/// An actor can be blocked by impassable terrain.
		/// An actor can be blocked by immovable actors and stationary actors *if* they are deemed as blocking by the
		/// blocking rules.
		/// An actor can never be blocked by a moving actor.
		/// </summary>
		Stationary,

		/// <summary>
		/// Actors on the map are not ignored.
		/// An actor can be blocked by impassable terrain.
		/// An actor can be blocked by immovable actors, stationary actors and moving actors *if* they are deemed as
		/// blocking by the blocking rules.
		/// </summary>
		All
	}

	public interface IPositionableInfo : IOccupySpaceInfo
	{
		bool CanEnterCell(World world, Actor self, CPos cell, SubCell subCell = SubCell.FullCell, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All);
	}

	public interface IPositionable : IOccupySpace
	{
		bool CanExistInCell(CPos location);
		bool IsLeavingCell(CPos location, SubCell subCell = SubCell.Any);
		bool CanEnterCell(CPos location, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All);
		SubCell GetValidSubCell(SubCell preferred = SubCell.Any);
		SubCell GetAvailableSubCell(CPos location, SubCell preferredSubCell = SubCell.Any, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All);
		void SetPosition(Actor self, CPos cell, SubCell subCell = SubCell.Any);
		void SetPosition(Actor self, WPos pos);
		void SetCenterPosition(Actor self, WPos pos);
	}

	public interface IPathFinder
	{
		/// <summary>
		/// Calculates a path for the actor from multiple possible sources to target.
		/// Returned path is *reversed* and given target to source.
		/// The shortest path between a source and the target is returned.
		/// </summary>
		List<CPos> FindPathToTargetCell(
			Actor self, IEnumerable<CPos> sources, CPos target, BlockedByActor check,
			Func<CPos, int> customCost = null,
			Actor ignoreActor = null,
			bool laneBias = true);

		/// <summary>
		/// Calculates a path for the actor from multiple possible sources, whilst searching for an acceptable target.
		/// Returned path is *reversed* and given target to source.
		/// The shortest path between a source and a discovered target is returned.
		/// </summary>
		List<CPos> FindPathToTargetCellByPredicate(
			Actor self, IEnumerable<CPos> sources, Func<CPos, bool> targetPredicate, BlockedByActor check,
			Func<CPos, int> customCost = null,
			Actor ignoreActor = null,
			bool laneBias = true);

		/// <summary>
		/// Determines if a path exists between source and target.
		/// Only terrain is taken into account, i.e. as if <see cref="BlockedByActor.None"/> was given.
		/// This would apply for any actor using the given <see cref="Locomotor"/>.
		/// </summary>
		bool PathExistsForLocomotor(Locomotor locomotor, CPos source, CPos target);
	}
}
