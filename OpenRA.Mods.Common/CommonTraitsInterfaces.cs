#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	public interface INotifyResourceClaimLost
	{
		void OnNotifyResourceClaimLost(Actor self, ResourceClaim claim, Actor claimer);
	}

	public interface INotifyChat { bool OnChat(string from, string message); }
	public interface IRenderActorPreviewInfo { IEnumerable<IActorPreview> RenderPreview (ActorPreviewInitializer init); }
	public interface ICruiseAltitudeInfo { WRange GetCruiseAltitude(); }

	public interface IConditional
	{
		IEnumerable<string> ConditionTypes { get; }
		bool AcceptsConditionLevel(Actor self, string type, int level);
		void ConditionLevelChanged(Actor self, string type, int oldLevel, int newLevel);
	}

	public interface INotifyHarvesterAction
	{
		void MovingToResources(Actor self, CPos targetCell, Activity next);
		void MovingToRefinery(Actor self, CPos targetCell, Activity next);
		void MovementCancelled(Actor self);
		void Harvested(Actor self, ResourceType resource);
	}
}
