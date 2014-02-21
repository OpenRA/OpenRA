#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Move;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Missions
{
	public static class MissionUtils
	{
		public static IEnumerable<Actor> FindAliveCombatantActorsInCircle(this World world, WPos location, WDist range)
		{
			return world.FindActorsInCircle(location, range)
				.Where(u => u.IsInWorld && u != world.WorldActor && !u.IsDead() && !u.Owner.NonCombatant);
		}

		public static IEnumerable<Actor> FindAliveCombatantActorsInBox(this World world, CPos a, CPos b)
		{
			return world.FindActorsInBox(a, b)
				.Where(u => u.IsInWorld && u != world.WorldActor && !u.IsDead() && !u.Owner.NonCombatant);
		}

		public static IEnumerable<Actor> FindAliveNonCombatantActorsInCircle(this World world, WPos location, WDist range)
		{
			return world.FindActorsInCircle(location, range)
				.Where(u => u.IsInWorld && u != world.WorldActor && !u.IsDead() && u.Owner.NonCombatant);
		}

		public static Actor ExtractUnitWithChinook(World world, Player owner, Actor unit, CPos entry, CPos lz, CPos exit)
		{
			var chinook = world.CreateActor("tran", new TypeDictionary { new OwnerInit(owner), new LocationInit(entry) });
			chinook.QueueActivity(new HeliFly(chinook, Target.FromCell(lz)));
			chinook.QueueActivity(new Turn(0));
			chinook.QueueActivity(new HeliLand(true));
			chinook.QueueActivity(new WaitFor(() => chinook.Trait<Cargo>().Passengers.Contains(unit)));
			chinook.QueueActivity(new Wait(150));
			chinook.QueueActivity(new HeliFly(chinook, Target.FromCell(exit)));
			chinook.QueueActivity(new RemoveSelf());
			return chinook;
		}

		public static Pair<Actor, Actor> InsertUnitWithChinook(World world, Player owner, string unitName, CPos entry, CPos lz, CPos exit, Action<Actor> afterUnload)
		{
			var unit = world.CreateActor(false, unitName, new TypeDictionary { new OwnerInit(owner) });
			var chinook = world.CreateActor("tran", new TypeDictionary { new OwnerInit(owner), new LocationInit(entry) });
			chinook.Trait<Cargo>().Load(chinook, unit);
			chinook.QueueActivity(new HeliFly(chinook, Target.FromCell(lz)));
			chinook.QueueActivity(new Turn(0));
			chinook.QueueActivity(new HeliLand(true));
			chinook.QueueActivity(new UnloadCargo(chinook, true));
			chinook.QueueActivity(new CallFunc(() => afterUnload(unit)));
			chinook.QueueActivity(new Wait(150));
			chinook.QueueActivity(new HeliFly(chinook, Target.FromCell(exit)));
			chinook.QueueActivity(new RemoveSelf());
			return Pair.New(chinook, unit);
		}

		public static void Paradrop(World world, Player owner, IEnumerable<string> units, CPos entry, CPos location)
		{
			var altitude = Rules.Info["badr"].Traits.Get<PlaneInfo>().CruiseAltitude;
			var badger = world.CreateActor("badr", new TypeDictionary
			{
				new CenterPositionInit(entry.CenterPosition + new WVec(WDist.Zero, WDist.Zero, altitude)),
				new OwnerInit(owner),
				new FacingInit(Util.GetFacing(location - entry, 0))
			});

			badger.QueueActivity(new FlyAttack(Target.FromCell(location)));
			badger.Trait<ParaDrop>().SetLZ(location);
			var cargo = badger.Trait<Cargo>();
			foreach (var unit in units)
			{
				cargo.Load(badger, world.CreateActor(false, unit, new TypeDictionary { new OwnerInit(owner) }));
			}
		}

		public static void Parabomb(World world, Player owner, CPos entry, CPos location)
		{
			var altitude = Rules.Info["badr.bomber"].Traits.Get<PlaneInfo>().CruiseAltitude;
			var badger = world.CreateActor("badr.bomber", new TypeDictionary
			{
				new CenterPositionInit(entry.CenterPosition + new WVec(WDist.Zero, WDist.Zero, altitude)),
				new OwnerInit(owner),
				new FacingInit(Util.GetFacing(location - entry, 0))
			});

			badger.Trait<AttackBomber>().SetTarget(location.CenterPosition);
			badger.QueueActivity(new Fly(badger, Target.FromCell(location)));
			badger.QueueActivity(new FlyOffMap());
			badger.QueueActivity(new RemoveSelf());
		}

		public static bool AreaSecuredWithUnits(World world, Player player, WPos location, WDist range)
		{
			var units = world.FindAliveCombatantActorsInCircle(location, range).Where(a => a.HasTrait<IPositionable>());
			return units.Any() && units.All(a => a.Owner == player);
		}

		public static IEnumerable<ProductionQueue> FindQueues(World world, Player player, string category)
		{
			return world.ActorsWithTrait<ProductionQueue>()
				.Where(a => a.Actor.Owner == player && a.Trait.Info.Type == category)
				.Select(a => a.Trait);
		}

		public static void StartProduction(World world, Player player, string category, string item)
		{
			var queue = FindQueues(world, player, category).FirstOrDefault(q => q.CurrentItem() == null);
			if (queue != null)
				queue.ResolveOrder(queue.self, Order.StartProduction(queue.self, item, 1));
		}

		public static Actor UnitContaining(this World world, Actor actor)
		{
			return world.Actors.FirstOrDefault(a => a.HasTrait<Cargo>() && a.Trait<Cargo>().Passengers.Contains(actor));
		}

		public static void PlayMissionMusic()
		{
			if (!Rules.InstalledMusic.Any() || !Game.Settings.Sound.MapMusic)
				return;
			Game.ConnectionStateChanged += StopMusic;
			PlayMusic();
		}

		static void PlayMusic()
		{
			var track = Rules.InstalledMusic.Random(Game.CosmeticRandom);
			Sound.PlayMusicThen(track.Value, PlayMusic);
		}

		static void StopMusic(OrderManager orderManager)
		{
			if (!orderManager.GameStarted)
			{
				Sound.StopMusic();
				Game.ConnectionStateChanged -= StopMusic;
			}
		}

		public static void CoopMissionAccomplished(World world, string text, params Player[] players)
		{
			if (players.First().WinState != WinState.Undefined)
				return;

			foreach (var player in players)
				player.WinState = WinState.Won;

			if (text != null)
				Game.AddChatLine(Color.Blue, "Mission accomplished", text);

			Sound.Play("misnwon1.aud");
		}

		public static void CoopMissionFailed(World world, string text, params Player[] players)
		{
			if (players.First().WinState != WinState.Undefined)
				return;

			foreach (var player in players)
			{
				player.WinState = WinState.Lost;
				foreach (var actor in world.Actors.Where(a => a.IsInWorld && a.Owner == player && !a.IsDead()))
				{
					actor.Kill(actor);
				}
			}
			
			if (text != null)
				Game.AddChatLine(Color.Red, "Mission failed", text);

			Sound.Play("misnlst1.aud");
		}

		public static Actor CreateActor(this World world, bool addToWorld, string name, Player owner, CPos? location, int? facing)
		{
			var td = new TypeDictionary { new OwnerInit(owner) };
			if (location.HasValue)
				td.Add(new LocationInit(location.Value));
			if (facing.HasValue)
				td.Add(new FacingInit(facing.Value));
			return world.CreateActor(addToWorld, name, td);
		}

		public static Actor CreateActor(this World world, string name, Player owner, CPos? location, int? facing)
		{
			return CreateActor(world, true, name, owner, location, facing);
		}

		public static void CapOre(Player player)
		{
			var res = player.PlayerActor.Trait<PlayerResources>();
			if (res.Ore > res.OreCapacity * 0.8)
				res.Ore = (int)(res.OreCapacity * 0.8);
		}

		public static void AttackNearestLandActor(bool queued, Actor self, Player enemyPlayer)
		{
			var enemies = self.World.Actors.Where(u => u.AppearsHostileTo(self) && u.Owner == enemyPlayer
					&& ((u.HasTrait<Building>() && !u.HasTrait<Wall>()) || (u.HasTrait<Mobile>() && !u.HasTrait<Aircraft>())) && u.IsInWorld && !u.IsDead());

			var enemy = enemies.ClosestTo(self);
			if (enemy != null)
				self.QueueActivity(queued, new AttackMove.AttackMoveActivity(self, new Attack(Target.FromActor(enemy), WDist.FromCells(3))));
		}
	}

	class TransformedAction : INotifyTransformed
	{
		Action<Actor> a;

		public TransformedAction(Action<Actor> a)
		{
			this.a = a;
		}

		public void OnTransformed(Actor toActor)
		{
			a(toActor);
		}
	}

	class InfiltrateAction : IAcceptInfiltrator
	{
		Action<Actor> a;

		public InfiltrateAction(Action<Actor> a)
		{
			this.a = a;
		}

		public void OnInfiltrate(Actor self, Actor spy)
		{
			a(spy);
		}
	}
}
