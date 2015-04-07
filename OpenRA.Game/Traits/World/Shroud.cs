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

namespace OpenRA.Traits
{
	[Desc("Required for shroud and fog visibility checks. Add this to the player actor.")]
	public class ShroudInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new Shroud(init.Self); }
	}

	public class Shroud
	{
		[Sync] public bool Disabled;

		public event Action<IEnumerable<CPos>> CellsChanged;

		readonly Actor self;
		readonly Map map;

		readonly CellLayer<short> visibleCount;
		readonly CellLayer<bool> explored;
		readonly CellLayer<short> generatedDisruptionCount;
		readonly CellLayer<short> generatedShroudCount;

		readonly Lazy<IFogVisibilityModifier[]> fogVisibilities;

		// Cache of visibility that was added, so no matter what crazy trait code does, it
		// can't make us invalid.
		readonly Dictionary<Actor, CPos[]> visibility = new Dictionary<Actor, CPos[]>();
		readonly Dictionary<Actor, CPos[]> disruptionGenerators = new Dictionary<Actor, CPos[]>();
		readonly Dictionary<Actor, CPos[]> shroudGenerators = new Dictionary<Actor, CPos[]>();

		public int Hash { get; private set; }

		static readonly Func<MPos, bool> TruthPredicate = _ => true;
		readonly Func<MPos, bool> shroudEdgeTest;
		readonly Func<MPos, bool> fastExploredTest;
		readonly Func<MPos, bool> slowExploredTest;
		readonly Func<MPos, bool> fastVisibleTest;
		readonly Func<MPos, bool> slowVisibleTest;

		public Shroud(Actor self)
		{
			this.self = self;
			map = self.World.Map;

			visibleCount = new CellLayer<short>(map);
			explored = new CellLayer<bool>(map);
			generatedDisruptionCount = new CellLayer<short>(map);
			generatedShroudCount = new CellLayer<short>(map);

			self.World.ActorAdded += a => { CPos[] visible = null; AddVisibility(a, ref visible); };
			self.World.ActorRemoved += RemoveVisibility;

			if (self.World.LobbyInfo.GlobalSettings.Fog)
			{
				self.World.ActorAdded += a => { CPos[] underDisruptionField = null; AddDisruptionGenerator(a, ref underDisruptionField); };
				self.World.ActorRemoved += RemoveDisruptionGenerator;
			}

			if (self.World.LobbyInfo.GlobalSettings.Shroud)
			{
				self.World.ActorAdded += a => { CPos[] underShroud = null; AddShroudGenerator(a, ref underShroud); };
				self.World.ActorRemoved += RemoveShroudGenerator;
			}

			fogVisibilities = Exts.Lazy(() => self.TraitsImplementing<IFogVisibilityModifier>().ToArray());

			shroudEdgeTest = map.Contains;
			fastExploredTest = IsExploredCore;
			slowExploredTest = IsExplored;
			fastVisibleTest = IsVisibleCore;
			slowVisibleTest = IsVisible;
		}

		void Invalidate(IEnumerable<CPos> changed)
		{
			if (CellsChanged != null)
				CellsChanged(changed);

			var oldHash = Hash;
			Hash = Sync.HashPlayer(self.Owner) + self.World.WorldTick * 3;

			// Invalidate may be called multiple times in one world tick, which is decoupled from rendering.
			if (oldHash == Hash)
				Hash += 1;
		}

		public static void UpdateVisibility(IEnumerable<Shroud> shrouds, Actor actor)
		{
			CPos[] visbility = null;
			foreach (var shroud in shrouds)
				shroud.UpdateVisibility(actor, ref visbility);
		}

		static CPos[] FindVisibleTiles(Actor actor, WRange range)
		{
			return GetVisOrigins(actor).SelectMany(o => FindVisibleTiles(actor.World, o, range)).Distinct().ToArray();
		}

		static IEnumerable<CPos> FindVisibleTiles(World world, CPos position, WRange radius)
		{
			var map = world.Map;
			var r = (radius.Range + 1023) / 1024;
			var limit = radius.Range * radius.Range;
			var pos = map.CenterOfCell(position);

			foreach (var cell in map.FindTilesInCircle(position, r))
				if ((map.CenterOfCell(cell) - pos).HorizontalLengthSquared <= limit)
					yield return cell;
		}

		void AddVisibility(Actor a, ref CPos[] visible)
		{
			var rs = a.TraitOrDefault<RevealsShroud>();
			if (rs == null || !a.Owner.IsAlliedWith(self.Owner) || rs.Range == WRange.Zero)
				return;

			// Lazily generate the visible tiles, allowing the caller to re-use them if desired.
			visible = visible ?? FindVisibleTiles(a, rs.Range);

			foreach (var c in visible)
			{
				var uv = c.ToMPos(map);
				visibleCount[uv]++;
				explored[uv] = true;
			}

			if (visibility.ContainsKey(a))
				throw new InvalidOperationException("Attempting to add duplicate actor visibility");

			visibility[a] = visible;
			Invalidate(visible);
		}

		void RemoveVisibility(Actor a)
		{
			CPos[] visible;
			if (!visibility.TryGetValue(a, out visible))
				return;

			foreach (var c in visible)
				visibleCount[c.ToMPos(map)]--;

			visibility.Remove(a);
			Invalidate(visible);
		}

		void UpdateVisibility(Actor a, ref CPos[] visible)
		{
			// Actors outside the world don't have any vis
			if (!a.IsInWorld)
				return;

			RemoveVisibility(a);
			AddVisibility(a, ref visible);
		}

		void AddDisruptionGenerator(Actor a, ref CPos[] underDisruption)
		{
			var cd = a.TraitOrDefault<CreatesDisruptionField>();
			if (cd == null || a.Owner.IsAlliedWith(self.Owner) || cd.Range == WRange.Zero)
				return;

			// Lazily generate the disrupted tiles, allowing the caller to re-use them if desired.
			underDisruption = underDisruption ?? FindVisibleTiles(a, cd.Range);

			foreach (var c in underDisruption)
				generatedDisruptionCount[c]++;

			if (disruptionGenerators.ContainsKey(a))
				throw new InvalidOperationException("Attempting to add duplicate disruption generator");

			disruptionGenerators[a] = underDisruption;
			Invalidate(underDisruption);
		}

		void RemoveDisruptionGenerator(Actor a)
		{
			CPos[] underDisruption;
			if (!disruptionGenerators.TryGetValue(a, out underDisruption))
				return;

			foreach (var c in underDisruption)
				generatedDisruptionCount[c]--;

			disruptionGenerators.Remove(a);
			Invalidate(underDisruption);
		}

		public static void UpdateDisruptionGenerator(IEnumerable<Shroud> shrouds, Actor actor)
		{
			CPos[] underDisruption = null;
			foreach (var shroud in shrouds)
				shroud.UpdateDisruptionGenerator(actor, ref underDisruption);
		}

		void UpdateDisruptionGenerator(Actor a, ref CPos[] underDisruption)
		{
			RemoveDisruptionGenerator(a);
			AddDisruptionGenerator(a, ref underDisruption);
		}

		void AddShroudGenerator(Actor a, ref CPos[] underShroud)
		{
			var cs = a.TraitOrDefault<CreatesShroud>();
			if (cs == null || a.Owner.IsAlliedWith(self.Owner) || cs.Range == WRange.Zero
				|| (self.World.LobbyInfo.GlobalSettings.Fog && a.HasTrait<CreatesDisruptionField>()))
				return;

			// Lazily generate the shrouded tiles, allowing the caller to re-use them if desired.
			underShroud = underShroud ?? FindVisibleTiles(a, cs.Range);

			foreach (var c in underShroud)
				generatedShroudCount[c]++;

			if (shroudGenerators.ContainsKey(a))
				throw new InvalidOperationException("Attempting to add duplicate shroud generator");

			shroudGenerators[a] = underShroud;
			Invalidate(underShroud);
		}

		void RemoveShroudGenerator(Actor a)
		{
			CPos[] underShroud;
			if (!shroudGenerators.TryGetValue(a, out underShroud))
				return;

			foreach (var c in underShroud)
				generatedShroudCount[c]--;

			shroudGenerators.Remove(a);
			Invalidate(underShroud);
		}

		public static void UpdateShroudGenerator(IEnumerable<Shroud> shrouds, Actor actor)
		{
			CPos[] underShroud = null;
			foreach (var shroud in shrouds)
				shroud.UpdateShroudGenerator(actor, ref underShroud);
		}

		void UpdateShroudGenerator(Actor a, ref CPos[] underShroud)
		{
			RemoveShroudGenerator(a);
			AddShroudGenerator(a, ref underShroud);
		}

		public bool IsUnderDisruptionField(MPos[] fp)
		{
			return fp.All(c => generatedDisruptionCount[c] > 0);
		}

		public bool IsUnderDisruptionField(CPos[] fp)
		{
			return fp.All(c => generatedDisruptionCount[c] > 0);
		}

		public void UpdatePlayerStance(World w, Player player, Stance oldStance, Stance newStance)
		{
			if (oldStance == newStance)
				return;

			foreach (var a in w.Actors.Where(a => a.Owner == player))
			{
				CPos[] visible = null;
				UpdateVisibility(a, ref visible);
				CPos[] underDisruptionField = null;
				UpdateDisruptionGenerator(a, ref underDisruptionField);
				CPos[] underShroud = null;
				UpdateShroudGenerator(a, ref underShroud);
			}
		}

		public static IEnumerable<CPos> GetVisOrigins(Actor a)
		{
			var ios = a.OccupiesSpace;
			if (ios != null)
			{
				var cells = ios.OccupiedCells();
				if (cells.Any())
					return cells.Select(c => c.First);
			}

			return new[] { a.World.Map.CellContaining(a.CenterPosition) };
		}

		public void Explore(World world, CPos center, WRange range)
		{
			var changed = new List<CPos>();
			foreach (var c in FindVisibleTiles(world, center, range))
			{
				if (!explored[c])
				{
					explored[c] = true;
					changed.Add(c);
				}
			}

			Invalidate(changed);
		}

		public void Explore(Shroud s)
		{
			if (map.Bounds != s.map.Bounds)
				throw new ArgumentException("The map bounds of these shrouds do not match.", "s");

			var changed = new List<CPos>();
			foreach (var uv in map.Cells.MapCoords)
			{
				if (!explored[uv] && s.explored[uv])
				{
					explored[uv] = true;
					changed.Add(uv.ToCPos(map));
				}
			}

			Invalidate(changed);
		}

		public void ExploreAll(World world)
		{
			var changed = new List<CPos>();
			foreach (var uv in map.Cells.MapCoords)
			{
				if (!explored[uv])
				{
					explored[uv] = true;
					changed.Add(uv.ToCPos(map));
				}
			}

			Invalidate(changed);
		}

		public void ResetExploration()
		{
			var changed = new List<CPos>();
			foreach (var uv in map.Cells.MapCoords)
			{
				var visible = visibleCount[uv] > 0;
				if (explored[uv] != visible)
				{
					explored[uv] = visible;
					changed.Add(uv.ToCPos(map));
				}
			}

			Invalidate(changed);
		}

		public bool IsExplored(CPos cell)
		{
			return IsExplored(cell.ToMPos(map));
		}

		public bool IsExplored(MPos uv)
		{
			if (!map.Contains(uv))
				return false;

			if (!ShroudEnabled)
				return true;

			return IsExploredCore(uv);
		}

		bool ShroudEnabled { get { return !Disabled && self.World.LobbyInfo.GlobalSettings.Shroud; } }

		bool IsExploredCore(MPos uv)
		{
			return explored[uv] && (generatedShroudCount[uv] == 0 || visibleCount[uv] > 0);
		}

		public Func<MPos, bool> IsExploredTest(CellRegion region)
		{
			// If the region to test extends outside the map we must use the slow test that checks the map boundary every time.
			if (!map.Cells.Contains(region))
				return slowExploredTest;

			// If shroud isn't enabled, then we can see everything inside the map.
			if (!ShroudEnabled)
				return shroudEdgeTest;

			// If shroud is enabled, we can use the fast test that just does the core check.
			return fastExploredTest;
		}

		public bool IsExplored(Actor a)
		{
			return GetVisOrigins(a).Any(IsExplored);
		}

		public bool IsVisible(CPos cell)
		{
			var uv = cell.ToMPos(map);
			return IsVisible(uv);
		}

		bool IsVisible(MPos uv)
		{
			if (!map.Contains(uv))
				return false;

			if (!FogEnabled)
				return true;

			return IsVisibleCore(uv);
		}

		bool FogEnabled { get { return !Disabled && self.World.LobbyInfo.GlobalSettings.Fog; } }

		bool IsVisibleCore(MPos uv)
		{
			return visibleCount[uv] > 0;
		}

		public Func<MPos, bool> IsVisibleTest(CellRegion region)
		{
			// If the region to test extends outside the map we must use the slow test that checks the map boundary every time.
			if (!map.Cells.Contains(region))
				return slowVisibleTest;

			// If fog isn't enabled, then we can see everything.
			if (!FogEnabled)
				return TruthPredicate;

			// If fog is enabled, we can use the fast test that just does the core check.
			return fastVisibleTest;
		}

		// Actors are hidden under shroud, but not under fog by default
		public bool IsVisible(Actor a)
		{
			if (a.TraitsImplementing<IVisibilityModifier>().Any(t => !t.IsVisible(a, self.Owner)))
				return false;

			return a.Owner.IsAlliedWith(self.Owner) || IsExplored(a);
		}

		public bool IsTargetable(Actor a)
		{
			IEnumerable<CPos> vis = GetVisOrigins(a);
			bool concealed = vis.All(c => generatedDisruptionCount[c] > 0 || generatedShroudCount[c] > 0);
			if (concealed)
			{
				if (FogEnabled)
					return vis.Any(IsVisible);
				else if (ShroudEnabled)
					return vis.Any(c => visibleCount[c.ToMPos(map)] > 0);
				else
					return true;
			}

			if (HasFogVisibility())
				return true;

			if (a.TraitsImplementing<IVisibilityModifier>().Any(t => !t.IsVisible(a, self.Owner)))
				return false;

			return vis.Any(IsVisible);
		}

		public bool HasFogVisibility()
		{
			return fogVisibilities.Value.Any(f => f.HasFogVisibility(self.Owner));
		}
	}
}
