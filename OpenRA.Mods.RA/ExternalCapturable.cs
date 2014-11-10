#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("This actor can be captured by a unit with ExternalCaptures: trait.")]
	public class ExternalCapturableInfo : ITraitInfo
	{
		[Desc("Type of actor (the ExternalCaptures: trait defines what Types it can capture).")]
		public readonly string Type = "building";
		public readonly bool AllowAllies = false;
		public readonly bool AllowNeutral = true;
		public readonly bool AllowEnemies = true;
		[Desc("Seconds it takes to change the owner.", "You might want to add a ExternalCapturableBar: trait, too.")]
		public readonly int CaptureCompleteTime = 15;

		public bool CanBeTargetedBy(Actor captor, Player owner)
		{
			var c = captor.TraitOrDefault<ExternalCaptures>();
			if (c == null)
				return false;

			var playerRelationship = owner.Stances[captor.Owner];
			if (playerRelationship == Stance.Ally && !AllowAllies)
				return false;

			if (playerRelationship == Stance.Enemy && !AllowEnemies)
				return false;

			if (playerRelationship == Stance.Neutral && !AllowNeutral)
				return false;

			if (!c.Info.CaptureTypes.Contains(Type))
				return false;

			return true;
		}

		public object Create(ActorInitializer init) { return new ExternalCapturable(init.self, this); }
	}

	public class ExternalCapturable : ITick
	{
		[Sync] public int CaptureProgressTime = 0;
		[Sync] public Actor Captor;
		private Actor self;
		public ExternalCapturableInfo Info;
		public bool CaptureInProgress { get { return Captor != null; } }

		public ExternalCapturable(Actor self, ExternalCapturableInfo info)
		{
			this.self = self;
			Info = info;
		}

		public void BeginCapture(Actor captor)
		{
			var building = self.TraitOrDefault<Building>();
			if (building != null)
				building.Lock();

			Captor = captor;
		}

		public void EndCapture()
		{
			var building = self.TraitOrDefault<Building>();
			if (building != null)
				building.Unlock();

			Captor = null;
		}

		public void Tick(Actor self)
		{
			if (Captor != null && (!Captor.IsInWorld || Captor.IsDead))
				EndCapture();

			if (!CaptureInProgress)
				CaptureProgressTime = 0;
			else
				CaptureProgressTime++;
		}
	}
}
