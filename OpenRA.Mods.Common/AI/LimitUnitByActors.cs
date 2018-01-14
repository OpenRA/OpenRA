using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.Common.AI
{
	[Desc("Adds metadata for the AI bots.",
		"Tells the AI what actors limit the amount of a given unit")]
	public class LimitUnitByActors
	{
		[FieldLoader.LoadUsing("LoadLimitUnits")]
		[Desc("The decisions associated with this power")]
		public readonly List<UnitLimit> LimitedUnits = new List<UnitLimit>();

		public LimitUnitByActors(MiniYaml yaml)
		{
			FieldLoader.Load(this, yaml);
		}

		static object LoadLimitUnits(MiniYaml yaml)
		{
			var ret = new List<UnitLimit>();
			foreach (var d in yaml.Nodes)
				if (d.Key.Split('@')[0] == "UnitLimit")
					ret.Add(new UnitLimit(d.Value));

			return ret;
		}

		// Checks if the Unit has any limiters telling the AI when to start making it
		public bool UnitHasStartLimiters(string unitName)
		{
			if (LimitedUnits != null)
			{
				var limitergroup = LimitedUnits.Where(ul => ul.LimitedUnit != null && ul.LimitedUnit.ToLower().Equals(unitName.ToLower()));
				if (limitergroup != null && limitergroup.Count() > 0)
				{
					var limiter = limitergroup.First();

					if (limiter != null)
					{
						return limiter.HasStartLimiters();
					}

					return false;
				}

				return false;
			}

			return false;
		}

		// Checks if the Unit has any Limiters telling the AI to stop making it
		public bool UnitHasStopLimiters(string unitName)
		{
			if (LimitedUnits != null)
			{
				var limitergroup = LimitedUnits.Where(ul => ul.LimitedUnit != null && ul.LimitedUnit.ToLower().Equals(unitName.ToLower()));
				if (limitergroup != null && limitergroup.Count() > 0)
				{
					var limiter = limitergroup.First();

					if (limiter != null)
					{
						return limiter.HasStopLimiters();
					}

					return false;
				}

				return false;
			}

			return false;
		}

		// Checks if the AI should stop making a unit
		public bool StopActorLimitsMet(string unitName, World world, Player player)
		{
			if (LimitedUnits != null)
			{
				var limitergroup = LimitedUnits.Where(ul => ul.LimitedUnit != null && ul.LimitedUnit.ToLower().Equals(unitName.ToLower()));
				if (limitergroup != null && limitergroup.Count() > 0)
				{
					var limiter = limitergroup.First();

					if (limiter != null)
					{
						return limiter.StopLimitsMet(world, player);
					}

					return false;
				}

				return false;
			}

			return false;
		}

		// Should be used as a not statement to check if the limit hasn't been reached yet
		// Checks if the AI should start making a unit
		public bool StartActorLimitsMet(string unitName, World world, Player player)
		{
			if (LimitedUnits != null)
			{
				var limitergroup = LimitedUnits.Where(ul => ul.LimitedUnit != null && ul.LimitedUnit.ToLower().Equals(unitName.ToLower()));
				if (limitergroup != null && limitergroup.Count() > 0)
				{
					var limiter = limitergroup.First();

					if (limiter != null)
					{
						return limiter.StartLimitsMet(world, player);
					}

					return false;
				}

				return false;
			}

			return false;
		}

		[Desc("Used to specify the Limiting actors of a given Unit.")]
		public class UnitLimit
		{
			[Desc("Actor that is going to be limited by another actor or actors.")]
			public readonly string LimitedUnit = null;

			[Desc("What actors should limit when the AI stops producing the named Unit")]
			public readonly Dictionary<string, int> StopProductionLimiters = null;

			[Desc("What actors should limit when the AI starts producing the named Unit.")]
			public readonly Dictionary<string, int> StartProductionLimiters = null;

			[Desc("Do you want to check if all Stop Limiters are true or if any one of them are true.",
				"True = checks if all are true",
				"False = checks if any one of them are true")]
			public readonly bool CheckAllStopLimiters = false;

			[Desc("Do you want to check if all Start Limiters are true or if any one of them are true.",
				"True = checks if all are true",
				"False = checks if any one of them are true")]
			public readonly bool CheckAllStartLimiters = false;

			public UnitLimit(MiniYaml yaml)
			{
				FieldLoader.Load(this, yaml);

				if (StopProductionLimiters == null && StartProductionLimiters == null)
				{
					throw new YamlException("Both Start and Stop Production Limiters cannot be null at the same time!");
				}
			}

			public bool HasStartLimiters() { return StartProductionLimiters != null && StartProductionLimiters.Any(); }

			public bool HasStopLimiters() { return StopProductionLimiters != null && StopProductionLimiters.Any(); }

			public bool StopLimitsMet(World world, Player player)
			{
				if (CheckAllStopLimiters)
				{
					return AllStopProductionLimitersMet(world, player);
				}
				else
				{
					return AnyStopProductionLimiterMet(world, player);
				}
			}

			public bool StartLimitsMet(World world, Player player)
			{
				if (CheckAllStartLimiters)
				{
					return AllStartProductionLimitersMet(world, player);
				}
				else
				{
					return AnyStartProductionLimiterMet(world, player);
				}
			}

			// Stop Building if everything at or above the limits
			// If anything is below the limit return false, if nothing below the limits return true
			private bool AllStopProductionLimitersMet(World world, Player player)
			{
				if (StopProductionLimiters != null)
				{
					foreach (var actor in StopProductionLimiters)
					{
						if (ActorBelowLimit(actor.Key.ToString(), actor.Value, world, player))
						{
							return false;
						}
					}
				}

				return true;
			}

			// Stop Building if anything above the limits
			// if anything above the limit return true, if nothing above the limit return false
			private bool AnyStopProductionLimiterMet(World world, Player player)
			{
				if (StopProductionLimiters != null)
				{
					foreach (var actor in StopProductionLimiters)
					{
						if (ActorAboveLimit(actor.Key.ToString(), actor.Value, world, player))
						{
							return true;
						}
					}
				}

				return false;
			}

			// Start building if everything above the limits
			private bool AllStartProductionLimitersMet(World world, Player player)
			{
				if (StartProductionLimiters != null)
				{
					foreach (var actor in StartProductionLimiters)
					{
						if (ActorBelowLimit(actor.Key.ToString(), actor.Value, world, player))
						{
							return false;
						}
					}
				}

				return true;
			}

			// Start building if anything above the limits
			private bool AnyStartProductionLimiterMet(World world, Player player)
			{
				if (StartProductionLimiters != null)
				{
					foreach (var actor in StartProductionLimiters)
					{
						if (ActorAboveLimit(actor.Key.ToString(), actor.Value, world, player))
						{
							return true;
						}
					}
				}

				return false;
			}

			private bool ActorBelowLimit(string limitingActor, int limit, World world, Player player)
			{
				return world.Actors.Count(a => a.Owner == player && a.Info.Name.ToLowerInvariant().Equals(limitingActor.ToLowerInvariant())) < limit;
			}

			private bool ActorAboveLimit(string limitingActor, int limit, World world, Player player)
			{
				return world.Actors.Count(a => a.Owner == player && a.Info.Name.ToLowerInvariant().Equals(limitingActor.ToLowerInvariant())) >= limit;
			}
		}
	}
}
