--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
FootprintTrigger1 = { CPos.New(77, 85), CPos.New(77, 86), CPos.New(77, 87), CPos.New(77, 88) }
Trigger1Rifles = { Rifle1, Rifle2, Rifle3, Rifle4 }
DoomedHeliPath = { DoomedHeliEntry.Location, DoomedHeli.Location }
FootprintTrigger2 = { CPos.New(90, 82), CPos.New(91, 82), CPos.New(93, 82), CPos.New(94, 82), CPos.New(95, 82), CPos.New(96, 82), CPos.New(97, 82), CPos.New(98, 82), CPos.New(99, 82), CPos.New(102, 82) }
Grenadiers = { "e2", "e2", "e2", "e2", "e2" }
FootprintTrigger3 = { CPos.New(88, 77), CPos.New(88, 76), CPos.New(91, 75), CPos.New(92, 75), CPos.New(93, 75), CPos.New(94, 75), CPos.New(95, 75), CPos.New(96, 75), CPos.New(97, 75), CPos.New(98, 75), CPos.New(99, 75), CPos.New(100, 75) }
Trigger3Team = { Gren1, Gren2, Gren3, Gren4, Gren5, VillageMammoth }
FootprintTrigger4 = { CPos.New(94, 60), CPos.New(95, 60), CPos.New(96, 60), CPos.New(97, 60), CPos.New(98, 60), CPos.New(99, 60), CPos.New(100, 60), CPos.New(101, 60), CPos.New(102, 60), CPos.New(103, 60), CPos.New(104, 60) }
CivilianSquad1 = { "c1", "c2", "c3", "c4", "c5" }
CivilianSquad2 = { "c6", "c7", "c8", "c9", "c10" }
FootprintTrigger5 = { CPos.New(99, 53), CPos.New(100, 53), CPos.New(101, 53), CPos.New(102, 53), CPos.New(103, 53), CPos.New(104, 53) }
Trigger5Team = { TeamFive1, TeamFive2, TeamFive3, TeamFive4, TeamFive5, TeamFive6, TeamFive7 }
TentTeam = { "e1", "e1", "e1", "e1", "e3", "e3" }
Doggos = { "dog", "dog", "dog" }
SovietAttackers = { "3tnk", "e2", "e2", "e2", "v2rl" }
MigEntryPath = { BaseAttackersSpawn, GuideSpawn }
FootprintTrigger6 = { CPos.New(81, 57), CPos.New(81, 58), CPos.New(81, 59), CPos.New(81, 60), CPos.New(81, 61) }
FootprintTrigger7 = { CPos.New(73, 58), CPos.New(73, 59), CPos.New(73, 60), CPos.New(73, 61) }
FootprintTrigger8 = { CPos.New(51, 83), CPos.New(51, 84), CPos.New(51, 85), CPos.New(51, 86), CPos.New(51, 87), CPos.New(51, 88) }
FootprintTrigger9 = { CPos.New(28, 77), CPos.New(29, 77), CPos.New(30, 77),  CPos.New(31, 77),  CPos.New(32, 77), CPos.New(33, 77), CPos.New(34, 77), CPos.New(35, 77), CPos.New(36, 77), CPos.New(37, 77), CPos.New(38, 77) }
BridgeMammoths = { BridgeMammoth1, BridgeMammoth2 }
FootprintTrigger10 = { CPos.New(24, 83), CPos.New(24, 84), CPos.New(24, 85) }
FootprintTrigger11 = { CPos.New(20, 65), CPos.New(21, 65), CPos.New(22, 65) }
SovBase = { SovFact, SovPower1, SovPower2, SovRax, SovWarFactory, SovFlame1, SovFlame2, SovTesla }
SovBaseTeam = { SovBaseTeam1, SovBaseTeam2, SovBaseTeam3, SovBaseTeam4, SovBaseTeam5, SovBaseTeam6 }
RaxTeam = { "e1", "e2", "e2", "e4", "e4", "shok" }
FootprintTrigger12 = { CPos.New(35, 39), CPos.New(35, 40), CPos.New(35, 41), CPos.New(35, 42) }
ExtractionHelicopterType = "tran"
ExtractionPath = { ChinookEntry.Location, ExtractionPoint.Location }

lstReinforcements =
{
	first =
	{
		actors = { "2tnk", "2tnk", "2tnk", "2tnk", "2tnk" },
		entryPath = { BoatSpawn.Location, BoatUnload1.Location },
		exitPath = { BoatSpawn.Location }
	},
	second =
	{
		actors = { "1tnk", "1tnk", "2tnk", "2tnk", "2tnk" },
		entryPath = { BoatSpawn.Location, BoatUnload2.Location },
		exitPath = { BoatSpawn.Location }
	}
}

IdleHunt = function(actor) if not actor.IsDead then Trigger.OnIdle(actor, actor.Hunt) end end

VIPs = { }
MissionStart = function()
	FlareBoy.Move(LightFlare.Location)

	Trigger.OnEnteredFootprint({ LightFlare.Location }, function(actor, id)
		if actor.Owner == England then
			Trigger.RemoveFootprintTrigger(id)
			local insertionFlare = Actor.Create("flare", true, { Owner = Allies, Location = LightFlare.Location })
			Trigger.AfterDelay(DateTime.Seconds(2), function()
				FlareBoy.AttackMove(FlareBoyAttack.Location)
				if Map.LobbyOption("difficulty") == "normal" then
					local normalDrop = InsertionDrop.TargetParatroopers(InsertionPoint.CenterPosition, Angle.New(892))
					Utils.Do(normalDrop, function(a)
						Trigger.OnPassengerExited(a, function(t,p)
							VIPs[#VIPs + 1] = p
							FailTrigger()
						end)
					end)
				else
					local hardDrop = InsertionDropHard.TargetParatroopers(InsertionPoint.CenterPosition, Angle.New(892))
					Utils.Do(hardDrop, function(a)
						Trigger.OnPassengerExited(a, function(t,p)
							VIPs[#VIPs + 1] = p
							FailTrigger()
						end)
					end)		
					Trigger.AfterDelay(DateTime.Seconds(6), function()
						Media.DisplayMessage("Commander, there are several civilians in the area.\nWe'll need you to call out targets.", "Tanya")
					end)
				end
			end)

			Trigger.AfterDelay(DateTime.Seconds(20), function()
				insertionFlare.Destroy()
			end)
		end
	end)
end

FailTrigger = function()
	Trigger.OnAnyKilled(VIPs, function()
		Allies.MarkFailedObjective(ProtectVIPs)
	end)
end	

FootprintTriggers = function()
	local foot1Triggered
	Trigger.OnEnteredFootprint(FootprintTrigger1, function(actor, id)
		if actor.Owner == Allies and not foot1Triggered then
			Trigger.RemoveFootprintTrigger(id)
			foot1Triggered = true

			local trig1cam = Actor.Create("camera", true, { Owner = Allies, Location = Trigger1Cam.Location })
			Trigger.AfterDelay(DateTime.Seconds(10), function()
				trig1cam.Destroy()
			end)

			Utils.Do(Trigger1Rifles, function(actor)
				if not actor.IsDead then
					actor.AttackMove(Trigger1Move.Location)
				end
			end)

			DoomedHeli = Reinforcements.ReinforceWithTransport(England, ExtractionHelicopterType, nil, DoomedHeliPath)[1]
		end
	end)

	local foot2Triggered
	Trigger.OnEnteredFootprint(FootprintTrigger2, function(actor, id)
		if actor.Owner == Allies and not foot2Triggered then
			Trigger.RemoveFootprintTrigger(id)
			foot2Triggered = true

			local drop1 = RifleDropS.TargetParatroopers(VillageParadrop.CenterPosition, Angle.SouthWest)
			Utils.Do(drop1, function(a)
				Trigger.OnPassengerExited(a, function(t, p)
					IdleHunt(p)
				end)
			end)

			local grens = Reinforcements.Reinforce(USSR, Grenadiers, { GrenEntry.Location }, 0)
			Utils.Do(grens, IdleHunt)
		end
	end)

	local foot3Triggered
	Trigger.OnEnteredFootprint(FootprintTrigger3, function(actor, id)
		if actor.Owner == Allies and not foot3Triggered then
			Trigger.RemoveFootprintTrigger(id)
			foot3Triggered = true

			Trig3House.Owner = Civilians
			local camera3 = Actor.Create("camera", true, { Owner = Allies, Location = Trigger3Cam.Location })
			Trigger.AfterDelay(DateTime.Seconds(10), function()
				camera3.Destroy()
			end)

			if not GuideHut.IsDead then
				local guide = Actor.Create("c6", true, { Owner = England, Location = GuideSpawn.Location })
				guide.Move(SafePath1.Location)
				guide.Move(SafePath2.Location)
				guide.Move(CivilianRally.Location)
			end

			Utils.Do(Trigger3Team, function(actor)
				if not actor.IsDead then
					actor.AttackMove(GuideSpawn.Location)
					IdleHunt(actor)
				end
			end)
		end
	end)

	local foot4Triggered
	Trigger.OnEnteredFootprint(FootprintTrigger4, function(actor, id)
		if actor.Owner == Allies and not foot4Triggered then
			Trigger.RemoveFootprintTrigger(id)
			foot4Triggered = true

			Trig4House.Owner = Civilians
			Reinforcements.Reinforce(England, CivilianSquad1, { CivFlee1.Location, CivilianRally.Location }, 0)
			Reinforcements.Reinforce(England, CivilianSquad2, { CivFlee2.Location, CivilianRally.Location }, 0)
		end
	end)

	local foot5Triggered
	Trigger.OnEnteredFootprint(FootprintTrigger5, function(actor, id)
		if actor.Owner == Allies and not foot5Triggered then
			Trigger.RemoveFootprintTrigger(id)
			foot5Triggered = true

			Media.PlaySoundNotification(Allies, "AlertBleep")
			Media.DisplayMessage("Alfa Niner this is Lima One Six. Be advised, Soviet aircraft and armor moving into your AO.", "Headquarters")
			Utils.Do(Trigger5Team, function(actor)
				if not actor.IsDead then
					actor.AttackMove(TacticalNuke1.Location)
				end
			end)

			SendMig(MigEntryPath)
			local barrelcam1 = Actor.Create("camera", true, { Owner = USSR, Location = TacticalNuke1.Location })
			local barrelcam2 = Actor.Create("camera", true, { Owner = USSR, Location = TacticalNuke2.Location })
			local wave1 = Reinforcements.Reinforce(USSR, SovietAttackers, { BaseAttackersSpawn.Location, SovietAttack.Location })
			Utils.Do(wave1, IdleHunt)
			local drop2 = RifleDropS.TargetParatroopers(SovietAttack.CenterPosition, Angle.East)
			Utils.Do(drop2, function(a)
				Trigger.OnPassengerExited(a, function(t, p)
					IdleHunt(p)
				end)
			end)

			Trigger.AfterDelay(DateTime.Seconds(20), function()
				Media.PlaySoundNotification(Allies, "AlertBuzzer")
				Media.DisplayMessage("Extraction point is compromised. Evacuate the base!", "Headquarters")
				local defenders = Reinforcements.Reinforce(England, TentTeam, { Tent.Location, TentMove.Location }, 0)
				Utils.Do(defenders, IdleHunt)
				if Map.LobbyOption("difficulty") == "hard" then
					Trigger.AfterDelay(DateTime.Seconds(30), function()
						local wave2 = Reinforcements.Reinforce(USSR, SovietAttackers, { BaseAttackersSpawn.Location, SovietAttack.Location })
						Utils.Do(wave2, IdleHunt)
					end)
				end
			end)

			Trigger.AfterDelay(DateTime.Seconds(35), function()
				local dogs = Reinforcements.Reinforce(USSR, Doggos, { GrenEntry.Location }, 0)
				Utils.Do(dogs, IdleHunt)
				Media.PlaySpeechNotification(Allies, "AbombLaunchDetected")
				local proxy = Actor.Create("powerproxy.parabombs", false, { Owner = USSR })
				proxy.TargetAirstrike(TacticalNuke1.CenterPosition, Angle.NorthWest)
				Trigger.AfterDelay(DateTime.Seconds(5), function()
					Media.PlaySpeechNotification(Allies, "AbombLaunchDetected")
					proxy.TargetAirstrike(TacticalNuke2.CenterPosition, Angle.NorthWest)
				end)
				proxy.Destroy()
			end)

			Trigger.AfterDelay(DateTime.Seconds(50), function()
				Media.DisplayMessage("We've set up a new extraction point to the Northwest.", "Headquarters")
			end)
		end
	end)

	local foot6Triggered
	Trigger.OnEnteredFootprint(FootprintTrigger6, function(actor, id)
		if actor.Owner == Allies and not foot6Triggered then
			Trigger.RemoveFootprintTrigger(id)
			foot6Triggered = true

			local reinforcement = lstReinforcements.first
			Media.PlaySpeechNotification(Allies, "ReinforcementsArrived")
			Reinforcements.ReinforceWithTransport(Allies, "lst.reinforcement", reinforcement.actors, reinforcement.entryPath, reinforcement.exitPath)
		end
	end)

	local foot7Triggered
	Trigger.OnEnteredFootprint(FootprintTrigger7, function(actor, id)
		if actor.Owner == Allies and not foot7Triggered then
			Trigger.RemoveFootprintTrigger(id)
			foot7Triggered = true

			local drop3 = RifleDropS.TargetParatroopers(TacticalNuke3.CenterPosition, Angle.West)
			Utils.Do(drop3, function(a)
				Trigger.OnPassengerExited(a, function(t, p)
					IdleHunt(p)
				end)
			end)

			local trig7camera1 = Actor.Create("camera", true, { Owner = Allies, Location = MammothCam.Location })
			local trig7camera2 = Actor.Create("camera", true, { Owner = Allies, Location = TacticalNuke3.Location })
			Trigger.AfterDelay(DateTime.Seconds(30), function()
				trig7camera1.Destroy()
			end)

			Trigger.AfterDelay(DateTime.Seconds(20), function()
				Media.PlaySpeechNotification(Allies, "AbombLaunchDetected")
				local proxy = Actor.Create("powerproxy.parabombs", false, { Owner = USSR })
				proxy.TargetAirstrike(TacticalNuke3.CenterPosition, Angle.SouthWest)
				proxy.Destroy()
			end)

			Trigger.AfterDelay(DateTime.Seconds(26), function()
				Reinforcements.Reinforce(England, CivilianSquad1, { House1.Location, TacticalNuke3.Location }, 0)
				Reinforcements.Reinforce(England, CivilianSquad2, { House2.Location, TacticalNuke3.Location }, 0)
				Reinforcements.Reinforce(England, CivilianSquad1, { House3.Location, TacticalNuke3.Location }, 0)
				Reinforcements.Reinforce(England, CivilianSquad2, { House4.Location, TacticalNuke3.Location }, 0)
			end)

			Trigger.AfterDelay(DateTime.Seconds(15), function()
				trig7camera2.Destroy()
			end)
		end
	end)

	local foot8Triggered
	Trigger.OnEnteredFootprint(FootprintTrigger8, function(actor, id)
		if actor.Owner == Allies and not foot8Triggered then
			Trigger.RemoveFootprintTrigger(id)
			foot8Triggered = true

			local trig8camera = Actor.Create("camera", true, { Owner = Allies, Location = TeslaCam.Location })
			Trigger.AfterDelay(DateTime.Seconds(10), function()
				trig8camera.Destroy()
			end)

			Media.PlaySpeechNotification(Allies, "ReinforcementsArrived")
			RifleDropA.TargetParatroopers(TeslaDrop.CenterPosition, Angle.New(124))
		end
	end)

	local foot9Triggered
	Trigger.OnEnteredFootprint(FootprintTrigger9, function(actor, id)
		if actor.Owner == Allies and not foot9Triggered then
			Trigger.RemoveFootprintTrigger(id)
			foot9Triggered = true

			local trig9camera = Actor.Create("camera", true, { Owner = Allies, Location = BridgeCam.Location })
			Trigger.AfterDelay(DateTime.Seconds(10), function()
				trig9camera.Destroy()
			end)

			Utils.Do(BridgeMammoths, function(actor)
				actor.AttackMove(MammysGo.Location)
			end)
		end
	end)

	local foot10Triggered
	Trigger.OnEnteredFootprint(FootprintTrigger10, function(actor, id)
		if actor.Owner == Allies and not foot10Triggered then
			Trigger.RemoveFootprintTrigger(id)
			foot10Triggered = true

			local trig10camera = Actor.Create("camera", true, { Owner = Allies, Location = TruckCam.Location })
			Trigger.AfterDelay(DateTime.Seconds(10), function()
				trig10camera.Destroy()
			end)

			Media.PlaySpeechNotification(Allies, "SignalFlareNorth")
			Actor.Create("camera", true, { Owner = Allies, Location = ExtractionPoint.Location })
			SendExtractionHelicopter()

			HealCrateTruck.Move(TruckGo.Location)
		end
	end)

	local foot11Triggered
	Trigger.OnEnteredFootprint(FootprintTrigger11, function(actor, id)
		if actor.Owner == Allies and not foot11Triggered then
			Trigger.RemoveFootprintTrigger(id)
			foot11Triggered = true

			local trig11camera = Actor.Create("camera", true, { Owner = Allies, Location = SovBaseCam.Location })
			Trigger.AfterDelay(DateTime.Seconds(10), function()
				trig11camera.Destroy()
			end)

			local reinforcement = lstReinforcements.second
			Media.PlaySpeechNotification(Allies, "ReinforcementsArrived")
			Reinforcements.ReinforceWithTransport(Allies, "lst.reinforcement", reinforcement.actors, reinforcement.entryPath, reinforcement.exitPath)
		end
	end)

	local foot12Triggered
	Trigger.OnEnteredFootprint(FootprintTrigger12, function(actor, id)
		if (actor.Type == "gnrl" or actor.Type == "gnrl.noautotarget") and not foot12Triggered then
			Trigger.RemoveFootprintTrigger(id)
			foot12Triggered = true

			Media.PlaySoundNotification(Allies, "AlertBleep")
			Media.DisplayMessage("Stalin will pay for what he has done today!\nI will bury him with my own hands!", "Stavros")
		end
	end)
end

SetupTriggers = function()	
	Utils.Do(USSR.GetGroundAttackers(), function(unit)
		Trigger.OnDamaged(unit, function() IdleHunt(unit) end)
	end)

	Trigger.OnKilled(BridgeBarrel1, function()
		local bridge = Utils.Where(Map.ActorsInWorld, function(actor) return actor.Type == "bridge2" end)[1]
		if not bridge.IsDead then
			bridge.Kill()
		end
	end)

	Trigger.OnKilled(BridgeBarrel2, function()
		local bridgepart1 = Utils.Where(Map.ActorsInWorld, function(actor) return actor.Type == "br2" end)[1]
		local bridgepart2 = Utils.Where(Map.ActorsInWorld, function(actor) return actor.Type == "br3" end)[1]
		if not bridgepart1.IsDead then
			bridgepart1.Kill()
		end
		if not bridgepart2.IsDead then
			bridgepart2.Kill()
		end
	end)

	local trukEscaped
	Trigger.OnEnteredFootprint({ TruckGo.Location }, function(actor, id)
		if actor.Type == "truk" and not trukEscaped then
			Trigger.RemoveFootprintTrigger(id)
			trukEscaped = true
			actor.Destroy()
		end
	end)
end

ChurchAttack = function()
	if not ChurchDamaged then
		local churchPanicTeam = Reinforcements.Reinforce(England, CivilianSquad1, { ChurchSpawn.Location }, 0)
		Utils.Do(churchPanicTeam, function(a)
			a.Move(a.Location + CVec.New(-1,-1))
			a.Panic()
		end)
	end
	ChurchDamaged = true
end

SendMig = function(waypoints)
	local MigEntryPath = { waypoints[1].Location, waypoints[2].Location }
	local Mig1 = Reinforcements.Reinforce(USSR, { "mig" }, MigEntryPath)
	if not BridgeBarrel1.IsDead then
		Utils.Do(Mig1, function(mig)
			mig.Attack(BridgeBarrel1)
		end)
	end

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		local Mig2 = Reinforcements.Reinforce(USSR, { "mig" }, MigEntryPath)
		local Mig3 = Reinforcements.Reinforce(USSR, { "mig" }, MigEntryPath)
		if not CivBarrel.IsDead then
			Utils.Do(Mig2, function(mig)
				mig.Attack(CivBarrel)
			end)
		end
		if not DoomedHeli.IsDead then
			Utils.Do(Mig3, function(mig)
				mig.Attack(DoomedHeli)
			end)
		end
	end)
end

SovBaseAttack = function()
	if not BaseDamaged then
		local drop4 = RifleDropS.TargetParatroopers(SovBaseDrop.CenterPosition, Angle.East)
		Utils.Do(drop4, function(a)
			Trigger.OnPassengerExited(a, function(t, p)
				IdleHunt(p)
			end)
		end)

		Utils.Do(SovBaseTeam, function(actor)
			if not actor.IsDead then
				IdleHunt(actor)
			end
		end)

		if Map.LobbyOption("difficulty") == "hard" then
			local barracksTeam = Reinforcements.Reinforce(USSR, RaxTeam, { SovRaxSpawn.Location, SovBaseCam.Location }, 0)
			Utils.Do(barracksTeam, IdleHunt)
		end
	end
	BaseDamaged = true
end

ExtractUnits = function(extractionUnit, pos, after)
	if extractionUnit.IsDead or not extractionUnit.HasPassengers then
		return
	end

	extractionUnit.Move(pos)
	extractionUnit.Destroy()

	Trigger.OnRemovedFromWorld(extractionUnit, after)
end

SendExtractionHelicopter = function()
	ExtractionHeli = Reinforcements.ReinforceWithTransport(Allies, ExtractionHelicopterType, nil, ExtractionPath)[1]
	local exitPos = CPos.New(ExtractionPath[1].X, ExtractionPath[2].Y)

	Trigger.OnKilled(ExtractionHeli, function() USSR.MarkCompletedObjective(SovietObj) end)
	Trigger.OnAllRemovedFromWorld(VIPs, function()
		ExtractUnits(ExtractionHeli, exitPos, function()
			Allies.MarkCompletedObjective(ProtectVIPs)
			Allies.MarkCompletedObjective(ExtractStavros)
		end)
	end)
end

WorldLoaded = function()
	Allies = Player.GetPlayer("Allies")
	USSR = Player.GetPlayer("USSR")
	BadGuy = Player.GetPlayer("BadGuy")
	England = Player.GetPlayer("England")
	Civilians = Player.GetPlayer("GreekCivilians")

	Trigger.OnObjectiveAdded(Allies, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	SovietObj = USSR.AddObjective("Kill Stavros.")
	ProtectVIPs = Allies.AddObjective("Keep Stavros and Tanya alive.")
	ExtractStavros = Allies.AddObjective("Get Stavros and Tanya to the extraction helicopter.")

	Trigger.OnObjectiveCompleted(Allies, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(Allies, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerLost(Allies, function()
		Media.PlaySpeechNotification(Allies, "Lose")
	end)
	Trigger.OnPlayerWon(Allies, function()
		Media.PlaySpeechNotification(Allies, "Win")
	end)

	InsertionDrop = Actor.Create("insertiondrop", false, { Owner = Allies })
	InsertionDropHard = Actor.Create("insertiondrophard", false, { Owner = Allies })
	RifleDropA = Actor.Create("rifledrop", false, { Owner = Allies })
	RifleDropS = Actor.Create("rifledrop", false, { Owner = USSR })
	Camera.Position = LightFlare.CenterPosition

	MissionStart()
	FootprintTriggers()
	SetupTriggers()
	Trigger.OnDamaged(Church, ChurchAttack)
	OnAnyDamaged(SovBase, SovBaseAttack)
end

OnAnyDamaged = function(actors, func)
	Utils.Do(actors, function(actor)
		Trigger.OnDamaged(actor, func)
	end)
end
