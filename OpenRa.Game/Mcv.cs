using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using System.Drawing;
using BluntDirectX.Direct3D;

namespace OpenRa.Game
{
	class Mcv : Actor
	{
		static Range<int>? mcvRange = null;
		MoveOrder currentOrder = null;
		int facing = 0;

		public Mcv( float2 location, int palette )
		{
			this.location = location;
			this.palette = palette;

			if (mcvRange == null)
				mcvRange = UnitSheetBuilder.AddUnit("mcv");
		}

		static float2[] fvecs;

		static Mcv()
		{
			fvecs = new float2[32];
			for (int i = 0; i < 32; i++)
			{
				float angle = i / 16.0f * (float)Math.PI;
				fvecs[i] = new float2(-(float)Math.Sin(angle), -(float)Math.Cos(angle));
			}
		}

		int GetFacing(float2 d)
		{
			if (float2.WithinEpsilon(d, float2.Zero, 0.001f))
				return facing;

			int highest = -1;
			float highestDot = -1.0f;

			for (int i = 0; i < 32; i++)
			{
				float dot = float2.Dot(fvecs[i], d);
				if (dot > highestDot)
				{
					highestDot = dot;
					highest = i;
				}
			}

			return highest;
		}

		public override Sprite[] CurrentImages
		{
			get
			{
				return new Sprite[] { UnitSheetBuilder.sprites[facing + mcvRange.Value.Start] };
			}
		}

		public void Accept(MoveOrder o)
		{
			currentOrder = o;
		}

		const float Speed = 48.0f;

		public override void Tick( double t )
		{
			if (currentOrder == null)
				return;

			if (float2.WithinEpsilon(location, currentOrder.Destination, 1.0f))
				return;

			Range<float2> r = new Range<float2>(
				new float2(-Speed * (float)t, -Speed * (float)t),
				new float2(Speed * (float)t, Speed * (float)t));

			float2 d = (currentOrder.Destination - location).Constrain(r);

			int desiredFacing = GetFacing(d);
			if (desiredFacing == facing)
				location += d;
			else
			{
				int df = desiredFacing - facing;
				if (df < 0)
					df = 32 - df;

				if (df < 32 - df)
					facing = (facing + 1) % 32;
				else
					facing = (facing + 31) % 32;
			}
		}
	}
}
