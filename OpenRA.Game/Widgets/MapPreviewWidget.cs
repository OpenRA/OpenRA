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
		bool showMapChooser = false;
		bool mapPreviewDirty = true;
		MapStub lastMap;

		public override void Draw( World world )
		{
			var map = Game.chrome.currentMap;
			if( map == null ) return;

			if (lastMap != map)
			{
				mapPreviewDirty = true;
				lastMap = map;
			}

			var mapRect = map.PreviewBounds( new Rectangle( Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height ) );

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

			Game.chrome.DrawSpawnPoints( map, Parent.Bounds );
			base.Draw( world );
		}
	}
}
