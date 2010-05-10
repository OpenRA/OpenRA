using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using OpenRA.GameRules;
using System.Windows.Forms;

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
}
