using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;
using IjwFramework.Types;
using OpenRa.Game.GameRules;

namespace OpenRa.Game.Traits
{
	class InfantrySquad : ITick, IRender
	{
		readonly List<Soldier> elements = new List<Soldier>();

		readonly int2[][] elementOffsets = new [] 
		{
			new int2[] {},
			new [] { new int2(0,0) },
			new [] { new int2(-5,-5), new int2(5,5) },
			new [] { new int2(-6,5), new int2(0, -5), new int2(6,4) },	/* todo: move squad arrangements ! */
		};

		public InfantrySquad(Actor self)
		{
			var ii = (UnitInfo.InfantryInfo)self.unitInfo;
			for (int i = 0; i < ii.SquadSize; i++)
				elements.Add(new Soldier(self.unitInfo.Name, 
					self.CenterLocation.ToInt2() + elementOffsets[ii.SquadSize][i]));
		}

		public void Tick(Actor self)
		{
			for (int i = 0; i < elements.Count; i++)
				elements[i].Tick(
					self.CenterLocation.ToInt2() + elementOffsets[elements.Count][i], self);
		}

		public IEnumerable<Tuple<Sprite, float2, int>> Render(Actor self)
		{
			return elements.Select(
				e => Util.Centered(self, e.anim.Image, e.location))
				.OrderBy( a => a.b.Y );		/* important to y-order elements of a squad! */
		}
	}

	class Soldier
	{
		public Animation anim;
		public float2 location;
		string name;
		int facing = 128;
		float speed;

		string currentSequence;

		void PlaySequence(string seq, bool isFacing)
		{
			if (currentSequence == seq) return;

			if (isFacing)
				anim.PlayFetchIndex(seq, () => Util.QuantizeFacing(facing, anim.CurrentSequence.Length));
			else
				anim.PlayRepeatingPreservingPosition(seq);

			currentSequence = seq;
		}

		public Soldier(string type, int2 initialLocation)
		{
			name = type;
			anim = new Animation(type);
			anim.PlayFetchIndex("stand",
				() => Util.QuantizeFacing(facing, anim.CurrentSequence.Length));
			location = initialLocation;
			speed = ((UnitInfo.InfantryInfo)Rules.UnitInfo[name]).Speed / 2;
		}

		public void Tick( int2 desiredLocation, Actor self )
		{
			anim.Tick();
			var d = (desiredLocation - location);

			facing = /*Util.GetFacing(d, facing); */ self.traits.Get<Mobile>().facing;

			if (float2.WithinEpsilon(d, float2.Zero, .1f))
				PlaySequence("stand", true);
			else
				PlaySequence("run-" + Util.QuantizeFacing(facing, 8), false);

			if (d.Length <= speed)
				location = desiredLocation;
			else
				location += (speed / d.Length) * d;
		}
	}
}
