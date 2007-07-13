using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using System.Drawing;
using BluntDirectX.Direct3D;

namespace OpenRa.Game
{
	static class UnitSheetBuilder
	{
		static readonly Package unitsPackage = new Package( "../../../conquer.mix" );
		public static readonly List<SheetRectangle<Sheet>> McvSheet = new List<SheetRectangle<Sheet>>();

		static TileSheetBuilder<Sheet> builder;
		static List<Sheet> sheets = new List<Sheet>();
		static Size pageSize = new Size(1024, 512);

		public static void Initialize( GraphicsDevice device )
		{
			Provider<Sheet> sheetProvider = delegate
			{
				Sheet sheet = new Sheet(new Bitmap(pageSize.Width, pageSize.Height), device);
				sheets.Add(sheet);
				return sheet;
			};

			builder = new TileSheetBuilder<Sheet>(pageSize, sheetProvider);
		}

		public static void AddUnit( string name, Palette pal )
		{
			ShpReader reader = new ShpReader( unitsPackage.GetContent( name + ".shp" ) );
			foreach( ImageHeader h in reader )
			{
				Bitmap bitmap = BitmapBuilder.FromBytes( h.Image, reader.Width, reader.Height, pal );

				SheetRectangle<Sheet> rect = builder.AddImage( bitmap.Size );
				using( Graphics g = Graphics.FromImage( rect.sheet.bitmap ) )
					g.DrawImage( bitmap, rect.origin );

				McvSheet.Add( rect );
			}
		}
	}
}
