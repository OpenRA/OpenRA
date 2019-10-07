#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("This actor can detonate mines.")]
	public class MineDetonatorInfo : ITraitInfo
	{
		[Desc("Define detonator type(s) checked by Mine and CrateAction for validity. Leave empty if actor is supposed to be able to detonate any mines.")]
		public readonly BitSet<MineDetonatorType> DetonatorTypes = default(BitSet<MineDetonatorType>);
		public bool All { get { return DetonatorTypes == default(BitSet<MineDetonatorType>); } }

		public object Create(ActorInitializer init) { return new MineDetonator(this); }
	}

	public class MineDetonator
	{
		public readonly MineDetonatorInfo Info;
		public MineDetonator(MineDetonatorInfo info)
		{
			Info = info;
		}
	}

	public class MineDetonatorType { }

	class MineInfo : ITraitInfo
	{
		public readonly bool AvoidFriendly = true;
		public readonly BitSet<DamageType> DetonateDamageType = default(BitSet<DamageType>);

		[Desc("Define actors that can detonate mines by setting one of these into the DetonatorTypes field from the MineDetonator trait.")]
		public readonly BitSet<MineDetonatorType> ValidDetonatorTypes = new BitSet<MineDetonatorType>("mine-detonator");

		public object Create(ActorInitializer init) { return new Mine(init, this); }
	}

	class Mine : INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		public MineInfo Info { get; private set; }
		bool detonated;
		readonly MineLocations mineLocation;

		public Mine(ActorInitializer init, MineInfo info)
		{
			Info = info;
			mineLocation = init.World.WorldActor.TraitOrDefault<MineLocations>();
		}

		public void Detonate(Actor self)
		{
			if (detonated)
				return;

			var detonators = self.World.ActorMap.GetActorsAt(self.Location).Where(a =>
			 {
				 if (!a.IsAtGroundLevel())
					 return false;

				 var mineDetonatorInfo = a.Info.TraitInfoOrDefault<MineDetonatorInfo>();
				 if (mineDetonatorInfo == null)
					 return false;

				 if (a.Info.HasTraitInfo<MineImmuneInfo>() || (a.Owner.Stances[self.Owner] == Stance.Ally && Info.AvoidFriendly))
					 return false;

				 // Make sure that the actor can detonate this mine type
				 return mineDetonatorInfo.All || mineDetonatorInfo.DetonatorTypes.Overlaps(Info.ValidDetonatorTypes);
			 });

			foreach (var detonator in detonators)
			{
				detonated = true;
				self.Kill(detonator, Info.DetonateDamageType);
			}

			if (detonated)
				self.Dispose();
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			mineLocation.Add(self);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			mineLocation.Remove(self);
		}
	}

	[Desc("Tag trait for stuff that should not trigger mines.")]
	class MineImmuneInfo : TraitInfo<MineImmune> { }

	class MineImmune { }

	class MineLocationsInfo : TraitInfo<MineLocations> { }

	class MineLocations : IWorldLoaded, INotifyEnteredCell
	{
		CellLayer<Actor> mines;

		public void Add(Actor self)
		{
			mines[self.Location] = self;
		}

		public void Remove(Actor self)
		{
			mines[self.Location] = null;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			mines = new CellLayer<Actor>(w.Map);
		}

		public bool Occupied(CPos cPos)
		{
			return mines[cPos] != null;
		}

		void INotifyEnteredCell.EnteredCell(Actor actor, CPos cell)
		{
			var mine = mines[cell];

			if (mine == null)
				return;

			var mineTrait = mine.Trait<Mine>();

			mineTrait.Detonate(mine);
		}
	}
}
