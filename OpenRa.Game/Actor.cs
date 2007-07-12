using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using OpenRa.FileFormats;
using BluntDirectX.Direct3D;
using System.Windows.Forms;

namespace OpenRa.Game
{
	abstract class Actor
	{
		public PointF location;
		public abstract SheetRectangle<Sheet>[] CurrentImages { get; }
	}
}
