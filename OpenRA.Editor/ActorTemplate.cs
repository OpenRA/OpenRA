using System.Drawing;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Editor
{
	class ActorTemplate
	{
		public Bitmap Bitmap;
		public ActorInfo Info;
		public bool Centered;
	}

	class BrushTemplate
	{
		public Bitmap Bitmap;
		public ushort N;
	}

	class ResourceTemplate
	{
		public Bitmap Bitmap;
		public ResourceTypeInfo Info;
		public int Value;
	}

	class WaypointTemplate
	{
	}
}
