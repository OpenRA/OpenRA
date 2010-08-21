using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA
{
	public class ModData
	{
		public readonly Manifest Manifest;
		public readonly ObjectCreator ObjectCreator;
		public readonly CursorSheetBuilder CursorSheetBuilder;
		public SheetBuilder SheetBuilder { get { return SheetBuilder.SharedInstance; } }

		public ModData( Manifest manifest )
		{
			Manifest = manifest;
			ObjectCreator = new ObjectCreator( manifest );
			FileSystem.LoadFromManifest( manifest );
			CursorSheetBuilder = new CursorSheetBuilder( this );
		}
	}
}
