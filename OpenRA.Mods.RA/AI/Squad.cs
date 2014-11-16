#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.AI
{
	public enum SquadType { Assault, Air, Rush, Protection }

	public class Squad
	{
		public List<Actor> units = new List<Actor>();
		public SquadType type;

		internal World world;
		internal HackyAI bot;
		internal MersenneTwister random;

		internal Target target;
		internal StateMachine fsm;

		internal AttackOrFleeFuzzy attackOrFleeFuzzy = new AttackOrFleeFuzzy();

		public Squad(HackyAI bot, SquadType type) : this(bot, type, null) { }

		public Squad(HackyAI bot, SquadType type, Actor target)
		{
			this.bot = bot;
			this.world = bot.world;
			this.random = bot.random;
			this.type = type;
			this.target = OpenRA.Traits.Target.FromActor(target);
			fsm = new StateMachine();

			switch (type)
			{
				case SquadType.Assault:
				case SquadType.Rush:
					fsm.ChangeState(this, new GroundUnitsIdleState(), true);
					break;
				case SquadType.Air:
					fsm.ChangeState(this, new AirIdleState(), true);
					break;
				case SquadType.Protection:
					fsm.ChangeState(this, new UnitsForProtectionIdleState(), true);
					break;
			}
		}

		public void Update()
		{
			if (IsValid)
				fsm.Update(this);
		}

		public bool IsValid { get { return units.Any(); } }

		public Actor Target
		{
			get { return target.Actor; }
			set { target = OpenRA.Traits.Target.FromActor(value); }
		}

		public bool TargetIsValid
		{
			get { return target.IsValidFor(units.FirstOrDefault()) && !target.Actor.HasTrait<Husk>(); }
		}

		public WPos CenterPosition { get { return units.Select(u => u.CenterPosition).Average(); } }
	}
}
