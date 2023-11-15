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
using System.IO;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class BridgeInfo : TraitInfo, IRulesetLoaded, Requires<HealthInfo>, Requires<BuildingInfo>
	{
		public readonly bool Long = false;

		public readonly ushort Template = 0;
		public readonly ushort DamagedTemplate = 0;
		public readonly ushort DestroyedTemplate = 0;

		// For long bridges
		public readonly ushort DestroyedPlusNorthTemplate = 0;
		public readonly ushort DestroyedPlusSouthTemplate = 0;
		public readonly ushort DestroyedPlusBothTemplate = 0;

		public readonly string[] ShorePieces = { "br1", "br2" };

		[WeaponReference]
		[Desc("The name of the weapon to use when demolishing the bridge")]
		public readonly string DemolishWeapon = "Demolish";

		public WeaponInfo DemolishWeaponInfo { get; private set; }

		[Desc("Types of damage that this bridge causes to units over/in path of it while being destroyed/repaired. Leave empty for no damage types.")]
		public readonly BitSet<DamageType> DamageTypes = default;

		public override object Create(ActorInitializer init) { return new Bridge(init.Self, this); }

		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (!rules.Actors[SystemActors.World].HasTraitInfo<ITiledTerrainRendererInfo>())
				throw new YamlException("Bridge requires a tile-based terrain renderer.");

			if (string.IsNullOrEmpty(DemolishWeapon))
				throw new YamlException("A value for DemolishWeapon of a Bridge trait is missing.");

			var weaponToLower = DemolishWeapon.ToLowerInvariant();
			if (!rules.Weapons.TryGetValue(weaponToLower, out var weapon))
				throw new YamlException($"Weapons Ruleset does not contain an entry '{weaponToLower}'");

			DemolishWeaponInfo = weapon;
		}

		public IEnumerable<(ushort Template, int Health)> Templates
		{
			get
			{
				if (Template != 0)
					yield return (Template, 100);

				if (DamagedTemplate != 0)
					yield return (DamagedTemplate, 49);

				if (DestroyedTemplate != 0)
					yield return (DestroyedTemplate, 0);

				if (DestroyedPlusNorthTemplate != 0)
					yield return (DestroyedPlusNorthTemplate, 0);

				if (DestroyedPlusSouthTemplate != 0)
					yield return (DestroyedPlusSouthTemplate, 0);

				if (DestroyedPlusBothTemplate != 0)
					yield return (DestroyedPlusBothTemplate, 0);
			}
		}
	}

	public class Bridge : IRender, INotifyDamageStateChanged, IRadarSignature, IBridgeSegment
	{
		readonly BuildingInfo buildingInfo;
		readonly ITiledTerrainRenderer terrainRenderer;
		readonly ITemplatedTerrainInfo terrainInfo;
		readonly Health health;
		readonly Actor self;
		readonly BridgeInfo info;
		readonly string type;
		readonly BridgeLayer bridgeLayer;

		ushort template;
		Dictionary<CPos, byte> footprint;
		(CPos Cell, Color Color)[] radarSignature;

		Bridge[] Neighbours { get; set; } = Array.Empty<Bridge>();

		public Bridge(Actor self, BridgeInfo info)
		{
			this.self = self;
			health = self.Trait<Health>();
			health.RemoveOnDeath = false;
			this.info = info;
			type = self.Info.Name;
			buildingInfo = self.Info.TraitInfo<BuildingInfo>();
			bridgeLayer = self.World.WorldActor.Trait<BridgeLayer>();

			terrainRenderer = self.World.WorldActor.Trait<ITiledTerrainRenderer>();
			terrainInfo = self.World.Map.Rules.TerrainInfo as ITemplatedTerrainInfo;
			if (terrainInfo == null)
				throw new InvalidDataException("Bridge requires a template-based tileset.");
		}

		public void Create(ushort template, Dictionary<CPos, byte> footprint)
		{
			this.template = template;
			this.footprint = footprint;

			bridgeLayer.Add(self);
			radarSignature = new (CPos Cell, Color Color)[footprint.Keys.Count];

			// Set the initial state.
			var i = 0;
			foreach (var c in footprint.Keys)
			{
				var tileInfo = GetTerrainInfo(c);
				self.World.Map.CustomTerrain[c] = tileInfo.TerrainType;
				radarSignature[i++] = (c, tileInfo.GetColor(self.World.LocalRandom));
			}
		}

		TerrainTileInfo GetTerrainInfo(CPos cell)
		{
			var dx = cell - self.Location;
			var index = dx.X + terrainInfo.Templates[template].Size.X * dx.Y;
			return terrainInfo.GetTerrainInfo(new TerrainTile(template, (byte)index));
		}

		IRenderable[] TemplateRenderables(WorldRenderer wr, PaletteReference palette, ushort template)
		{
			var offset = buildingInfo.CenterOffset(self.World).Y + 1024;

			return footprint.Select(c => (IRenderable)new SpriteRenderable(
				terrainRenderer.TileSprite(new TerrainTile(template, c.Value)),
				wr.World.Map.CenterOfCell(c.Key), WVec.Zero, -offset, palette, 1f, 1f,
				float3.Ones, TintModifiers.None, true)).ToArray();
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
					renderables.Add(t.Template, TemplateRenderables(wr, palette, t.Template));

				initialized = true;
			}

			return renderables[template];
		}

		public IEnumerable<Rectangle> ScreenBounds(Actor self, WorldRenderer wr)
		{
			foreach (var kv in footprint)
			{
				var xy = wr.ScreenPxPosition(wr.World.Map.CenterOfCell(kv.Key));
				var size = terrainRenderer.TileSprite(new TerrainTile(template, kv.Value)).Bounds.Size;

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

		bool LongBridgeSegmentIsDead()
		{
			// The long bridge artwork requires a hack to display correctly
			// if the adjacent shore piece is dead.
			return health.IsDead || (info.Long && Neighbours.Any(b => b.health.IsDead && info.ShorePieces.Contains(b.type)));
		}

		ushort ChooseTemplate()
		{
			if (info.Long && LongBridgeSegmentIsDead())
			{
				// Long bridges have custom art for multiple segments being destroyed.
				var neighbour1Dead = Neighbours.Length > 0 && Neighbours[0].LongBridgeSegmentIsDead();
				var neighbour2Dead = Neighbours.Length > 1 && Neighbours[1].LongBridgeSegmentIsDead();
				if (neighbour1Dead && neighbour2Dead)
					return info.DestroyedPlusBothTemplate;

				if (neighbour1Dead || neighbour2Dead)
				{
					var deadNeighbourPos = (neighbour1Dead ? Neighbours[0] : Neighbours[1]).self.Location;
					if (deadNeighbourPos.Y == self.Location.Y)
					{
						// Support horizontal bridges.
						if (deadNeighbourPos.X < self.Location.X)
							return info.DestroyedPlusNorthTemplate;
					}
					else if (deadNeighbourPos.Y < self.Location.Y)
						return info.DestroyedPlusNorthTemplate;

					return info.DestroyedPlusSouthTemplate;
				}

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

			// Update map.
			var i = 0;
			foreach (var c in footprint.Keys)
			{
				var tileInfo = GetTerrainInfo(c);
				self.World.Map.CustomTerrain[c] = tileInfo.TerrainType;
				radarSignature[i++] = (c, tileInfo.GetColor(self.World.LocalRandom));
			}

			if (!killedUnits && LongBridgeSegmentIsDead())
			{
				killedUnits = true;
				KillUnitsOnBridge();
			}
		}

		void IBridgeSegment.Repair(Actor repairer)
		{
			if (health.IsDead)
			{
				health.Resurrect(self, repairer);
				killedUnits = false;
				KillUnitsOnBridge();
			}
			else
				health.InflictDamage(self, repairer, new Damage(-health.MaxHP), true);
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			UpdateState();

			foreach (var neighbour in Neighbours)
			{
				neighbour.UpdateState();

				// Need to update the neighbours neighbour to correctly
				// display the broken shore hack.
				if (neighbour.info.ShorePieces.Contains(type))
					foreach (var n in neighbour.Neighbours)
						if (n != this)
							n.UpdateState();
			}
		}

		void IBridgeSegment.Demolish(Actor saboteur, BitSet<DamageType> damageTypes)
		{
			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead)
					return;

				// Use .FromPos since this actor is dead. Cannot use Target.FromActor.
				info.DemolishWeaponInfo.Impact(Target.FromPos(self.CenterPosition), saboteur);

				self.Kill(saboteur, damageTypes);
			});
		}

		void IRadarSignature.PopulateRadarSignatureCells(Actor self, List<(CPos Cell, Color Color)> destinationBuffer)
		{
			destinationBuffer.AddRange(radarSignature);
		}

		string IBridgeSegment.Type => "GroundLevelBridge";
		DamageState IBridgeSegment.DamageState => self.GetDamageState();
		bool IBridgeSegment.Valid => self.IsInWorld;
		IEnumerable<CPos> IBridgeSegment.Footprint => buildingInfo.PathableTiles(self.Location);
		CPos IBridgeSegment.Location => self.Location;

		void IBridgeSegment.SetNeighbours(IEnumerable<IBridgeSegment> neighbours)
		{
			var n = new Stack<Bridge>();
			foreach (var neighbour in neighbours)
				if (neighbour is Bridge bridge)
					n.Push(bridge);

			Neighbours = n.ToArray();
		}
	}
}
