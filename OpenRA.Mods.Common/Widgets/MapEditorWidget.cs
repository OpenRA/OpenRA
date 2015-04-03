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
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class MapEditorWidget : Widget
	{
		public readonly string TooltipContainer;
		public readonly string TooltipTemplate;
		readonly Lazy<TooltipContainerWidget> tooltipContainer;

		public ushort? SelectedTileId;
		public ResourceTypeInfo SelectedResourceTypeInfo;
		public string SelectedActor;
		public int ActorFacing;
		public string SelectedOwner;

		public enum DragType { None, Tiles, Layers, Actors }
		public DragType CurrentPreview = DragType.None;

		public CPos MouseCursorCell;
		IEnumerable<Actor> underCursor;

		readonly World world;
		readonly WorldRenderer worldRenderer;
		readonly Ruleset rules;

		readonly ResourceLayer resourceLayer;

		[ObjectCreator.UseCtor]
		public MapEditorWidget(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;

			rules = world.Map.Rules;

			tooltipContainer = Exts.Lazy(() => Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));

			resourceLayer = world.WorldActor.Trait<ResourceLayer>();
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			UpdatePosition(mi.Location);

			if (mi.Event == MouseInputEvent.Up)
				return false;

			if (mi.Event == MouseInputEvent.Scroll)
			{
				if (CurrentPreview == DragType.Actors)
					ActorFacing += mi.ScrollDelta;
				else
				{
					if (mi.ScrollDelta < 0)
						worldRenderer.Viewport.Zoom = 0.5f;
					if (mi.ScrollDelta > 0)
						worldRenderer.Viewport.Zoom = 1f;
				}
			}

			underCursor = world.ScreenMap.ActorsAt(mi);
			if (underCursor.Any())
			{
				if (TooltipContainer != null && CurrentPreview == DragType.None)
				{
					var actor = world.Map.Actors.Value.Values.FirstOrDefault(a => a.InitDict.Get<LocationInit>().Value(world) == underCursor.First().Location);
					Func<string> getText = () =>
					{
						if (actor == null || !world.Map.Actors.Value.Values.Contains(actor))
							return "";

						return world.Map.Actors.Value.FirstOrDefault(k => k.Value == actor).Key;
					};
					tooltipContainer.Value.SetTooltip(TooltipTemplate, new WidgetArgs() { { "getText", getText } });
				}
			}
			else if (world.Map.Contains(MouseCursorCell) && resourceLayer.GetResourceDensity(MouseCursorCell) > 0 && mi.Button == MouseButton.None)
			{
				if (TooltipContainer != null && CurrentPreview == DragType.None)
				{
					var resourcePatchValue = resourceLayer.GetValueFromPatchAround(MouseCursorCell); // TODO: calculation is wrong for 1x1 patches
					Func<string> getText = () => { return "${0}".F(resourcePatchValue); };
					tooltipContainer.Value.SetTooltip(TooltipTemplate, new WidgetArgs() { { "getText", getText } });
				}
			}
			else if (TooltipContainer != null)
				tooltipContainer.Value.RemoveTooltip();

			if (mi.Button == MouseButton.Right)
			{
				if (TooltipContainer != null)
					tooltipContainer.Value.RemoveTooltip();

				if (CurrentPreview == DragType.None)
				{
					if (underCursor.Any())
					{
						var key = world.Map.Actors.Value.FirstOrDefault(a =>
						a.Value.InitDict.Get<LocationInit>().Value(world) == underCursor.First().Location);
						if (key.Key != null)
							world.Map.Actors.Value.Remove(key.Key);

						if (underCursor.First().Info.Name == "mpspawn" && world.Map.Players.Any(p => p.Key.StartsWith("Multi")))
							world.Map.Players.Remove(world.Map.Players.Last().Key);

						underCursor.First().Destroy();
					}

					if (world.Map.MapResources.Value[MouseCursorCell].Type != 0)
					{
						world.Map.MapResources.Value[MouseCursorCell] = new ResourceTile();
						resourceLayer.Destroy(MouseCursorCell);
						resourceLayer.Update();
					}
				}

				CurrentPreview = DragType.None;
				SelectedTileId = null;
				SelectedResourceTypeInfo = null;
				SelectedActor = null;
			}

			if (CurrentPreview != DragType.None)
			{
				if (world.Map.Contains(MouseCursorCell))
				{
					if (mi.Button == MouseButton.Left)
					{
						var random = new Random();

						if (CurrentPreview == DragType.Tiles && SelectedTileId.HasValue)
						{
							var tileset = rules.TileSets[world.Map.Tileset];
							var template = tileset.Templates.First(t => t.Value.Id == SelectedTileId.Value).Value;
							if (!(template.Size.Length > 1 && mi.Event == MouseInputEvent.Move))
							{
								var i = 0;
								for (var y = 0; y < template.Size.Y; y++)
								{
									for (var x = 0; x < template.Size.X; x++)
									{
										if (template.Contains(i) && template[i] != null)
										{
											var index = template.PickAny ? (byte)random.Next(0, template.TilesCount) : (byte)i;
											var cell = MouseCursorCell + new CVec(x, y);
											world.Map.MapTiles.Value[cell] = new TerrainTile(SelectedTileId.Value, index);
											world.Map.MapHeight.Value[cell] = (byte)(world.Map.MapHeight.Value[cell] + template[index].Height);
										}

										i++;
									}
								}
							}
						}

						if (CurrentPreview == DragType.Layers)
						{
							var type = (byte)SelectedResourceTypeInfo.ResourceType;
							var index = (byte)random.Next(SelectedResourceTypeInfo.MaxDensity);
							world.Map.MapResources.Value[MouseCursorCell] = new ResourceTile(type, index);
							resourceLayer.Update();
						}

						if (CurrentPreview == DragType.Actors && mi.Event == MouseInputEvent.Down)
						{
							var newActorReference = new ActorReference(SelectedActor);
							newActorReference.Add(new OwnerInit(SelectedOwner));

							newActorReference.Add(new LocationInit(MouseCursorCell));
							var info = rules.Actors[SelectedActor];
							var mobile = info.Traits.GetOrDefault<MobileInfo>();
							if (mobile != null && mobile.SharesCell)
							{
								var subcell = world.ActorMap.FreeSubCell(MouseCursorCell);
								if (subcell != SubCell.Invalid)
									newActorReference.Add(new SubCellInit(subcell));
							}

							var initDict = newActorReference.InitDict;

							if (info.Traits.Contains<IFacingInfo>())
								initDict.Add(new FacingInit(ActorFacing));

							if (info.Traits.Contains<TurretedInfo>())
								initDict.Add(new TurretFacingInit(ActorFacing));

							var actor = world.CreateActor(SelectedActor, initDict);

							var actorName = NextActorName();
							world.Map.Actors.Value.Add(actorName, newActorReference);

							if (SelectedActor == "mpspawn")
							{
								world.Map.UpdateMultiPlayers();

								var hue = (byte)Game.CosmeticRandom.Next(255);
								var sat = (byte)Game.CosmeticRandom.Next(70, 255);
								var lum = (byte)Game.CosmeticRandom.Next(140, 255);
								world.Map.Players.Last().Value.Color = new HSLColor(hue, sat, lum);

								var player = new Player(world, null, null, world.Map.Players.Last().Value);
								world.AddPlayer(player);

								var paletteName = "player" + player.InternalName;
								var playerColor = rules.Actors["player"].Traits.Get<PlayerColorPaletteInfo>();
								var remap = new PlayerColorRemap(playerColor.RemapIndex, world.Map.Players.Last().Value.Color, playerColor.Ramp);
								if (!worldRenderer.PaletteExists(paletteName))
									worldRenderer.AddPalette(paletteName, new ImmutablePalette(worldRenderer.Palette(playerColor.BasePalette).Palette, remap), true);

								foreach (var p in world.Players)
									foreach (var q in world.Players)
										if (!p.Stances.ContainsKey(q))
											p.Stances[q] = Stance.None;

								world.LocalPlayer.SetStance(player, Stance.Ally);

								actor.ChangeOwner(world.Players.Last());
							}
						}
					}
				}
			} else
			{
				if (mi.Button == MouseButton.Left)
				{
					var moveActor = underCursor.FirstOrDefault();
					if (moveActor != null)
					{
						SelectedActor = moveActor.Info.Name;
						SelectedOwner = moveActor.Owner.InternalName;
						var facing = moveActor.TraitOrDefault<IFacing>();
						if (facing != null)
							ActorFacing = facing.Facing;
						moveActor.Destroy();
					}
				}
			}

			return base.HandleMouseInput(mi);
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Key == Keycode.UP || e.Key == Keycode.DOWN || e.Key == Keycode.LEFT || e.Key == Keycode.RIGHT)
				UpdatePosition(Viewport.LastMousePos);

			return base.HandleKeyPress(e);
		}

		string NextActorName()
		{
			var id = world.Actors.Count();
			var possibleName = "Actor" + id.ToString();

			while (world.Map.Actors.Value.ContainsKey(possibleName))
			{
				id++;
				possibleName = "Actor" + id.ToString();
			}

			return possibleName;
		}

		void UpdatePosition(int2 mouseLocation)
		{
			var mouseWorldPixel = worldRenderer.Viewport.ViewToWorldPx(mouseLocation);
			var mouseWorldPosition = worldRenderer.Position(mouseWorldPixel);
			MouseCursorCell = world.Map.CellContaining(mouseWorldPosition);
		}
	}
}
