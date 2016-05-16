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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[RequireExplicitImplementation]
	interface IWallConnectorInfo : ITraitInfoInterface
	{
		string GetWallConnectionType();
	}

	[Desc("Render trait for actors that change sprites if neighbors with the same trait are present.")]
	class WithWallSpriteBodyInfo : WithSpriteBodyInfo, IWallConnectorInfo, Requires<BuildingInfo>
	{
		public readonly string Type = "wall";

		public override object Create(ActorInitializer init) { return new WithWallSpriteBody(init, this); }

		public override IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			var adjacent = 0;

			if (init.Contains<RuntimeNeighbourInit>())
			{
				var location = CPos.Zero;
				if (init.Contains<LocationInit>())
					location = init.Get<LocationInit, CPos>();

				var neighbours = init.Get<RuntimeNeighbourInit, Dictionary<CPos, string[]>>();
				foreach (var kv in neighbours)
				{
					var haveNeighbour = false;
					foreach (var n in kv.Value)
					{
						var rb = init.World.Map.Rules.Actors[n].TraitInfos<IWallConnectorInfo>().FirstOrDefault(Exts.IsTraitEnabled);
						if (rb != null && rb.GetWallConnectionType() == Type)
						{
							haveNeighbour = true;
							break;
						}
					}

					if (!haveNeighbour)
						continue;

					if (kv.Key == location + new CVec(0, -1))
						adjacent |= 1;
					else if (kv.Key == location + new CVec(+1, 0))
						adjacent |= 2;
					else if (kv.Key == location + new CVec(0, +1))
						adjacent |= 4;
					else if (kv.Key == location + new CVec(-1, 0))
						adjacent |= 8;
				}
			}

			var anim = new Animation(init.World, image, () => 0);
			anim.PlayFetchIndex(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), Sequence), () => adjacent);

			yield return new SpriteActorPreview(anim, () => WVec.Zero, () => 0, p, rs.Scale);
		}

		string IWallConnectorInfo.GetWallConnectionType()
		{
			return Type;
		}
	}

	class WithWallSpriteBody : WithSpriteBody, INotifyRemovedFromWorld, IWallConnector, ITick
	{
		readonly WithWallSpriteBodyInfo wallInfo;
		int adjacent = 0;
		bool dirty = true;

		bool IWallConnector.AdjacentWallCanConnect(Actor self, CPos wallLocation, string wallType, out CVec facing)
		{
			facing = wallLocation - self.Location;
			return wallInfo.Type == wallType && Math.Abs(facing.X) + Math.Abs(facing.Y) == 1;
		}

		void IWallConnector.SetDirty() { dirty = true; }

		public WithWallSpriteBody(ActorInitializer init, WithWallSpriteBodyInfo info)
			: base(init, info, () => 0)
		{
			wallInfo = info;
		}

		public override void DamageStateChanged(Actor self, AttackInfo e)
		{
			DefaultAnimation.PlayFetchIndex(NormalizeSequence(self, Info.Sequence), () => adjacent);
		}

		public void Tick(Actor self)
		{
			if (!dirty)
				return;

			// Update connection to neighbours
			var adjacentActors = CVec.Directions.SelectMany(dir =>
				self.World.ActorMap.GetActorsAt(self.Location + dir));

			adjacent = 0;
			foreach (var a in adjacentActors)
			{
				CVec facing;
				var wc = a.TraitsImplementing<IWallConnector>().FirstOrDefault(Exts.IsTraitEnabled);
				if (wc == null || !wc.AdjacentWallCanConnect(a, self.Location, wallInfo.Type, out facing))
					continue;

				if (facing.Y > 0)
					adjacent |= 1;
				else if (facing.X < 0)
					adjacent |= 2;
				else if (facing.Y < 0)
					adjacent |= 4;
				else if (facing.X > 0)
					adjacent |= 8;
			}

			dirty = false;
		}

		public override void BuildingComplete(Actor self)
		{
			DefaultAnimation.PlayFetchIndex(NormalizeSequence(self, Info.Sequence), () => adjacent);
			UpdateNeighbours(self);

			// Set the initial animation frame before the render tick (for frozen actor previews)
			self.World.AddFrameEndTask(_ => DefaultAnimation.Tick());
		}

		static void UpdateNeighbours(Actor self)
		{
			var adjacentActorTraits = CVec.Directions.SelectMany(dir =>
					self.World.ActorMap.GetActorsAt(self.Location + dir))
				.SelectMany(a => a.TraitsImplementing<IWallConnector>());

			foreach (var aat in adjacentActorTraits)
				aat.SetDirty();
		}

		public void RemovedFromWorld(Actor self)
		{
			UpdateNeighbours(self);
		}
	}

	public class RuntimeNeighbourInit : IActorInit<Dictionary<CPos, string[]>>, ISuppressInitExport
	{
		[FieldFromYamlKey] readonly Dictionary<CPos, string[]> value = null;
		public RuntimeNeighbourInit() { }
		public RuntimeNeighbourInit(Dictionary<CPos, string[]> init) { value = init; }
		public Dictionary<CPos, string[]> Value(World world) { return value; }
	}
}
