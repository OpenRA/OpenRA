#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.AI
{
	public enum SquadType { Assault, Air, Rush, Protection }

	public class Squad
	{
		public List<Actor> Units = new List<Actor>();
		public SquadType Type;

		internal World World;
		internal HackyAI Bot;
		internal MersenneTwister Random;

		internal Target Target;
		internal StateMachine FuzzyStateMachine;

		public Squad(HackyAI bot, SquadType type) : this(bot, type, null) { }

		public Squad(HackyAI bot, SquadType type, Actor target)
		{
			Bot = bot;
			World = bot.World;
			Random = bot.Random;
			Type = type;
			Target = Target.FromActor(target);
			FuzzyStateMachine = new StateMachine();

			switch (type)
			{
				case SquadType.Assault:
				case SquadType.Rush:
					FuzzyStateMachine.ChangeState(this, new GroundUnitsIdleState(), true);
					break;
				case SquadType.Air:
					FuzzyStateMachine.ChangeState(this, new AirIdleState(), true);
					break;
				case SquadType.Protection:
					FuzzyStateMachine.ChangeState(this, new UnitsForProtectionIdleState(), true);
					break;
			}
		}

		public void Update()
		{
			if (IsValid)
				FuzzyStateMachine.Update(this);
		}

		public bool IsValid { get { return Units.Any(); } }

		public Actor TargetActor
		{
			get { return Target.Actor; }
			set { Target = Target.FromActor(value); }
		}

		public bool IsTargetValid
		{
			get { return Target.IsValidFor(Units.FirstOrDefault()) && !Target.Actor.Info.HasTraitInfo<HuskInfo>(); }
		}

		public bool IsTargetVisible
		{
			get { return Bot.Player.PlayerActor.Owner.CanTargetActor(TargetActor); }
		}

		public WPos CenterPosition { get { return Units.Select(u => u.CenterPosition).Average(); } }
	}
}
