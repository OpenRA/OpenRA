using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Graphics;
using System.Drawing;
using OpenRA.FileFormats;

namespace OpenRA.Widgets
{
	class MapPreviewWidget : Widget
	{
		Sheet mapChooserSheet;
		Sprite mapChooserSprite;
		bool mapPreviewDirty = true;
		MapStub lastMap;
		
		public MapPreviewWidget() : base() { }

		public MapPreviewWidget(Widget other)
			: base(other)
		{
			lastMap = (other as MapPreviewWidget).lastMap;
		}

		Session.Client ClientForSpawnpoint(int i)
		{
			return Game.LobbyInfo.Clients.FirstOrDefault(c => c.SpawnPoint == i + 1);
		}

		const int closeEnough = 50;
		
		public override bool HandleInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Down && mi.Button == MouseButton.Left)
			{
				var container = new Rectangle(DrawPosition().X, DrawPosition().Y, Parent.Bounds.Width, Parent.Bounds.Height);

				var points = Game.chrome.currentMap.Waypoints
					.Select((sp, i) => Pair.New(Game.chrome.currentMap.ConvertToPreview(sp.Value, container), i))
					.Where(a => ClientForSpawnpoint(a.Second) == null && (a.First - mi.Location).LengthSquared < closeEnough)
					.ToArray();

				if (points.Length > 0)
					Game.IssueOrder(Order.Chat("/spawn {0}".F(points[0].Second + 1)));

				return points.Length > 0;
			}

			return false;
		}

		public override Widget Clone() { return new MapPreviewWidget(this); }

		public override void DrawInner( World world )
		{
			var map = Game.chrome.currentMap;
			if( map == null ) return;

			if (lastMap != map)
			{
				mapPreviewDirty = true;
				lastMap = map;
			}
			var pos = DrawPosition();
			var rect = new Rectangle(pos.X, pos.Y, Bounds.Width, Bounds.Height);
			var mapRect = map.PreviewBounds( new Rectangle( rect.X, rect.Y, rect.Width, rect.Height ) );

			if( mapPreviewDirty )
			{
				if( mapChooserSheet == null || mapChooserSheet.Size.Width != map.Width || mapChooserSheet.Size.Height != map.Height )
					mapChooserSheet = new Sheet( Game.renderer, new Size( map.Width, map.Height ) );

				mapChooserSheet.Texture.SetData( map.Preview.Value );
				mapChooserSprite = new Sprite( mapChooserSheet, new Rectangle( 0, 0, map.Width, map.Height ), TextureChannel.Alpha );
				mapPreviewDirty = false;
			}

			Game.chrome.renderer.RgbaSpriteRenderer.DrawSprite( mapChooserSprite,
				new float2( mapRect.Location ),
				"chrome",
				new float2( mapRect.Size ) );

			DrawSpawnPoints( map, new Rectangle(pos.X, pos.Y, Parent.Bounds.Width, Parent.Bounds.Height ), world );
		}

		void DrawSpawnPoints(MapStub map, Rectangle container, World world)
		{
			var points = map.Waypoints
				.Select((sp, i) => Pair.New(sp, Game.LobbyInfo.Clients.FirstOrDefault(
					c => c.SpawnPoint == i + 1)))
				.ToList();

			foreach (var p in points)
			{
				var pos = map.ConvertToPreview(p.First.Value, container) - new int2(8, 8);

				if (p.Second == null)
					Game.chrome.renderer.RgbaSpriteRenderer.DrawSprite(
						ChromeProvider.GetImage(Game.chrome.renderer, "spawnpoints", "unowned"), pos, "chrome");
				else
				{
					var playerColors = Game.world.PlayerColors();
					Game.chrome.lineRenderer.FillRect(new RectangleF(
						Game.viewport.Location.X + pos.X + 2,
						Game.viewport.Location.Y + pos.Y + 2,
						12, 12), playerColors[p.Second.PaletteIndex % playerColors.Count()].Color);

					Game.chrome.renderer.RgbaSpriteRenderer.DrawSprite(
						ChromeProvider.GetImage(Game.chrome.renderer, "spawnpoints", "owned"), pos, "chrome");
				}
			}

			Game.chrome.lineRenderer.Flush();
			Game.chrome.renderer.RgbaSpriteRenderer.Flush();
		}
	}
}
