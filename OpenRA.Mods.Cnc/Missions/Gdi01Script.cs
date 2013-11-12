#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Move;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Missions
{
	class Gdi01ScriptInfo : TraitInfo<Gdi01Script> { }

	class Gdi01Script : IWorldLoaded, ITick
	{
		Dictionary<string, Actor> actors;
		Dictionary<string, Player> players;

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			players = w.Players.ToDictionary(p => p.InternalName);
			actors = w.WorldActor.Trait<SpawnMapActors>().Actors;
			var b = w.Map.Bounds;
			wr.Viewport.Center(new CPos(b.Left + b.Width / 2, b.Top + b.Height / 2).CenterPosition);

			Action afterFMV = () =>
			{
				Sound.PlayMusic(Rules.Music["aoi"]);
				started = true;
			};
			Game.RunAfterDelay(0, () => Media.PlayFMVFullscreen(w, "gdi1.vqa", () =>
			                            Media.PlayFMVFullscreen(w, "landing.vqa", afterFMV)));
		}

		public void OnVictory(World w)
		{
			Action afterFMV = () =>
			{
				players["GoodGuy"].WinState = WinState.Won;
				started = false;
				Sound.StopMusic();
				Sound.PlayToPlayer(players["GoodGuy"], "accom1.aud");
			};
			Game.RunAfterDelay(0, () => Media.PlayFMVFullscreen(w, "consyard.vqa", afterFMV));
		}

		public void OnLose(World w)
		{
			Action afterFMV = () =>
			{
				players["GoodGuy"].WinState = WinState.Lost;
				started = false;
				Sound.StopMusic();
				Sound.PlayToPlayer(players["GoodGuy"], "fail1.aud");
			};
			Game.RunAfterDelay(0, () => Media.PlayFMVFullscreen(w, "gameover.vqa", afterFMV));
		}

		int ticks = 0;
		bool started = false;

		int lastBadCount = -1;
		public void Tick(Actor self)
		{
			if (!started)
				return;

			if (ticks == 0)
			{
				SetGunboatPath();
				self.World.AddFrameEndTask(w =>
				{
					// Initial Nod reinforcements
					foreach (var i in new[] { "e1", "e1" })
					{
						var a = self.World.CreateActor(i.ToLowerInvariant(), new TypeDictionary
						{
							new OwnerInit(players["BadGuy"]),
							new FacingInit(0),
							new LocationInit(actors["nod0"].Location),
						});
						var mobile = a.Trait<Mobile>();
						a.QueueActivity(mobile.MoveTo(actors["nod1"].Location, 2));
						a.QueueActivity(mobile.MoveTo(actors["nod2"].Location, 2));
						a.QueueActivity(mobile.MoveTo(actors["nod3"].Location, 2));

						// TODO: Queue hunt order
					}
				});
			}

			// GoodGuy win conditions
			// BadGuy is dead
			var badcount = self.World.Actors.Count(a => a != a.Owner.PlayerActor &&
											       a.Owner == players["BadGuy"] && !a.IsDead());
			if (badcount != lastBadCount)
			{
				Game.Debug("{0} badguys remain".F(badcount));
				lastBadCount = badcount;

				if (badcount == 0)
					OnVictory(self.World);
			}

			// GoodGuy lose conditions: MCV/cyard must survive
			var hasAnything = self.World.ActorsWithTrait<MustBeDestroyed>()
				.Any(a => a.Actor.Owner == players["GoodGuy"]);
			if (!hasAnything)
				OnLose(self.World);

			// GoodGuy reinforcements
			if (ticks == 25 * 5)
			{
				ReinforceFromSea(self.World,
								 actors["lstStart"].Location,
								 actors["lstEnd"].Location,
								 new CPos(53, 53),
								 new string[] { "e1", "e1", "e1" },
								 players["GoodGuy"]);
			}

			if (ticks == 25 * 15)
			{
				ReinforceFromSea(self.World,
								 actors["lstStart"].Location,
								 actors["lstEnd"].Location,
								 new CPos(53, 53),
								 new string[] { "e1", "e1", "e1" },
								 players["GoodGuy"]);
			}

			if (ticks == 25 * 30)
			{
				ReinforceFromSea(self.World,
								 actors["lstStart"].Location,
								 actors["lstEnd"].Location,
								 new CPos(53, 53),
								 new string[] { "jeep" },
								 players["GoodGuy"]);
			}

			if (ticks == 25 * 60)
			{
				ReinforceFromSea(self.World,
								 actors["lstStart"].Location,
								 actors["lstEnd"].Location,
								 new CPos(53, 53),
								 new string[] { "jeep" },
								 players["GoodGuy"]);
			}

			ticks++;
		}

		void SetGunboatPath()
		{
			var self = actors["Gunboat"];
			var mobile = self.Trait<Mobile>();
			self.Trait<AutoTarget>().stance = UnitStance.AttackAnything; // TODO: this is ignored
			self.QueueActivity(mobile.ScriptedMove(actors["gunboatLeft"].Location));
			self.QueueActivity(mobile.ScriptedMove(actors["gunboatRight"].Location));
			self.QueueActivity(new CallFunc(() => SetGunboatPath()));
		}

		void ReinforceFromSea(World world, CPos startPos, CPos endPos, CPos unload, string[] items, Player player)
		{
			world.AddFrameEndTask(w =>
			{
				Sound.PlayToPlayer(w.LocalPlayer, "reinfor1.aud");

				var a = w.CreateActor("lst", new TypeDictionary
				{
					new LocationInit(startPos),
					new OwnerInit(player),
					new FacingInit(0),
				});

				var mobile = a.Trait<Mobile>();
				var cargo = a.Trait<Cargo>();
				foreach (var i in items)
					cargo.Load(a, world.CreateActor(false, i.ToLowerInvariant(), new TypeDictionary
					{
						new OwnerInit(player),
						new FacingInit(0),
					}));

				a.CancelActivity();
				a.QueueActivity(mobile.ScriptedMove(endPos));
				a.QueueActivity(new CallFunc(() =>
				{
					while (!cargo.IsEmpty(a))
					{
						var b = cargo.Unload(a);
						world.AddFrameEndTask(w2 =>
						{
							if (b.Destroyed) return;
							w2.Add(b);
							b.TraitsImplementing<IPositionable>().FirstOrDefault().SetPosition(b, a.Location);
							b.QueueActivity(mobile.MoveTo(unload, 2));
						});
					}
				}));
				a.QueueActivity(new Wait(25));
				a.QueueActivity(mobile.ScriptedMove(startPos));
				a.QueueActivity(new RemoveSelf());
			});
		}
	}
}
