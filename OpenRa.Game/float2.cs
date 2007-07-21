using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using BluntDirectX.Direct3D;

namespace OpenRa.Game
{
	[StructLayout(LayoutKind.Sequential)]
	struct float2
	{
		public float X, Y;

		public float2(float x, float y) { X = x; Y = y; }
		public float2(PointF p) { X = p.X; Y = p.Y; }
		public float2(Point p) { X = p.X; Y = p.Y; }
		public float2(Size p) { X = p.Width; Y = p.Height; }
		public float2(SizeF p) { X = p.Width; Y = p.Height; }

		public PointF ToPointF() { return new PointF(X, Y); }

		public static implicit operator float2( int2 src )
		{
			return new float2( src.X, src.Y );
		}

		public static float2 operator +(float2 a, float2 b) { return new float2(a.X + b.X, a.Y + b.Y); }
		public static float2 operator -(float2 a, float2 b) { return new float2(a.X - b.X, a.Y - b.Y); }

		public static float2 operator -(float2 a) { return new float2(-a.X, -a.Y); }

		static float Lerp(float a, float b, float t) { return (1 - t) * a + t * b; }

		public static float2 Lerp(float2 a, float2 b, float t)
		{
			return new float2(
				Lerp(a.X, b.X, t),
				Lerp(a.Y, b.Y, t));
		}

		public static float2 Lerp(float2 a, float2 b, float2 t)
		{
			return new float2(
				Lerp(a.X, b.X, t.X),
				Lerp(a.Y, b.Y, t.Y));
		}

		public static float2 FromAngle(float a)
		{
			return new float2((float)Math.Sin(a), (float)Math.Cos(a));
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

		public static float2 operator /(float2 a, float2 b)
		{
			return new float2(a.X / b.X, a.Y / b.Y);
		}

		public static bool WithinEpsilon(float2 a, float2 b, float e)
		{
			float2 d = a - b;
			return Math.Abs(d.X) < e && Math.Abs(d.Y) < e;
		}

		public float2 Sign() { return new float2(Math.Sign(X), Math.Sign(Y)); }
		public static float Dot(float2 a, float2 b) { return a.X * b.X + a.Y * b.Y; }
		public float2 Round() { return new float2((float)Math.Round(X), (float)Math.Round(Y)); }
	}
}
