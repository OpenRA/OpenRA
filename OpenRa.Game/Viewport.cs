using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace OpenRa.Game
{
	class Viewport
	{
		readonly Size clientSize;
		PointF scrollPosition;

		public PointF ScrollPosition
		{
			get { return scrollPosition; }
			set { scrollPosition = value; }
		}

		public Size ClientSize
		{
			get { return clientSize; }
		} 

		public Viewport(Size clientSize)
		{
			this.clientSize = clientSize;
		}

		public float2 Size { get { return new float2(clientSize); } }
	}
}
