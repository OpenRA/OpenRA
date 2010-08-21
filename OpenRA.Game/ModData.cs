using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;

namespace OpenRA
{
	public class ModData
	{
		public readonly Manifest Manifest;
		public readonly ObjectCreator ObjectCreator;

		public ModData( Manifest manifest )
		{
			Manifest = manifest;
			ObjectCreator = new ObjectCreator( manifest );
		}
	}
}
