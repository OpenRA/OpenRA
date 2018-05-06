#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class BridgeInfo : ITraitInfo, IRulesetLoaded, Requires<HealthInfo>, Requires<BuildingInfo>
	{
		public readonly bool Long = false;

		[Desc("Delay (in ticks) between repairing adjacent spans in long bridges")]
		public readonly int RepairPropagationDelay = 20;

		public readonly ushort Template = 0;
		public readonly ushort DamagedTemplate = 0;
		public readonly ushort DestroyedTemplate = 0;

		// For long bridges
		public readonly ushort DestroyedPlusNorthTemplate = 0;
		public readonly ushort DestroyedPlusSouthTemplate = 0;
		public readonly ushort DestroyedPlusBothTemplate = 0;

		public readonly string[] ShorePieces = { "br1", "br2" };
		public readonly int[] NorthOffset = null;
		public readonly int[] SouthOffset = null;

		[Desc("The name of the weapon to use when demolishing the bridge")]
		[WeaponReference] public readonly string DemolishWeapon = "Demolish";

		public WeaponInfo DemolishWeaponInfo { get; private set; }

		[Desc("Types of damage that this bridge causes to units over/in path of it while being destroyed/repaired. Leave empty for no damage types.")]
		public readonly HashSet<string> DamageTypes = new HashSet<string>();

		public object Create(ActorInitializer init) { return new Bridge(init.Self, this); }

		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (string.IsNullOrEmpty(DemolishWeapon))
				throw new YamlException("A value for DemolishWeapon of a Bridge trait is missing.");

			WeaponInfo weapon;
			var weaponToLower = DemolishWeapon.ToLowerInvariant();
			if (!rules.Weapons.TryGetValue(weaponToLower, out weapon))
				throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(weaponToLower));

			DemolishWeaponInfo = weapon;
		}

		public IEnumerable<Pair<ushort, int>> Templates
		{
			get
			{
				if (Template != 0)
					yield return Pair.New(Template, 100);

				if (DamagedTemplate != 0)
					yield return Pair.New(DamagedTemplate, 49);

				if (DestroyedTemplate != 0)
					yield return Pair.New(DestroyedTemplate, 0);

				if (DestroyedPlusNorthTemplate != 0)
					yield return Pair.New(DestroyedPlusNorthTemplate, 0);

				if (DestroyedPlusSouthTemplate != 0)
					yield return Pair.New(DestroyedPlusSouthTemplate, 0);

				if (DestroyedPlusBothTemplate != 0)
					yield return Pair.New(DestroyedPlusBothTemplate, 0);
			}
		}
	}

	class Bridge : IRender, INotifyDamageStateChanged
	{
		readonly BuildingInfo buildingInfo;
		readonly Bridge[] neighbours = new Bridge[2];
		readonly LegacyBridgeHut[] huts = new LegacyBridgeHut[2]; // Huts before this / first & after this / last
		readonly Health health;
		readonly Actor self;
		readonly BridgeInfo info;
		readonly string type;

		readonly Lazy<bool> isDangling;
		ushort template;
		Dictionary<CPos, byte> footprint;

		public LegacyBridgeHut Hut { get; private set; }
		public bool IsDangling { get { return isDangling.Value; } }

		public Bridge(Actor self, BridgeInfo info)
		{
			this.self = self;
			health = self.Trait<Health>();
			health.RemoveOnDeath = false;
			this.info = info;
			type = self.Info.Name;
			isDangling = new Lazy<bool>(() => huts[0] == huts[1] && (neighbours[0] == null || neighbours[1] == null));
			buildingInfo = self.Info.TraitInfo<BuildingInfo>();
		}

		public Bridge Neighbour(int direction) { return neighbours[direction]; }
		public IEnumerable<Bridge> Enumerate(int direction, bool includeSelf = false)
		{
			for (var b = includeSelf ? this : neighbours[direction]; b != null; b = b.neighbours[direction])
				yield return b;
		}

		public void Do(Action<Bridge, int> action)
		{
			action(this, -1);
			for (var d = 0; d <= 1; d++)
				if (neighbours[d] != null)
					action(neighbours[d], d);
		}

		public void Create(ushort template, Dictionary<CPos, byte> footprint)
		{
			this.template = template;
			this.footprint = footprint;

			// Set the initial custom terrain types
			foreach (var c in footprint.Keys)
				self.World.Map.CustomTerrain[c] = GetTerrainType(c);
		}

		byte GetTerrainType(CPos cell)
		{
			var dx = cell - self.Location;
			var tileSet = self.World.Map.Rules.TileSet;
			var index = dx.X + tileSet.Templates[template].Size.X * dx.Y;
			return tileSet.GetTerrainIndex(new TerrainTile(template, (byte)index));
		}

		public void LinkNeighbouringBridges(World world, LegacyBridgeLayer bridges)
		{
			for (var d = 0; d <= 1; d++)
			{
				if (neighbours[d] != null)
					continue; // Already linked by reverse lookup

				var offset = d == 0 ? info.NorthOffset : info.SouthOffset;
				if (offset == null)
					continue; // End piece type

				neighbours[d] = GetNeighbor(offset, bridges);
				if (neighbours[d] != null)
					neighbours[d].neighbours[1 - d] = this; // Save reverse lookup
			}
		}

		internal void AddHut(LegacyBridgeHut hut)
		{
			// TODO: This method is incomprehensible and fragile, and should be rewritten.
			if (huts[0] == huts[1])
				huts[1] = hut;
			if (Hut == null)
			{
				Hut = hut; // Assume only one until called again
				if (huts[0] == null)
					huts[0] = hut; // Set only first time
				for (var d = 0; d <= 1; d++)
					for (var b = neighbours[d]; b != null; b = b.Hut == null ? b.neighbours[d] : null)
						b.huts[d] = hut;
			}
			else
				Hut = null;
		}

		public LegacyBridgeHut GetHut(int index) { return huts[index]; }
		public Bridge GetNeighbor(int[] offset, LegacyBridgeLayer bridges)
		{
			if (offset == null)
				return null;

			return bridges.GetBridge(self.Location + new CVec(offset[0], offset[1]));
		}

		IRenderable[] TemplateRenderables(WorldRenderer wr, PaletteReference palette, ushort template)
		{
			var offset = buildingInfo.CenterOffset(self.World).Y + 1024;

			return footprint.Select(c => (IRenderable)(new SpriteRenderable(
				wr.Theater.TileSprite(new TerrainTile(template, c.Value)),
				wr.World.Map.CenterOfCell(c.Key), WVec.Zero, -offset, palette, 1f, true))).ToArray();
		}

		bool initialized;
		Dictionary<ushort, IRenderable[]> renderables;
		public IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			if (!initialized)
			{
				var palette = wr.Palette(TileSet.TerrainPaletteInternalName);
				renderables = new Dictionary<ushort, IRenderable[]>();
				foreach (var t in info.Templates)
					renderables.Add(t.First, TemplateRenderables(wr, palette, t.First));

				initialized = true;
			}

			return renderables[template];
		}

		public IEnumerable<Rectangle> ScreenBounds(Actor self, WorldRenderer wr)
		{
			foreach (var kv in footprint)
			{
				var xy = wr.ScreenPxPosition(wr.World.Map.CenterOfCell(kv.Key));
				var size = wr.Theater.TileSprite(new TerrainTile(template, kv.Value)).Bounds.Size;

				// Add an extra pixel padding to avoid issues with odd-sized sprites
				var halfWidth = size.Width / 2 + 1;
				var halfHeight = size.Height / 2 + 1;

				yield return Rectangle.FromLTRB(
					xy.X - halfWidth,
					xy.Y - halfHeight,
					xy.X + halfWidth,
					xy.Y + halfHeight);
			}
		}

		void KillUnitsOnBridge()
		{
			foreach (var c in footprint.Keys)
				foreach (var a in self.World.ActorMap.GetActorsAt(c))
					if (a.Info.HasTraitInfo<IPositionableInfo>() && !a.Trait<IPositionable>().CanExistInCell(c))
						a.Kill(self, info.DamageTypes);
		}

		bool NeighbourIsDeadShore(Bridge neighbour)
		{
			return neighbour != null && info.ShorePieces.Contains(neighbour.type) && neighbour.health.IsDead;
		}

		bool LongBridgeSegmentIsDead()
		{
			// The long bridge artwork requires a hack to display correctly
			// if the adjacent shore piece is dead
			if (!info.Long)
				return health.IsDead;

			if (NeighbourIsDeadShore(neighbours[0]) || NeighbourIsDeadShore(neighbours[1]))
				return true;

			return health.IsDead;
		}

		ushort ChooseTemplate()
		{
			if (info.Long && LongBridgeSegmentIsDead())
			{
				// Long bridges have custom art for multiple segments being destroyed
				var previousIsDead = neighbours[0] != null && neighbours[0].LongBridgeSegmentIsDead();
				var nextIsDead = neighbours[1] != null && neighbours[1].LongBridgeSegmentIsDead();
				if (previousIsDead && nextIsDead)
					return info.DestroyedPlusBothTemplate;
				if (previousIsDead)
					return info.DestroyedPlusNorthTemplate;
				if (nextIsDead)
					return info.DestroyedPlusSouthTemplate;

				return info.DestroyedTemplate;
			}

			var ds = health.DamageState;
			return (ds == DamageState.Dead && info.DestroyedTemplate > 0) ? info.DestroyedTemplate :
				(ds >= DamageState.Heavy && info.DamagedTemplate > 0) ? info.DamagedTemplate : info.Template;
		}

		bool killedUnits = false;
		void UpdateState()
		{
			var oldTemplate = template;

			template = ChooseTemplate();
			if (template == oldTemplate)
				return;

			// Update map
			foreach (var c in footprint.Keys)
				self.World.Map.CustomTerrain[c] = GetTerrainType(c);

			// If this bridge repair operation connects two pathfinding domains,
			// update the domain index.
			var domainIndex = self.World.WorldActor.TraitOrDefault<DomainIndex>();
			if (domainIndex != null)
				domainIndex.UpdateCells(self.World, footprint.Keys);

			if (LongBridgeSegmentIsDead() && !killedUnits)
			{
				killedUnits = true;
				KillUnitsOnBridge();
			}
		}

		public void Repair(Actor repairer, int direction, Action onComplete)
		{
			// Repair self
			var initialDamage = health.DamageState;
			self.World.AddFrameEndTask(w =>
			{
				if (health.IsDead)
				{
					health.Resurrect(self, repairer);
					killedUnits = false;
					KillUnitsOnBridge();
				}
				else
					health.InflictDamage(self, repairer, new Damage(-health.MaxHP), true);
				if (direction < 0 ? neighbours[0] == null && neighbours[1] == null : Hut != null || neighbours[direction] == null)
					onComplete(); // Done if single or reached other hut
			});

			// Repair adjacent spans onto next hut or end
			if (direction >= 0 && Hut == null && neighbours[direction] != null)
			{
				var delay = initialDamage == DamageState.Undamaged || NeighbourIsDeadShore(neighbours[direction]) ?
					0 : info.RepairPropagationDelay;

				self.World.AddFrameEndTask(w => w.Add(new DelayedAction(delay, () =>
					neighbours[direction].Repair(repairer, direction, onComplete))));
			}
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			Do((b, d) => b.UpdateState());

			// Need to update the neighbours neighbour to correctly
			// display the broken shore hack
			if (info.ShorePieces.Contains(type))
				for (var d = 0; d <= 1; d++)
					if (neighbours[d] != null && neighbours[d].neighbours[d] != null)
						neighbours[d].neighbours[d].UpdateState();
		}

		void AggregateDamageState(Bridge b, int d, ref DamageState damage)
		{
			if (b.health.DamageState > damage)
				damage = b.health.DamageState;
			if (b.Hut == null && d >= 0 && b.neighbours[d] != null)
				AggregateDamageState(b.neighbours[d], d, ref damage);
		}

		// Find the worst span damage before other hut
		public DamageState AggregateDamageState()
		{
			var damage = health.DamageState;
			Do((b, d) => AggregateDamageState(b, d, ref damage));
			return damage;
		}

		public void Demolish(Actor saboteur, int direction)
		{
			var initialDamage = health.DamageState;
			self.World.AddFrameEndTask(w =>
			{
				// Use .FromPos since this actor is killed. Cannot use Target.FromActor
				info.DemolishWeaponInfo.Impact(Target.FromPos(self.CenterPosition), saboteur, Enumerable.Empty<int>());

				self.Kill(saboteur);
			});

			// Destroy adjacent spans between (including) huts
			if (direction >= 0 && Hut == null && neighbours[direction] != null)
			{
				var delay = initialDamage == DamageState.Dead || NeighbourIsDeadShore(neighbours[direction]) ?
					0 : info.RepairPropagationDelay;

				self.World.AddFrameEndTask(w => w.Add(new DelayedAction(delay, () =>
					neighbours[direction].Demolish(saboteur, direction))));
			}
		}
	}
}
