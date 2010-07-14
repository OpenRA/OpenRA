using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	class MapPreviewWidget : Widget
	{
		Sheet mapChooserSheet;
		Sprite mapChooserSprite;
		bool mapPreviewDirty = true;
		MapStub lastMap;

		public Func<MapStub> Map = () => null;
		public Action<int> OnSpawnClick = spawn => {};
		public Func<Dictionary<int2, Color>> SpawnColors = () => new Dictionary<int2, Color>();
		
		public MapPreviewWidget() : base() { }

		protected MapPreviewWidget(MapPreviewWidget other)
			: base(other)
		{
			lastMap = other.lastMap;
			Map = other.Map;
			OnSpawnClick = other.OnSpawnClick;
			SpawnColors = other.SpawnColors;
		}
		
		static Sprite UnownedSpawn = null;
		static Sprite OwnedSpawn = null;
		const int closeEnough = 50;

		public override bool HandleInput(MouseInput mi)
		{			
			var map = Map();
			if (map == null)
				return false;
			
			if (mi.Event == MouseInputEvent.Down && mi.Button == MouseButton.Left)
			{			
				var p = map.Waypoints
					.Select((sp, i) => Pair.New(map.ConvertToPreview(sp.Value, RenderBounds), i))
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
			if (UnownedSpawn == null)
				UnownedSpawn = ChromeProvider.GetImage(Game.chrome.renderer, "spawnpoints", "unowned");
			if (OwnedSpawn == null)
				OwnedSpawn = ChromeProvider.GetImage(Game.chrome.renderer, "spawnpoints", "owned");
			
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

			DrawSpawnPoints( map, world );
		}

		void DrawSpawnPoints(MapStub map, World world)
		{		
			var colors = SpawnColors();
			foreach (var p in map.SpawnPoints)
			{
				var pos = map.ConvertToPreview(p, RenderBounds);
				var sprite = UnownedSpawn;
				var offset = new int2(-UnownedSpawn.bounds.Width/2, -UnownedSpawn.bounds.Height/2);

				if (colors.ContainsKey(p))
				{
					sprite = OwnedSpawn;
					offset = new int2(-OwnedSpawn.bounds.Width/2, -OwnedSpawn.bounds.Height/2);
					
					Game.chrome.lineRenderer.FillRect(new RectangleF(
						Game.viewport.Location.X + pos.X + offset.X + 2,
						Game.viewport.Location.Y + pos.Y + offset.Y + 2,
						12, 12), colors[p]);
				}
				
				Game.chrome.renderer.RgbaSpriteRenderer.DrawSprite(sprite, pos + offset, "chrome");
			}

			Game.chrome.lineRenderer.Flush();
			Game.chrome.renderer.RgbaSpriteRenderer.Flush();
		}
	}
}
