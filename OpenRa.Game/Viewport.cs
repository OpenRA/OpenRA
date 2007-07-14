using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using BluntDirectX.Direct3D;

namespace OpenRa.Game
{
	class Viewport
	{
		readonly Size clientSize;
		readonly float2 mapSize;
		float2 scrollPosition;

		public PointF ScrollPosition { get { return scrollPosition.ToPointF(); } }
		public Size ClientSize { get { return clientSize; } }

		public void Scroll(float2 delta)
		{
			scrollPosition = (scrollPosition + delta).Constrain(new Range<float2>(float2.Zero, mapSize));
		}

		public Viewport(Size clientSize, float2 mapSize)
		{
			this.clientSize = clientSize;
			this.mapSize = 24 * mapSize - new float2(clientSize) + new float2(128, 0);
		}
	}
}
