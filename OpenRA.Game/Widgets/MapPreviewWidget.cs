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
		
		public Func<MapStub> Map = () => {return null;};
		public Action<int> OnSpawnClick = spawn => {};
		public Func<Dictionary<int2,Color>> SpawnColors = () => {return new Dictionary<int2, Color>(); };
		
		public MapPreviewWidget() : base() { }

		public MapPreviewWidget(Widget other)
			: base(other)
		{
			lastMap = (other as MapPreviewWidget).lastMap;
		}

		const int closeEnough = 50;
		public override bool HandleInput(MouseInput mi)
		{			
			var map = Map();
			if (map == null)
				return false;
			
			if (mi.Event == MouseInputEvent.Down && mi.Button == MouseButton.Left)
			{
				var container = new Rectangle(RenderOrigin.X, RenderOrigin.Y, Parent.Bounds.Width, Parent.Bounds.Height);
				
				var p = map.Waypoints
					.Select((sp, i) => Pair.New(map.ConvertToPreview(sp.Value, container), i))
					.Where(a => (a.First - mi.Location).LengthSquared < closeEnough)
					.Select(a => a.Second + 1)
					.FirstOrDefault();
				OnSpawnClick(p);
				return true;
			}

			return false;
		}

		public override Widget Clone() { return new MapPreviewWidget(this); }

		public override void DrawInner( World world )
		{
			var map = Map();
			if( map == null ) return;
			if (lastMap != map)
			{
				mapPreviewDirty = true;
				lastMap = map;
			}
			
			var mapRect = map.PreviewBounds( RenderBounds );

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

			DrawSpawnPoints( map, new Rectangle(RenderOrigin.X, RenderOrigin.Y, Parent.Bounds.Width, Parent.Bounds.Height ), world );
		}

		void DrawSpawnPoints(MapStub map, Rectangle container, World world)
		{		
			var colors = SpawnColors();
			foreach (var p in map.SpawnPoints)
			{
				var pos = map.ConvertToPreview(p, container) - new int2(8, 8);
				var sprite = "unowned";
				
				if (colors.ContainsKey(p))
				{
					Game.chrome.lineRenderer.FillRect(new RectangleF(
						Game.viewport.Location.X + pos.X + 2,
						Game.viewport.Location.Y + pos.Y + 2,
						12, 12), colors[p]);

					sprite = "owned";
				}
				
				Game.chrome.renderer.RgbaSpriteRenderer.DrawSprite(
						ChromeProvider.GetImage(Game.chrome.renderer, "spawnpoints", sprite), pos, "chrome");
			}

			Game.chrome.lineRenderer.Flush();
			Game.chrome.renderer.RgbaSpriteRenderer.Flush();
		}
	}
}
