#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NLua;
using OpenRA.Effects;
using OpenRA.FileSystem;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;
using OpenRA.Network;
using OpenRA.Scripting;
using OpenRA.Support;
using OpenRA.Traits;
using WorldRenderer = OpenRA.Graphics.WorldRenderer;

namespace OpenRA.Mods.RA.Scripting
{
	[Desc("Part of the legacy Lua API.")]
	public class LuaScriptInterfaceInfo : ITraitInfo, Requires<SpawnMapActorsInfo>
	{
		public readonly string[] LuaScripts = { };

		public object Create(ActorInitializer init) { return new LuaScriptInterface(this); }
	}

	public sealed class LuaScriptInterface : IWorldLoaded, ITick, IDisposable
	{
		World world;
		SpawnMapActors sma;
		readonly LuaScriptContext context = new LuaScriptContext();
		readonly LuaScriptInterfaceInfo info;

		public LuaScriptInterface(LuaScriptInterfaceInfo info)
		{
			this.info = info;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;
			sma = world.WorldActor.Trait<SpawnMapActors>();

			context.Lua["World"] = w;
			context.Lua["WorldRenderer"] = wr;
			context.RegisterObject(this, "Internal", false);
			context.RegisterType(typeof(WVec), "WVec", true);
			context.RegisterType(typeof(CVec), "CVec", true);
			context.RegisterType(typeof(WPos), "WPos", true);
			context.RegisterType(typeof(CPos), "CPos", true);
			context.RegisterType(typeof(WRot), "WRot", true);
			context.RegisterType(typeof(WAngle), "WAngle", true);
			context.RegisterType(typeof(WRange), "WRange", true);
			context.RegisterType(typeof(int2), "int2", true);
			context.RegisterType(typeof(float2), "float2", true);

			context.LoadLuaScripts(f => GlobalFileSystem.Open(f).ReadAllText(), Game.modData.Manifest.LuaScripts);

			AddMapActorGlobals();

			context.LoadLuaScripts(f => w.Map.Container.GetContent(f).ReadAllText(), info.LuaScripts);

			context.InvokeLuaFunction("WorldLoaded");
		}

		void AddMapActorGlobals()
		{
			foreach (var kv in sma.Actors)
			{
				if (context.Lua[kv.Key] != null)
					context.ShowErrorMessage("{0}: The global name '{1}' is reserved and may not be used by map actor {2}".F(GetType().Name, kv.Key, kv.Value), null);
				else
					context.Lua[kv.Key] = kv.Value;
			}
		}

		public void Tick(Actor self)
		{
			using (new PerfSample("tick_lua"))
				context.InvokeLuaFunction("Tick");
		}

		public void Dispose()
		{
			context.Dispose();
		}

		[LuaGlobal]
		public object New(string typeName, LuaTable args)
		{
			var type = Game.modData.ObjectCreator.FindType(typeName);
			if (type == null)
				throw new InvalidOperationException("Cannot locate type: {0}".F(typeName));
			if (args == null)
				return Activator.CreateInstance(type);
			var argsArray = ConvertArgs(args);
			return Activator.CreateInstance(type, argsArray);
		}

		static object[] ConvertArgs(LuaTable args)
		{
			var argsArray = new object[args.Keys.Count];
			for (var i = 1; i <= args.Keys.Count; i++)
			{
				var arg = args[i] as LuaTable;
				if (arg != null && arg[1] != null && arg[2] != null)
					argsArray[i - 1] = Convert.ChangeType(arg[1], Enum<TypeCode>.Parse(arg[2].ToString()));
				else
					argsArray[i - 1] = args[i];
			}
			return argsArray;
		}

		[LuaGlobal]
		public void Debug(object obj)
		{
			if (obj != null)
				Game.Debug(obj.ToString());
		}

		[LuaGlobal]
		public object TraitOrDefault(Actor actor, string className)
		{
			var type = Game.modData.ObjectCreator.FindType(className);
			if (type == null)
				return null;

			var method = typeof(Actor).GetMethod("TraitOrDefault");
			var genericMethod = method.MakeGenericMethod(type);
			return genericMethod.Invoke(actor, null);
		}

		[LuaGlobal]
		public object Trait(Actor actor, string className)
		{
			var ret = TraitOrDefault(actor, className);
			if (ret == null)
				throw new InvalidOperationException("Actor {0} does not have trait of type {1}".F(actor, className));
			return ret;
		}

		[LuaGlobal]
		public bool HasTrait(Actor actor, string className)
		{
			var ret = TraitOrDefault(actor, className);
			return ret != null;
		}

		[LuaGlobal]
		public object[] ActorsWithTrait(string className)
		{
			var type = Game.modData.ObjectCreator.FindType(className);
			if (type == null)
				throw new InvalidOperationException("Cannot locate type: {0}".F(className));

			var method = typeof(World).GetMethod("ActorsWithTrait");
			var genericMethod = method.MakeGenericMethod(type);
			var result = ((IEnumerable)genericMethod.Invoke(world, null)).Cast<object>().ToArray();
			return result;
		}

		[LuaGlobal]
		public object TraitInfoOrDefault(string actorType, string className)
		{
			var type = Game.modData.ObjectCreator.FindType(className);
			if (type == null || !world.Map.Rules.Actors.ContainsKey(actorType))
				return null;

			return world.Map.Rules.Actors[actorType].Traits.GetOrDefault(type);
		}

		[LuaGlobal]
		public object TraitInfo(string actorType, string className)
		{
			var ret = TraitInfoOrDefault(actorType, className);
			if (ret == null)
				throw new InvalidOperationException("Actor type {0} does not have trait info of type {1}".F(actorType, className));
			return ret;
		}

		[LuaGlobal]
		public bool HasTraitInfo(string actorType, string className)
		{
			var ret = TraitInfoOrDefault(actorType, className);
			return ret != null;
		}

		[LuaGlobal]
		public void RunAfterDelay(double delay, Action func)
		{
			world.AddFrameEndTask(w => w.Add(new DelayedAction((int)delay, func)));
		}

		[LuaGlobal]
		public void PlaySpeechNotification(Player player, string notification)
		{
			Sound.PlayNotification(world.Map.Rules, player, "Speech", notification, player != null ? player.Country.Race : null);
		}

		[LuaGlobal]
		public void PlaySoundNotification(Player player, string notification)
		{
			Sound.PlayNotification(world.Map.Rules, player, "Sounds", notification, player != null ? player.Country.Race : null);
		}

		[LuaGlobal]
		public void WaitFor(Actor actor, Func<bool> func)
		{
			actor.QueueActivity(new WaitFor(func));
		}

		[LuaGlobal]
		public void CallFunc(Actor actor, Action func)
		{
			actor.QueueActivity(new CallFunc(func));
		}

		[LuaGlobal]
		public int GetFacing(object vec, double currentFacing)
		{
			if (vec is CVec)
				return world.Map.FacingBetween(CPos.Zero, CPos.Zero + (CVec)vec, (int)currentFacing);
			if (vec is WVec)
				return Util.GetFacing((WVec)vec, (int)currentFacing);
			throw new ArgumentException("Unsupported vector type: {0}".F(vec.GetType()));
		}

		[LuaGlobal]
		public WRange GetWRangeFromCells(double cells)
		{
			return WRange.FromCells((int)cells);
		}

		[LuaGlobal]
		public void SetWinState(Player player, string winState)
		{
			player.WinState = Enum<WinState>.Parse(winState);
		}

		[LuaGlobal]
		public void PlayRandomMusic()
		{
			if (!Game.Settings.Sound.MapMusic || !world.Map.Rules.InstalledMusic.Any())
				return;
			Game.ConnectionStateChanged += StopMusic;
			PlayMusic();
		}

		void PlayMusic()
		{
			var track = world.Map.Rules.InstalledMusic.Random(Game.CosmeticRandom);
			Sound.PlayMusicThen(track.Value, PlayMusic);
		}

		void StopMusic(OrderManager orderManager)
		{
			if (!orderManager.GameStarted)
			{
				Sound.StopMusic();
				Game.ConnectionStateChanged -= StopMusic;
			}
		}

		[LuaGlobal]
		public bool IsDead(Actor actor)
		{
			return actor.IsDead();
		}

		[LuaGlobal]
		public void PlayMovieFullscreen(string movie, Action onComplete)
		{
			Media.PlayFMVFullscreen(world, movie, onComplete);
		}

		[LuaGlobal]
		public void FlyToPos(Actor actor, WPos pos)
		{
			actor.QueueActivity(new Fly(actor, Target.FromPos(pos)));
		}

		[LuaGlobal]
		public void FlyAttackActor(Actor actor, Actor targetActor)
		{
			actor.QueueActivity(new FlyAttack(Target.FromActor(targetActor)));
		}

		[LuaGlobal]
		public void FlyAttackCell(Actor actor, CPos location)
		{
			actor.QueueActivity(new FlyAttack(Target.FromCell(actor.World, location)));
		}

		[LuaGlobal]
		public void HeliFlyToPos(Actor actor, WPos pos)
		{
			actor.QueueActivity(new HeliFly(actor, Target.FromPos(pos)));
		}

		[LuaGlobal]
		public void SetUnitStance(Actor actor, string stance)
		{
			var at = actor.TraitOrDefault<AutoTarget>();
			if (at != null)
				at.Stance = Enum<UnitStance>.Parse(stance);
		}

		[LuaGlobal]
		public bool RequiredUnitsAreDestroyed(Player player)
		{
			return player.HasNoRequiredUnits();
		}

		[LuaGlobal]
		public void AttackMove(Actor actor, CPos location, double nearEnough)
		{
			if (actor.HasTrait<AttackMove>())
				actor.QueueActivity(new AttackMove.AttackMoveActivity(actor, new Move.Move(location, (int)nearEnough)));
			else
				actor.QueueActivity(new Move.Move(location, (int)nearEnough));
		}

		[LuaGlobal]
		public int GetRandomInteger(double low, double high)
		{
			return world.SharedRandom.Next((int)low, (int)high);
		}

		[LuaGlobal]
		public CPos GetRandomCell()
		{
			return world.Map.ChooseRandomCell(world.SharedRandom);
		}

		[LuaGlobal]
		public CPos GetRandomEdgeCell()
		{
			return world.Map.ChooseRandomEdgeCell(world.SharedRandom);
		}

		[LuaGlobal]
		public Actor GetNamedActor(string actorName)
		{
			return sma.Actors[actorName];
		}

		[LuaGlobal]
		public bool IsNamedActor(Actor actor)
		{
			return actor.ActorID <= sma.LastMapActorID && actor.ActorID > sma.LastMapActorID - sma.Actors.Count;
		}

		[LuaGlobal]
		public IEnumerable<Actor> GetNamedActors()
		{
			return sma.Actors.Values;
		}

		[LuaGlobal]
		public Actor[] FindActorsInBox(WPos topLeft, WPos bottomRight)
		{
			return world.ActorMap.ActorsInBox(topLeft, bottomRight).ToArray();
		}

		[LuaGlobal]
		public Actor[] FindActorsInCircle(WPos location, WRange radius)
		{
			return world.FindActorsInCircle(location, radius).ToArray();
		}

		ClassicProductionQueue GetSharedQueueForCategory(Player player, string category)
		{
			return world.ActorsWithTrait<ClassicProductionQueue>()
				.Where(a => a.Actor.Owner == player && a.Trait.Info.Type == category)
				.Select(a => a.Trait).FirstOrDefault();
		}

		ClassicProductionQueue GetSharedQueueForUnit(Player player, string unit)
		{
			var ri = world.Map.Rules.Actors[unit];

			var bi = ri.Traits.GetOrDefault<BuildableInfo>();
			if (bi == null)
				return null;

			return bi.Queue.Select(q => GetSharedQueueForCategory(player, q)).FirstOrDefault();
		}

		[LuaGlobal]
		public void BuildWithSharedQueue(Player player, string unit, double amount)
		{
			var queue = GetSharedQueueForUnit(player, unit);

			if (queue != null)
				queue.ResolveOrder(queue.Actor, Order.StartProduction(queue.Actor, unit, (int)amount));
		}

		[LuaGlobal]
		public void BuildWithPerFactoryQueue(Actor factory, string unit, double amount)
		{
			var ri = world.Map.Rules.Actors[unit];

			var bi = ri.Traits.GetOrDefault<BuildableInfo>();
			if (bi == null)
				return;

			var queue = factory.TraitsImplementing<ProductionQueue>()
				.FirstOrDefault(q => q.Enabled);

			if (queue != null)
				queue.ResolveOrder(factory, Order.StartProduction(factory, unit, (int)amount));
		}

		[LuaGlobal]
		public bool SharedQueueIsBusy(Player player, string category)
		{
			var queue = GetSharedQueueForCategory(player, category);

			if (queue == null)
				return true;

			return queue.CurrentItem() != null;
		}

		[LuaGlobal]
		public bool PerFactoryQueueIsBusy(Actor factory)
		{
			var queue = factory.TraitsImplementing<ProductionQueue>()
				.FirstOrDefault(q => q.Enabled);

			if (queue == null)
				return true;

			return queue.CurrentItem() != null;
		}

		[LuaGlobal]
		public void Guard(Actor guard, Actor target)
		{
			if (target.HasTrait<Guardable>())
			{
				var gt = guard.TraitOrDefault<Guard>();

				if (gt != null)
					gt.GuardTarget(guard, Target.FromActor(target));
			}
		}

		[LuaGlobal]
		public IEnumerable<CPos> ExpandFootprint(LuaTable cells, bool allowDiagonal)
		{
			return Util.ExpandFootprint(cells.Values.Cast<CPos>(), allowDiagonal);
		}

		[LuaGlobal]
		public WPos CenterOfCell(CPos position)
		{
			return world.Map.CenterOfCell(position);
		}
	}
}
