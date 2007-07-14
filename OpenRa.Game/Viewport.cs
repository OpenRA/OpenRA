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
		PointF scrollPosition;

		public PointF ScrollPosition
		{
			get { return scrollPosition; }
		}

		public void Scroll(float2 delta)
		{
			float2 scrollPos = new float2(ScrollPosition) + delta;
			scrollPos = scrollPos.Constrain(new Range<float2>(float2.Zero, mapSize));
			scrollPosition = scrollPos.ToPointF();
		}

		public Size ClientSize
		{
			get { return clientSize; }
		} 

		public Viewport(Size clientSize, float2 mapSize)
		{
			this.clientSize = clientSize;
			this.mapSize = 24 * mapSize - Size + new float2(128, 0);
		}

		public float2 Size { get { return new float2(clientSize); } }
	}
}
