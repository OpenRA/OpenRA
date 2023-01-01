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

		public override IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, string image, int facings, PaletteReference p)
		{
			if (!EnabledByDefault)
				yield break;

			var adjacent = 0;
			var locationInit = init.GetOrDefault<LocationInit>();
			var neighbourInit = init.GetOrDefault<RuntimeNeighbourInit>();

			if (locationInit != null && neighbourInit != null)
			{
				var location = locationInit.Value;
				foreach (var kv in neighbourInit.Value)
				{
					var haveNeighbour = false;
					foreach (var n in kv.Value)
					{
						var rb = init.World.Map.Rules.Actors[n].TraitInfos<IWallConnectorInfo>().FirstEnabledTraitOrDefault();
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

			var anim = new Animation(init.World, image);
			anim.PlayFetchIndex(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), Sequence), () => adjacent);

			yield return new SpriteActorPreview(anim, () => WVec.Zero, () => 0, p);
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
			: base(init, info)
		{
			wallInfo = info;
		}

		protected override void DamageStateChanged(Actor self)
		{
			if (IsTraitDisabled)
				return;

			DefaultAnimation.PlayFetchIndex(NormalizeSequence(self, Info.Sequence), () => adjacent);
		}

		void ITick.Tick(Actor self)
		{
			if (!dirty)
				return;

			// Update connection to neighbours
			var adjacentActors = CVec.Directions.SelectMany(dir =>
				self.World.ActorMap.GetActorsAt(self.Location + dir));

			adjacent = 0;
			foreach (var a in adjacentActors)
			{
				var wc = a.TraitsImplementing<IWallConnector>().FirstEnabledTraitOrDefault();
				if (wc == null || !wc.AdjacentWallCanConnect(a, self.Location, wallInfo.Type, out var facing))
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

		protected override void TraitEnabled(Actor self)
		{
			base.TraitEnabled(self);
			dirty = true;

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

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			UpdateNeighbours(self);
		}
	}

	public class RuntimeNeighbourInit : ValueActorInit<Dictionary<CPos, string[]>>, ISuppressInitExport, ISingleInstanceInit
	{
		public RuntimeNeighbourInit(Dictionary<CPos, string[]> value)
			: base(value) { }
	}
}
