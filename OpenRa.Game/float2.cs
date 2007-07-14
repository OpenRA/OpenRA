using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using BluntDirectX.Direct3D;

namespace OpenRa.Game
{
	[StructLayout(LayoutKind.Sequential)]
	class float2
	{
		public float X, Y;

		public float2(float x, float y)
		{
			X = x;
			Y = y;
		}

		public PointF ToPointF()
		{
			return new PointF(X, Y);
		}

		public float2(PointF p)
		{
			X = p.X;
			Y = p.Y;
		}

		public float2(Point p)
		{
			X = p.X;
			Y = p.Y;
		}

		public float2(Size p)
		{
			X = p.Width;
			Y = p.Height;
		}

		public float2(SizeF p)
		{
			X = p.Width;
			Y = p.Height;
		}

		public static float2 operator +(float2 a, float2 b)
		{
			return new float2(a.X + b.X, a.Y + b.Y);
		}

		public static float2 operator -(float2 a, float2 b)
		{
			return new float2(a.X - b.X, a.Y - b.Y);
		}

		public float2 Constrain(Range<float2> r)
		{
			return new float2(
				Util.Constrain(X, new Range<float>(r.Start.X, r.End.X)),
				Util.Constrain(Y, new Range<float>(r.Start.Y, r.End.Y)));
		}

		public static float2 operator *(float a, float2 b)
		{
			return new float2(a * b.X, a * b.Y);
		}

		public static readonly float2 Zero = new float2(0, 0);
	}
}
