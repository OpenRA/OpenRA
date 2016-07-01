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

using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
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
	public interface INotifyAttack
	{
		void Attacking(Actor self, Target target, Armament a, Barrel barrel);
		void PreparingAttack(Actor self, Target target, Armament a, Barrel barrel);
	}

	public interface INotifyBurstComplete { void FiredBurst(Actor self, Target target, Armament a); }
	public interface INotifyCharging { void Charging(Actor self, Target target); }
	public interface INotifyChat { bool OnChat(string from, string message); }
	public interface INotifyParachuteLanded { void OnLanded(Actor ignore); }
	public interface IRenderActorPreviewInfo : ITraitInfo { IEnumerable<IActorPreview> RenderPreview(ActorPreviewInitializer init); }
	public interface ICruiseAltitudeInfo : ITraitInfo { WDist GetCruiseAltitude(); }

	public interface IUpgradable
	{
		IEnumerable<string> UpgradeTypes { get; }
		bool AcceptsUpgradeLevel(Actor self, string type, int level);
		void UpgradeLevelChanged(Actor self, string type, int oldLevel, int newLevel);
	}

	// Implement to construct before UpgradeManager
	public interface IUpgradableInfo : ITraitInfo { }

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
		bool WantsTransport { get; set; }
		void MovementCancelled(Actor self);
		void RequestTransport(CPos destination, Activity afterLandActivity);
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
}
