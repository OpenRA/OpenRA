// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text.Json;
using OpenRA;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Primitives;
using OpenRA.Network;
using OpenRA.Graphics;
using OpenRA.Mods.Common.LoadScreens;
namespace OpenRA.TerrainProbe;

public static class ScenarioTrace
{
	public static void Write(World world, string kind, string payload)
	{
		var directory = Path.Combine(Platform.SupportDir, "Logs"); Directory.CreateDirectory(directory);
		File.AppendAllText(Path.Combine(directory, "terrain.jsonl"), JsonSerializer.Serialize(new { tick = world.WorldTick, catchUp = world.IsLoadingGameSave, kind, payload }) + "\n");
	}
}
public sealed class TerrainProbeLoadScreen : BlankLoadScreen
{
	public override void StartGame(Arguments args)
	{
		if (!args.Contains("TerrainProbe.LoadSave")) { base.StartGame(args); return; }
		var path = args.GetValue("TerrainProbe.LoadSave", ""); var save = new GameSave(path);
		OpenRA.Widgets.Ui.ResetAll(); Game.Settings.Save();
		Game.CreateAndStartLocalServer(save.GlobalSettings.Map, [Order.FromTargetString("LoadGameSave", Path.GetFileName(path), true), Order.Command("state Ready")]);
	}
}
public sealed class TerrainRuntimeProbeInfo : TraitInfo
{ public override object Create(ActorInitializer init) => new TerrainRuntimeProbe(init.World); }
public sealed class TerrainRuntimeProbe : ITick, IWorldLoaded
{
	readonly World world; WorldRenderer? renderer; Actor? idle, walker, raised, tank; int raisedZ; bool complete; CPos lastTo; Actor? policyShooter, policyTarget;
	readonly CPos center = new(42, 6); readonly List<string> failures = [];
	public TerrainRuntimeProbe(World world) { this.world = world; }
	void IWorldLoaded.WorldLoaded(World world, WorldRenderer renderer)
	{ this.renderer = renderer; renderer.Viewport.Center(world.Map.CenterOfCell(center + new CVec(5, 5))); }
	void Check(string name, bool passed, string detail = "")
	{ ScenarioTrace.Write(world, passed ? "pass" : "fail", name + ":" + detail); if (!passed) failures.Add(name); }
	void Height(CPos cell, int height) { world.Map.Tiles[cell] = new TerrainTile(0, 0); world.Map.Height[cell] = (byte)height; }
	Actor Spawn(string type, CPos location)
	{ return world.CreateActor(true, type, new TypeDictionary { new OwnerInit(world.Players.First(p => p.InternalName == "GDI")), new LocationInit(location) }); }
	void LowerPatch(CPos origin)
	{
		// Four incident cells share a lowered corner; their remaining corners stay at level 4.
		var cells = new[] { origin, origin + new CVec(1, 0), origin + new CVec(1, 1), origin + new CVec(0, 1) };
		var tiles = new ushort[] { 50, 51, 48, 49 };
		for (var i = 0; i < cells.Length; i++) { world.Map.Tiles[cells[i]] = new TerrainTile(tiles[i], 0); world.Map.Height[cells[i]] = 3; }
	}
	bool Reachable(Mobile mobile, CPos start, CPos target)
	{
		var seen = new HashSet<CPos> { start }; var queue = new Queue<CPos>(); queue.Enqueue(start);
		while (queue.Count > 0)
		{
			var from = queue.Dequeue(); if (from == target) return true;
			foreach (var d in CVec.Directions)
			{
				var to = from + d; if (seen.Contains(to) || mobile.Locomotor.MovementCostToEnterCell(null, from, to, BlockedByActor.None, null) == short.MaxValue) continue;
				seen.Add(to); queue.Enqueue(to);
			}
		}
		return false;
	}
	void Gate(Actor actor, int x)
	{
		var mobile = actor.Trait<Mobile>(); var cells = world.Map.AllCells.Where(c => c.X == x).ToArray();
		var old = cells.ToDictionary(c => c, c => world.Map.Height[c]);
		foreach (var c in cells) Height(c, 7);
		var source = new CPos(x - 1, 10); var target = new CPos(x + 1, 10); var gate = new CPos(x, 10);
		foreach (var level in new[] { 7, 5, 7, 5 })
		{
			Height(gate, level);
			var path = mobile.PathFinder.FindPathToTargetCell(actor, [source], target, BlockedByActor.None);
			var expected = Reachable(mobile, source, target);
			Check($"hpf-gate-{x}-{level}", (path.Count > 0) == expected && expected == (level == 5));
			Check($"hpf-edges-{x}-{level}", path.Zip(path.Skip(1)).All(p => mobile.Locomotor.MovementCostToEnterCell(null, p.First, p.Second, BlockedByActor.None, null) != short.MaxValue));
		}
		foreach (var c in cells) Height(c, old[c]);
	}
	void Geometry(CPos origin)
	{
		var points = new Dictionary<(int, int), int>(); var offsets = new[] { (0, 0), (1, 0), (1, 1), (0, 1) }; var valid = true;
		for (var y = origin.Y - 3; y <= origin.Y + 3; y++) for (var x = origin.X - 3; x <= origin.X + 3; x++)
			{
				var cell = new CPos(x, y); var ramp = world.Map.Grid.Ramps[world.Map.Ramp[cell]];
				for (var i = 0; i < 4; i++)
				{
					var point = (x + offsets[i].Item1, y + offsets[i].Item2); var height = 724 * world.Map.Height[cell] + ramp.Corners[i].Z;
					if (points.TryGetValue(point, out var old) && old != height) valid = false; points[point] = height;
				}
			}
		Check("engine-surface-shared-corners", valid);
	}
	void Finish() { complete = true; ScenarioTrace.Write(world, "complete", JsonSerializer.Serialize(new { passed = failures.Count == 0, failures })); }
	void ITick.Tick(Actor self)
	{
		if (complete) return;
		var tick = world.WorldTick;
		var sightCell = center + new CVec(2, -2); var owner = world.Players.First(p => p.InternalName == "GDI");
		try
		{
			if (tick == 1)
			{
				idle = Spawn("e1", center); walker = Spawn("e1", center + new CVec(0, 3)); raised = Spawn("e1", center + new CVec(-3, 0)); Spawn("e1", center + new CVec(1, -1)); tank = Spawn("ttnk", center + new CVec(0, 6));
				var raisedMobile = raised.Trait<Mobile>(); raisedMobile.SetCenterPosition(raised, raised.CenterPosition + new WVec(0, 0, 2000)); raisedZ = raised.CenterPosition.Z;
				var dest = center + new CVec(1, 3); Height(dest, 6); Height(center + new CVec(0, 4), 5); Height(center + new CVec(1, 4), 5);
				var mobile = walker.Trait<Mobile>(); var path = mobile.PathFinder.FindPathToTargetCell(walker, [walker.Location], dest, BlockedByActor.None);
				Check("adjacent-detour", path.Count > 2, string.Join(";", path));
				Check("every-route-edge-valid", path.Zip(path.Skip(1)).All(p => mobile.Locomotor.MovementCostToEnterCell(walker, p.First, p.Second, BlockedByActor.None, null) != short.MaxValue));
				Height(center, 3); Height(raised.Location, 3); Height(tank.Location, 3);
				world.AddFrameEndTask(_ => LowerPatch(center + new CVec(8, 0)));
			}
			if (tick == 2) { Gate(walker!, 55); Gate(walker!, 59); }
			if (tick == 4)
			{
				Check("idle-grounded", world.Map.DistanceAboveTerrain(idle!.CenterPosition).Length == 0, idle.CenterPosition.ToString());
				Check("tank-grounded", world.Map.DistanceAboveTerrain(tank!.CenterPosition).Length == 0, tank.CenterPosition.ToString());
				Check("raised-preserved", raised!.CenterPosition.Z == raisedZ);


				var mobile = walker!.Trait<Mobile>(); var target = walker.Location + new CVec(8, 0);
				walker.QueueActivity(mobile.MoveTo(target, 0));
			}
			if (tick == 6)
			{
				lastTo = walker!.Trait<Mobile>().ToCell;
				// A route already exists; changing an entire column forces a detour.
				for (var y = walker!.Location.Y - 1; y <= walker.Location.Y + 1; y++) Height(new CPos(center.X + 4, y), 7);
			}
			if (tick > 6 && tick < 160 && walker != null)
			{
				var moving = walker.Trait<Mobile>();
				Check("moving-grounded", world.Map.DistanceAboveTerrain(walker.CenterPosition).Length == 0);
				if (moving.ToCell != lastTo)
					Check("live-edge-valid", moving.Locomotor.MovementCostToEnterCell(walker, moving.FromCell, moving.ToCell, BlockedByActor.None, null) != short.MaxValue, $"{moving.FromCell}->{moving.ToCell}");
				lastTo = moving.ToCell;
			}
			if (tick == 8) { Check("stationary-sight-before", owner.Shroud.IsVisible(sightCell.ToMPos(world.Map))); Height(sightCell, 7); }
			if (tick == 10) { Check("stationary-sight-closes", !owner.Shroud.IsVisible(sightCell.ToMPos(world.Map))); Height(sightCell, 4); }
			if (tick == 12) Check("stationary-sight-reopens", owner.Shroud.IsVisible(sightCell.ToMPos(world.Map)));
			if (tick == 172 && !world.IsLoadingGameSave)
				Sync.RunUnsynced(world, () => world.RequestGameSave("terrain-checkpoint.orasav", false));
			if (tick == 170)
			{
				Check("cached-route-detour-arrived", walker!.Location == center + new CVec(8, 3), walker.Location.ToString());
				world.AddFrameEndTask(_ => LowerPatch(center + new CVec(8, -2)));
			}
			if (tick == 180)
			{

#if RANGE_PROBE
				var source = center + new CVec(0, 12); var goal = source + new CVec(9, 0);
				// A reachable high strip is a valid firing location. Staying on low ground is not.
				for (var x = source.X + 4; x <= goal.X; x++)
					for (var y = source.Y - 1; y <= source.Y + 1; y++) Height(new CPos(x, y), x == source.X + 4 ? 5 : 6);
				policyShooter = Spawn("policyunit", source); policyTarget = Spawn("targetunit", goal);
				var range = policyShooter.TraitsImplementing<Armament>().First();
				Check("policy-rejects-low-origin", !range.IsInRange(policyShooter.CenterPosition, Target.FromActor(policyTarget)));
				Check("policy-accepts-prospective-origin", range.IsInRange(world.Map.CenterOfCell(source + new CVec(6, 0)), Target.FromActor(policyTarget)));
				policyShooter.Trait<AttackBase>().AttackTarget(Target.FromActor(policyTarget), AttackSource.Default, false, true, true);
#else
    Finish();
#endif
			}
#if RANGE_PROBE
			if (tick == 325) renderer?.Viewport.Center(world.Map.CenterOfCell(center + new CVec(5, 5)));
			if (tick == 330)
			{
				Check("policy-mover-reached-valid-origin", policyShooter!.CenterPosition.Z >= policyTarget!.CenterPosition.Z, policyShooter.CenterPosition.ToString());
				Check("policy-shot-from-valid-origin", policyTarget.Trait<Health>().HP < policyTarget.Trait<Health>().MaxHP);

			}

			if (tick == 340)
			{
				foreach (var cell in world.Map.AllCells) Height(cell, 4);
			}
			if (tick == 350) world.AddFrameEndTask(_ => LowerPatch(center + new CVec(8, 4)));
			if (tick == 415) renderer?.Viewport.Center(world.Map.CenterOfCell(center + new CVec(8, 4)));
			if (tick == 425) { Geometry(center + new CVec(8, 4)); Finish(); }
#endif
		}
		catch (Exception e) { complete = true; ScenarioTrace.Write(world, "exception", e.ToString()); throw; }
	}
}

#if RANGE_PROBE
public sealed class ProbeHeightPolicyInfo : TraitInfo, IArmamentRangePolicyInfo
{ public override object Create(ActorInitializer init) => new ProbeHeightPolicy(); }
public sealed class ProbeHeightPolicy : IArmamentRangePolicy
{
	public int RangeRevision => 0;
	public WDist GetMaximumRange(Armament armament, WDist nominalRange) => nominalRange;
	public bool IsInRange(Armament armament, WPos origin, in Target target, WDist min, WDist max) =>
	 origin.Z >= target.CenterPosition.Z && target.IsInRange(origin, max) && !target.IsInRange(origin, min);
}
#endif
