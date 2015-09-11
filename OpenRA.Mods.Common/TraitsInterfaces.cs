#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public interface ISpriteBody
	{
		void PlayCustomAnimation(Actor self, string newAnimation, Action after);
		void PlayCustomAnimationRepeating(Actor self, string name);
		void PlayCustomAnimationBackwards(Actor self, string name, Action after);
	}

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

	public interface IQuantizeBodyOrientationInfo : ITraitInfo
	{
		int QuantizedBodyFacings(ActorInfo ai, SequenceProvider sequenceProvider, string race);
	}

	public interface INotifyResourceClaimLost
	{
		void OnNotifyResourceClaimLost(Actor self, ResourceClaim claim, Actor claimer);
	}

	public interface IPlaceBuildingDecoration
	{
		IEnumerable<IRenderable> Render(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition);
	}

	public interface INotifyAttack { void Attacking(Actor self, Target target, Armament a, Barrel barrel); }
	public interface INotifyCharging { void Charging(Actor self, Target target); }
	public interface INotifyChat { bool OnChat(string from, string message); }
	public interface INotifyParachuteLanded { void OnLanded(); }
	public interface IRenderActorPreviewInfo { IEnumerable<IActorPreview> RenderPreview(ActorPreviewInitializer init); }
	public interface ICruiseAltitudeInfo { WDist GetCruiseAltitude(); }

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
		Sprite Sprite();
		string Palette();
		float Scale();
		float2 Offset(float2 iconSize);
		bool IsOverlayActive(ActorInfo ai);
	}

	public interface INotifyTransform { void BeforeTransform(Actor self); void OnTransform(Actor self); void AfterTransform(Actor toActor); }

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

	public interface IProvidesRangesInfo : ITraitInfo
	{
		bool ProvidesRanges(string type, string variant, ActorInfo ai, World w);
		IEnumerable<IRanged> GetRanges(string type, string variant, ActorInfo ai, World w);
	}

	public interface IProvidesRanges
	{
		// return true if implementer may provide type during its lifetime
		bool ProvidesRanges(string type, string variant);
		IEnumerable<IRanged> GetRanges(string type, string variant);
	}

	public static class ProvidesRanges
	{
		public static readonly IEnumerable<IRanged> NoRanges = Enumerable.Empty<IRanged>();
	}
}
