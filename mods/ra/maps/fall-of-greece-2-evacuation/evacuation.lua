--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
AlliedReinforcementsA = { "jeep", "jeep" }
AlliedReinforcementsB = { "e1", "e1", "e1", "e3", "e3" }
AlliedReinforcementsC = { "jeep", "mcv" }
IslandPatrol = { SubPatrol1.Location, SubPatrol2.Location, SubPatrol3.Location, SubPatrol4.Location }
Submarines = { Sub1, Sub2, Sub3, Sub4, Sub5, Sub6, Sub7, Sub8 }
RifleSquad = { Rifle1, Rifle2, Rifle3 }
NWVillageTrigger = { CPos.New(31, 64), CPos.New(32, 64), CPos.New(34, 51), CPos.New(35, 51), CPos.New(36, 51) }
SWVillageTrigger = { CPos.New(44, 97), CPos.New(45, 97), CPos.New(46, 97), CPos.New(47, 97) }
MiddleVillageTrigger = { CPos.New(52, 70), CPos.New(53, 70), CPos.New(54, 70) }
EastVillageTrigger = { CPos.New(78, 61), CPos.New(79, 61), CPos.New(80, 61), CPos.New(81, 61), CPos.New(82, 61) }
CivilianTypes = { "c2", "c3", "c4", "c5", "c6", "c8", "c9", "c10", "c11" }
NWVillage = { NWChurch, NWHouse1, NWHouse2, NWHouse3 }
SWVillage = { SWChurch, SWHouse1, SWHouse2, SWHouse3, SWHouse4 }
MiddleVillage = { MiddleChurch, MiddleHouse1, MiddleHouse2, MiddleHouse3, MiddleHouse4 }
EastVillage = { EastChurch, EastHouse1, EastHouse2, EastHouse3 }

MissionStart = function()
	Reinforcements.Reinforce(Allies, AlliedReinforcementsA, { AlliedSpawn.Location, AlliedBase.Location }, 5)
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		AlliesArrived = true
		Reinforcements.Reinforce(Allies, AlliedReinforcementsB, { AlliedSpawn.Location, AlliedBase.Location }, 2)
		Utils.Do(RifleSquad, function(actor)
			if not actor.IsDead then
				actor.AttackMove(AlliedBase.Location)
				actor.Hunt()
			end
		end)
	end)

	Sub1.Patrol(IslandPatrol, true, 1)

	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Reinforcements.Reinforce(Allies, AlliedReinforcementsC, { AlliedSpawn.Location, AlliedBase.Location }, 5)
	end)

	Trigger.OnKilled(NWChurch, function()
		if not NWChurchEmpty then
			CiviliansKilled = CiviliansKilled + 5
		end
	end)
	Trigger.OnKilled(EastChurch, function()
		if not EastChurchEmpty then
			CiviliansKilled = CiviliansKilled + 5
		end
	end)
	Trigger.OnKilled(MiddleChurch, function()
		if not MiddleChurchEmpty then
			CiviliansKilled = CiviliansKilled + 5
		end
	end)
	Trigger.OnKilled(SWChurch, function()
		if not SWChurchEmpty then
			CiviliansKilled = CiviliansKilled + 5
		end
	end)

	Trigger.OnAllKilled(Submarines, function()
		Allies.MarkCompletedObjective(ClearWaterway)
	end)
end

VillageSetup = function()
	local foot1Triggered
	Trigger.OnEnteredFootprint(NWVillageTrigger, function(actor, id)
		if actor.Owner == Allies and not foot1Triggered then
			Trigger.RemoveFootprintTrigger(id)
			foot1Triggered = true

			Utils.Do(NWVillage, function(building)
				building.Owner = Allies
			end)

			Civs1 = Reinforcements.Reinforce(Allies, Utils.Take(5, Utils.Shuffle(CivilianTypes)), { ChurchNorthwest.Location, VillageNorthwest.Location }, 0)
			if not NWChurch.IsDead then
				Utils.Do(Civs1, function(civ)
					Trigger.OnKilled(civ, function()
						CiviliansKilled = CiviliansKilled + 1
					end)
				end)
				NWChurchEmpty = true
			end
		end

		Trigger.AfterDelay(DateTime.Seconds(30), function()
			local nwAttackers = Reinforcements.Reinforce(USSR, { "3tnk", "3tnk", "3tnk" }, { NWVillageAttack.Location, VillageNorthwest.Location }, 20)
			Utils.Do(nwAttackers, IdleHunt)
		end)
	end)

	local foot2Triggered
	Trigger.OnEnteredFootprint(EastVillageTrigger, function(actor, id)
		if actor.Owner == Allies and not foot2Triggered then
			Trigger.RemoveFootprintTrigger(id)
			foot2Triggered = true

			Utils.Do(EastVillage, function(building)
				building.Owner = Allies
			end)

			Civs2 = Reinforcements.Reinforce(Allies, Utils.Take(5, Utils.Shuffle(CivilianTypes)), { ChurchEast.Location, VillageEast.Location }, 0)
			if not EastChurch.IsDead then
				Utils.Do(Civs2, function(civ)
					Trigger.OnKilled(civ, function()
						CiviliansKilled = CiviliansKilled + 1
					end)
				end)
				EastChurchEmpty = true
			end

			local villageDrop = FlamerDrop.TargetParatroopers(VillageEast.CenterPosition, Angle.North)
			Utils.Do(villageDrop, function(a)
				Trigger.OnPassengerExited(a, function(t, p)
					IdleHunt(p)
				end)
			end)
		end
	end)

	local foot3Triggered
	Trigger.OnEnteredFootprint(MiddleVillageTrigger, function(actor, id)
		if actor.Owner == Allies and not foot3Triggered then
			Trigger.RemoveFootprintTrigger(id)
			foot3Triggered = true

			Utils.Do(MiddleVillage, function(building)
				building.Owner = Allies
			end)

			Civs3 = Reinforcements.Reinforce(Allies, Utils.Take(5, Utils.Shuffle(CivilianTypes)), { ChurchMiddle.Location, VillageMiddle.Location }, 0)
			if not MiddleChurch.IsDead then
				Utils.Do(Civs3, function(civ)
					Trigger.OnKilled(civ, function()
						CiviliansKilled = CiviliansKilled + 1
					end)
				end)
				MiddleChurchEmpty = true
			end

			local proxy = Actor.Create("powerproxy.parabombs", false, { Owner = USSR })
			proxy.TargetAirstrike(ChurchMiddle.CenterPosition, Angle.NorthWest)
		end
	end)

	local foot4Triggered
	Trigger.OnEnteredFootprint(SWVillageTrigger, function(actor, id)
		if actor.Owner == Allies and not foot4Triggered then
			Trigger.RemoveFootprintTrigger(id)
			foot4Triggered = true

			Utils.Do(SWVillage, function(building)
				building.Owner = Allies
			end)

			Civs4 = Reinforcements.Reinforce(Allies, Utils.Take(5, Utils.Shuffle(CivilianTypes)), { ChurchSouthwest.Location, VillageSouthwest.Location }, 0)
			if not SWChurch.IsDead then
				Utils.Do(Civs4, function(civ)
					Trigger.OnKilled(civ, function()
						CiviliansKilled = CiviliansKilled + 1
					end)
				end)
				SWChurchEmpty = true
			end
		end

		Trigger.AfterDelay(DateTime.Seconds(30), function()
			local swAttackers = Reinforcements.Reinforce(USSR, { "3tnk", "3tnk", "3tnk", "ttnk", "e4", "e4", "e4" }, { SWVillageAttack.Location, VillageSouthwest.Location }, 20)
			Utils.Do(swAttackers, IdleHunt)
		end)
	end)
end

SetCivilianEvacuatedText = function()
	local attributes = { ["evacuated"] = CiviliansEvacuated, ["threshold"] = CiviliansEvacuatedThreshold }
	local civiliansEvacuated = UserInterface.Translate("civilians-evacuated", attributes)
	UserInterface.SetMissionText(civiliansEvacuated, TextColor)
end

CiviliansEvacuatedThreshold =
{
	hard = 20,
	normal = 15,
	easy = 10
}
CiviliansKilledThreshold =
{
	hard = 1,
	normal = 6,
	easy = 11
}
CiviliansEvacuated = 0
CiviliansKilled = 0
EvacuateCivilians = function()
	Trigger.OnInfiltrated(SafeHouse, function()
		CiviliansEvacuated = CiviliansEvacuated + 1
		SetCivilianEvacuatedText()
	end)

	Trigger.OnKilled(SafeHouse, function()
		USSR.MarkCompletedObjective(SovietObj)
	end)

	local enemyBase = Utils.Where(USSR.GetActors(), function(actor)
		return
			actor.HasProperty("Sell") and
			actor.Type ~= "brik"
	end)

	Trigger.OnAllKilled(enemyBase, function()
		Media.PlaySoundNotification(Allies, "AlertBleep")
		Media.DisplayMessage(UserInterface.Translate("chinook-assist-evacuation"), UserInterface.Translate("chinook-pilot"))
		Reinforcements.Reinforce(Allies, { "tran" }, { ChinookEntry.Location, ChinookLZ.Location })
	end)
end

Tick = function()
	USSR.Cash = 10000
	if CiviliansEvacuated >= CiviliansEvacuatedThreshold then
		Allies.MarkCompletedObjective(RescueCivilians)
	end

	if CiviliansKilled >= CiviliansKilledThreshold then
		Allies.MarkFailedObjective(RescueCivilians)
	end

	if AlliesArrived and Allies.HasNoRequiredUnits() then
		USSR.MarkCompletedObjective(SovietObj)
	end
end

WorldLoaded = function()
	Allies = Player.GetPlayer("Allies")
	USSR = Player.GetPlayer("USSR")

	InitObjectives(Allies)

	if Difficulty == "easy" then
		RescueCivilians = AddPrimaryObjective(Allies, "rescue-civlians-island-shelter-easy")
	elseif Difficulty == "normal" then
		RescueCivilians = AddPrimaryObjective(Allies, "rescue-civlians-island-shelter-normal")
	else
		RescueCivilians = AddPrimaryObjective(Allies, "rescue-civlians-island-shelter-hard")
	end

	ClearWaterway = AddSecondaryObjective(Allies, "clear-enemy-submarines")
	SovietObj = AddPrimaryObjective(USSR, "")

	CiviliansEvacuatedThreshold = CiviliansEvacuatedThreshold[Difficulty]
	CiviliansKilledThreshold = CiviliansKilledThreshold[Difficulty]
	TextColor = Allies.Color
	SetCivilianEvacuatedText()
	StandardDrop = Actor.Create("paradrop", false, { Owner = USSR })
	FlamerDrop = Actor.Create("flamerdrop", false, { Owner = USSR })
	Camera.Position = DefaultCameraPosition.CenterPosition
	MissionStart()
	VillageSetup()
	EvacuateCivilians()
	ActivateAI()
end
