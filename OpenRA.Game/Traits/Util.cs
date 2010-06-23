#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	public static class Util
	{
		public static void TickFacing( ref int facing, int desiredFacing, int rot )
		{
			var leftTurn = ( facing - desiredFacing ) & 0xFF;
			var rightTurn = ( desiredFacing - facing ) & 0xFF;
			if( Math.Min( leftTurn, rightTurn ) < rot )
				facing = desiredFacing & 0xFF;
			else if( rightTurn < leftTurn )
				facing = ( facing + rot ) & 0xFF;
			else
				facing = ( facing - rot ) & 0xFF;
		}

		static float2[] fvecs = Graphics.Util.MakeArray<float2>( 32,
			i => -float2.FromAngle( i / 16.0f * (float)Math.PI ) * new float2( 1f, 1.3f ) );

		public static int GetFacing( float2 d, int currentFacing )
		{
			if( float2.WithinEpsilon( d, float2.Zero, 0.001f ) )
				return currentFacing;

			int highest = -1;
			float highestDot = -1.0f;

			for( int i = 0 ; i < fvecs.Length ; i++ )
			{
				float dot = float2.Dot( fvecs[ i ], d );
				if( dot > highestDot )
				{
					highestDot = dot;
					highest = i;
				}
			}

			return highest * 8;
		}

		public static int GetNearestFacing( int facing, int desiredFacing )
		{
			var turn = desiredFacing - facing;
			if( turn > 128 )
				turn -= 256;
			if( turn < -128 )
				turn += 256;

			return facing + turn;
		}

		public static int QuantizeFacing(int facing, int numFrames)
		{
			var step = 256 / numFrames;
			var a = (facing + step / 2) & 0xff;
			return a / step;
		}

		static float2 RotateVectorByFacing(float2 v, int facing, float ecc)
		{
			var angle = (facing / 256f) * (2 * (float)Math.PI);
			var sinAngle = (float)Math.Sin(angle);
			var cosAngle = (float)Math.Cos(angle);

			return new float2(
				(cosAngle * v.X + sinAngle * v.Y),
				ecc * (cosAngle * v.Y - sinAngle * v.X));
		}

		static float2 GetRecoil(Actor self, float recoil)
		{
			var abInfo = self.Info.Traits.GetOrDefault<AttackBaseInfo>();
			if (abInfo == null || abInfo.Recoil == 0) return float2.Zero;
			var rut = self.traits.GetOrDefault<RenderUnitTurreted>();
			if (rut == null) return float2.Zero;

			var facing = self.traits.Get<Turreted>().turretFacing;
			return RotateVectorByFacing(new float2(0, recoil * self.Info.Traits.Get<AttackBaseInfo>().Recoil), facing, .7f);
		}

		public static float2 CenterOfCell(int2 loc)
		{
			return new float2(12, 12) + Game.CellSize * (float2)loc;
		}

		public static float2 BetweenCells(int2 from, int2 to)
		{
			return 0.5f * (CenterOfCell(from) + CenterOfCell(to));
		}

		public static float2 GetTurretPosition(Actor self, Unit unit, int[] offset, float recoil)
		{
			if( unit == null ) return offset.AbsOffset();	/* things that don't have a rotating base don't need the turrets repositioned */

			var ru = self.traits.GetOrDefault<RenderUnit>();
			var numDirs = (ru != null) ? ru.anim.CurrentSequence.Facings : 8;
			var bodyFacing = unit.Facing;
			var quantizedFacing = QuantizeFacing(bodyFacing, numDirs) * (256 / numDirs);

			return (RotateVectorByFacing(offset.RelOffset(), quantizedFacing, .7f) + GetRecoil(self, recoil))
				+ offset.AbsOffset();
		}

		public static int2 AsInt2(this int[] xs) { return new int2(xs[0], xs[1]); }
		public static float2 RelOffset(this int[] offset) { return new float2(offset[0], offset[1]); }
		public static float2 AbsOffset(this int[] offset) { return new float2(offset.ElementAtOrDefault(2), offset.ElementAtOrDefault(3)); }

		public static Renderable Centered(Actor self, Sprite s, float2 location)
		{
			var pal = self.Owner == null ? "player0" : self.Owner.Palette;
			var loc = location - 0.5f * s.size;
			return new Renderable(s, loc.Round(), pal);
		}

		public static float GetEffectiveSpeed(Actor self, UnitMovementType umt)
		{		
			var unitInfo = self.Info.Traits.GetOrDefault<UnitInfo>();
			if( unitInfo == null ) return 0f;
			
			var terrain = 1f;
			if (umt != UnitMovementType.Fly)
			{
				var tt = self.World.GetTerrainType(self.Location);
				terrain = Rules.TerrainTypes[tt].GetSpeedModifier(umt)*self.World.WorldActor.traits
					.WithInterface<ICustomTerrain>()
					.Select(t => t.GetSpeedModifier(self.Location, umt))
					.Product();
			}
			var modifier = self.traits
				.WithInterface<ISpeedModifier>()
				.Select(t => t.GetSpeedModifier())
				.Product();
			return unitInfo.Speed * terrain * modifier;
		}

		public static IActivity SequenceActivities(params IActivity[] acts)
		{
			return acts.Reverse().Aggregate(
				(next, a) => { a.NextActivity = next; return a; });
		}

		public static float GetMaximumRange(Actor self)
		{
			return new[] { self.GetPrimaryWeapon(), self.GetSecondaryWeapon() }
				.Where(w => w != null).Max(w => w.Range);
		}

		public static Color ArrayToColor(int[] x) { return Color.FromArgb(x[0], x[1], x[2]); }

		public static int2 CellContaining(float2 pos) { return (1 / 24f * pos).ToInt2(); }
	}
}
