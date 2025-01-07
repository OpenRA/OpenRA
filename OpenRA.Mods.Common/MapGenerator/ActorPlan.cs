#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.MapGenerator
{
	/// <summary>Description of an actor to add to a map.</summary>
	public sealed class ActorPlan
	{
		public readonly Map Map;
		public readonly ActorInfo Info;
		public readonly ActorReference Reference;

		public CPos Location
		{
			get => Reference.Get<LocationInit>().Value;
			set
			{
				Reference.RemoveAll<LocationInit>();
				Reference.Add(new LocationInit(value));
			}
		}

		public WPos WPosLocation
		{
			get => CellLayerUtils.CPosToWPos(Location, Map.Grid.Type);
			set => Location = CellLayerUtils.WPosToCPos(value, Map.Grid.Type);
		}

		/// <summary>
		/// WPos representation of actor's center.
		/// For example, A 1x2 actor on a Rectangular grid will have +WVec(0, 512, 0)
		/// offset to its WPosLocation.
		/// </summary>
		public WPos WPosCenterLocation
		{
			get => WPosLocation + WVecCenterOffset();
			set => WPosLocation = value - WVecCenterOffset();
		}

		/// <summary>
		/// Create an ActorPlan from a reference. The referenced actor becomes owned.
		/// </summary>
		public ActorPlan(Map map, ActorReference reference)
		{
			Map = map;
			Reference = reference;
			if (!map.Rules.Actors.TryGetValue(Reference.Type.ToLowerInvariant(), out Info))
				throw new ArgumentException($"MultiBrush Actor of unknown type `{Reference.Type.ToLowerInvariant()}`");
		}

		/// <summary>
		/// Create an ActorPlan containing a new Neutral-owned actor of the given type.
		/// </summary>
		public ActorPlan(Map map, string type)
			: this(map, ActorFromType(type))
		{ }

		/// <summary>
		/// Create a cloned actor plan, cloning the underlying ActorReference.
		/// </summary>
		public ActorPlan Clone()
		{
			return new ActorPlan(Map, Reference.Clone());
		}

		static ActorReference ActorFromType(string type)
		{
			return new ActorReference(type)
			{
				new LocationInit(default),
				new OwnerInit("Neutral"),
			};
		}

		/// <summary>
		/// The footprint of the actor (influenced by its location).
		/// </summary>
		public IReadOnlyDictionary<CPos, SubCell> Footprint()
		{
			var location = Location;
			var ios = Info.TraitInfoOrDefault<IOccupySpaceInfo>();
			var subCellInit = Reference.GetOrDefault<SubCellInit>();
			var subCell = subCellInit != null ? subCellInit.Value : SubCell.Any;

			var occupiedCells = ios?.OccupiedCells(Info, location, subCell);
			if (occupiedCells == null || occupiedCells.Count == 0)
				return new Dictionary<CPos, SubCell>() { { location, SubCell.FullCell } };
			else
				return occupiedCells;
		}

		/// <summary>
		/// Relocates the actor such that the top-most, left-most footprint
		/// square is at (0, 0).
		/// </summary>
		public ActorPlan AlignFootprint()
		{
			var footprint = Footprint();
			var first = footprint.Select(kv => kv.Key).OrderBy(cpos => (cpos.Y, cpos.X)).First();
			Location -= new CVec(first.X, first.Y);
			return this;
		}

		/// <summary>
		/// <para>
		/// Return a WVec center offset (from its WPosLocation) for the actor.
		/// </para>
		/// <para>
		/// For example, for a 1x2 actor on a Rectangular grid, this would be WVec(0, 512, 0).
		/// </para>
		/// </summary>
		WVec WVecCenterOffset()
		{
			var bi = Info.TraitInfoOrDefault<BuildingInfo>();
			if (bi == null)
				return new WVec(0, 0, 0);

			var left = int.MaxValue;
			var right = int.MinValue;
			var top = int.MaxValue;
			var bottom = int.MinValue;
			foreach (var (cvec, type) in bi.Footprint)
			{
				if (type == FootprintCellType.Empty)
					continue;
				left = Math.Min(left, cvec.X);
				top = Math.Min(top, cvec.Y);
				right = Math.Max(right, cvec.X);
				bottom = Math.Max(bottom, cvec.Y);
			}

			return CellLayerUtils.CVecToWVec(new CVec(left + right, top + bottom), Map.Grid.Type) / 2;
		}
	}
}
