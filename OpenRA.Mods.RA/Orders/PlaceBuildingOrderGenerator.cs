#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Orders
{
	public class PlaceBuildingOrderGenerator : IOrderGenerator
	{
		readonly Actor Producer;
		readonly string Building;
		readonly IEnumerable<Renderable> Preview;
		readonly BuildingInfo BuildingInfo;
		Sprite buildOk, buildBlocked;

		public PlaceBuildingOrderGenerator(Actor producer, string name)
		{
			Producer = producer;
			Building = name;
			BuildingInfo = Rules.Info[Building].Traits.Get<BuildingInfo>();

			Preview = Rules.Info[Building].Traits.Get<RenderBuildingInfo>()
								.RenderPreview(Rules.Info[Building], producer.Owner);

			buildOk = SequenceProvider.GetSequence("overlay", "build-valid").GetSprite(0);
			buildBlocked = SequenceProvider.GetSequence("overlay", "build-invalid").GetSprite(0);
		}

		public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
				world.CancelInputMode();

			var ret = InnerOrder( world, xy, mi ).ToList();
			if (ret.Count > 0)
				world.CancelInputMode();

			return ret;
		}

		IEnumerable<Order> InnerOrder(World world, int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				var topLeft = xy - FootprintUtils.AdjustForBuildingSize( BuildingInfo );
				if (!world.CanPlaceBuilding( Building, BuildingInfo, topLeft, null)
					|| !BuildingInfo.IsCloseEnoughToBase(world, Producer.Owner, Building, topLeft))
				{
					var eva = world.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
					Sound.Play(eva.BuildingCannotPlaceAudio);
					yield break;
				}

				var isLineBuild = Rules.Info[ Building ].Traits.Contains<LineBuildInfo>();
				yield return new Order(isLineBuild ? "LineBuild" : "PlaceBuilding",
					Producer.Owner.PlayerActor, false) { TargetLocation = topLeft, TargetString = Building };
			}
		}

		public void Tick( World world ) {}
		public void RenderAfterWorld( WorldRenderer wr, World world ) {}
		public void RenderBeforeWorld( WorldRenderer wr, World world )
		{
			var position = Game.viewport.ViewToWorld(Viewport.LastMousePos).ToInt2();
			var topLeft = position - FootprintUtils.AdjustForBuildingSize( BuildingInfo );

			var actorInfo = Rules.Info[Building];
			foreach (var dec in actorInfo.Traits.WithInterface<IPlaceBuildingDecoration>())
				dec.Render(wr, world, actorInfo, Traits.Util.CenterOfCell(position));	/* hack hack */

			var cells = new Dictionary<int2, bool>();
			// Linebuild for walls.
			// Assumes a 1x1 footprint; weird things will happen for other footprints
			if (Rules.Info[Building].Traits.Contains<LineBuildInfo>())
			{
				foreach( var t in BuildingUtils.GetLineBuildCells( world, topLeft, Building, BuildingInfo ) )
					cells.Add( t, BuildingInfo.IsCloseEnoughToBase( world, world.LocalPlayer, Building, t ) );
			}
			else
			{
				foreach (var r in Preview)
					r.Sprite.DrawAt(Game.CellSize*topLeft + r.Pos,
									wr.GetPaletteIndex(r.Palette),
									r.Scale*r.Sprite.size);

				var res = world.WorldActor.Trait<ResourceLayer>();
				var isCloseEnough = BuildingInfo.IsCloseEnoughToBase(world, world.LocalPlayer, Building, topLeft);
				foreach (var t in FootprintUtils.Tiles(Building, BuildingInfo, topLeft))
					cells.Add( t, isCloseEnough && world.IsCellBuildable(t, BuildingInfo) && res.GetResource(t) == null );
			}

			foreach( var c in cells )
				( c.Value ? buildOk : buildBlocked ).DrawAt( wr, Game.CellSize * c.Key, "terrain" );
		}

		public string GetCursor(World world, int2 xy, MouseInput mi) { return "default"; }
	}
}
