--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
Cabins = { NorthCabin, SouthCabin }
NWFootprintTrigger = { CPos.New(41, 49), CPos.New(41, 48), CPos.New(41, 47), CPos.New(41, 46), CPos.New(41, 45), CPos.New(41, 44) }
SWFootprintTrigger = { CPos.New(26, 77), CPos.New(27, 77), CPos.New(28, 77), CPos.New(29, 77), CPos.New(33, 77), CPos.New(34, 77), CPos.New(35, 77), CPos.New(36, 77), CPos.New(37, 77), CPos.New(38, 77), CPos.New(39, 77) }
SEFootprintTrigger = { CPos.New(75, 83), CPos.New(76, 83), CPos.New(77, 83), CPos.New(78, 83), CPos.New(79, 83), CPos.New(80, 83), CPos.New(81, 83), CPos.New(82, 83), CPos.New(83, 83), CPos.New(84, 83), CPos.New(85, 83), CPos.New(86, 83), CPos.New(87, 83), CPos.New(88, 83), CPos.New(89, 83) }
NWWaterPath = { WaterEntryNW.Location, WaterLandingNW.Location }
NEWaterPath = { WaterEntryNE.Location, WaterLandingNE.Location }
SEWaterPath = { WaterEntrySE.Location, WaterLandingSE.Location }
NWWaterUnits = { "dtrk", "ttnk", "ttnk", "ttnk", "3tnk" }
NEWaterUnits = { "dtrk", "v2rl", "e6", "e6", "e6" }
SEWaterUnits = { "ttnk", "ttnk", "shok", "shok", "shok" }
NorthPillboxes = { NorthPill1, NorthPill2 }
SouthPillboxes = { SouthPill1, SouthPill2 }
VehicleSquad1 = { Ltnk1, Jeep1 }
VehicleSquad2 = { Ltnk2, Jeep2 }
VehicleSquad3 = { Ltnk3, Jeep3 }
CivSquads = { { "c1", "c3", "c7", "c10" }, { "c2", "c4", "c6", "c11" }, { "c11", "c10", "c9" }, { "c8", "c7", "c6" }, { "c5", "c4", "c3" }, { "c5", "c10" }, { "c4", "c2" }, { "c3", "c5" }, { "c9", "c11" } }
AlliedInfantry = { "e1", "e1", "e1", "e3" }
AttackGroupSize = 4
InfantryDelay = DateTime.Seconds(10)
IdlingUnits = { }

MissionStart = function()
	LZCamera = Actor.Create("camera", true, { Owner = USSR, Location = LZ.Location })
	ShockDrop.TargetParatroopers(LZ.CenterPosition, Angle.New(508))

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		ShockDrop.TargetParatroopers(LZ.CenterPosition, Angle.New(520))
	end)

	Trigger.AfterDelay(DateTime.Seconds(4), function()
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
	end)

	Trigger.AfterDelay(DateTime.Seconds(10), function()
		LZCamera.Destroy()
		TroopsArrived = true
	end)

	ProduceInfantry()
end

ProduceInfantry = function()
	if (SETent1.IsDead or SETent1.Owner ~= Greece) and (SETent2.IsDead or SETent2.Owner ~= Greece) then
		return
	end

	local toBuild = { Utils.Random(AlliedInfantry) }

	Greece.Build(toBuild, function(unit)
		IdlingUnits[#IdlingUnits + 1] = unit[1]
		Trigger.AfterDelay(InfantryDelay, ProduceInfantry)

		if #IdlingUnits >= (AttackGroupSize * 1.5) then
			SendAttack()
		end
	end)
end

SendAttack = function()
	local units = { }

	for i = 0, AttackGroupSize, 1 do
		local number = Utils.RandomInteger(1, #IdlingUnits)

		if IdlingUnits[number] and not IdlingUnits[number].IsDead then
			units[i] = IdlingUnits[number]
			table.remove(IdlingUnits, number)
		end
	end

	Utils.Do(units, function(unit)
		if not unit.IsDead then
			IdleHunt(unit)
		end
	end)
end

DomeCaptured = false
MissionTriggers = function()
	local neFootTriggered
	Trigger.OnEnteredProximityTrigger(NECivSpawn1.CenterPosition, WDist.FromCells(9), function(actor, id)
		if actor.Owner == USSR and actor.Type ~= "badr" and not neFootTriggered then
			Trigger.RemoveFootprintTrigger(id)
			neFootTriggered = true

			local proxy = Actor.Create("powerproxy.parabombs", false, { Owner = USSR })
			proxy.TargetAirstrike(NECivSpawn6.CenterPosition, Angle.SouthEast)
			proxy.Destroy()

			Utils.Do(VehicleSquad1, function(a)
				if not a.IsDead then
					a.AttackMove(NECivSpawn1.Location)
					IdleHunt(a)
				end
			end)

			local neVillageCam = Actor.Create("camera", true, { Owner = USSR, Location = NECivSpawn1.Location })
			Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
			local neTroops = Reinforcements.ReinforceWithTransport(USSR, "lst.reinforcement", NEWaterUnits, NEWaterPath, { NEWaterPath[1] })[2]
			Trigger.OnAllRemovedFromWorld(Utils.Where(neTroops, function(a) return a.Type == "e6" end), function()
				if not DomeCaptured then
					USSR.MarkFailedObjective(CaptureDome)
				end
			end)

			Trigger.AfterDelay(DateTime.Seconds(2), function()
				if not NEVillage1.IsDead then
					Reinforcements.Reinforce(Spain, Utils.Random(CivSquads), { NECivSpawn1.Location, CivRallyNW.Location }, 0)
				end
				if not NEVillage2.IsDead then
					Reinforcements.Reinforce(Spain, Utils.Random(CivSquads), { NECivSpawn2.Location, CivRallyNW.Location }, 0)
				end
			end)
			Trigger.AfterDelay(DateTime.Seconds(3), function()
				if not NEVillage3.IsDead then
					Reinforcements.Reinforce(Spain, Utils.Random(CivSquads), { NECivSpawn3.Location, CivRallyNW.Location }, 0)
				end
				if not NEVillage4.IsDead then
					Reinforcements.Reinforce(Spain, Utils.Random(CivSquads), { NECivSpawn4.Location, CivRallyNW.Location }, 0)
				end
			end)
			Trigger.AfterDelay(DateTime.Seconds(4), function()
				if not NEVillage5.IsDead then
					Reinforcements.Reinforce(Spain, Utils.Random(CivSquads), { NECivSpawn5.Location, CivRallyNW.Location }, 0)
				end
				if not NEVillage6.IsDead then
					Reinforcements.Reinforce(Spain, Utils.Random(CivSquads), { NECivSpawn6.Location, CivRallySE.Location }, 0)
				end
			end)
			Trigger.AfterDelay(DateTime.Seconds(5), function()
				if not NEVillage7.IsDead then
					Reinforcements.Reinforce(Spain, Utils.Random(CivSquads), { NECivSpawn7.Location, CivRallySE.Location }, 0)
				end
				if not NEVillage8.IsDead then
					Reinforcements.Reinforce(Spain, Utils.Random(CivSquads), { NECivSpawn8.Location, CivRallySE.Location }, 0)
				end
				if not NEVillage9.IsDead then
					Reinforcements.Reinforce(Spain, Utils.Random(CivSquads), { NECivSpawn9.Location, CivRallySE.Location }, 0)
				end
			end)

			Trigger.AfterDelay(DateTime.Seconds(10), function()
				neVillageCam.Destroy()
			end)
		end
	end)

	Trigger.OnDamaged(NorthCabin, function()
		if not NorthCabinDamaged then
			Reinforcements.Reinforce(Spain, Utils.Random(CivSquads), { NEHermitSpawn.Location, CivRallyNW.Location }, 1)
			NorthCabinDamaged = true
		end
	end)

	Trigger.OnDamaged(SouthCabin, function()
		if not SouthCabinDamaged then
			Reinforcements.Reinforce(Spain, Utils.Random(CivSquads), { SouthHermitSpawn.Location, CivRallySE.Location }, 1)
			SouthCabinDamaged = true
		end
	end)

	Trigger.OnAllKilled(Cabins, function()
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		ShockDrop.TargetParatroopers(LZ.CenterPosition, Angle.SouthWest)
	end)

	Trigger.OnAllKilled(NorthPillboxes, function()
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		ShockDrop.TargetParatroopers(LZ.CenterPosition, Angle.South)
	end)

	local nwFootTriggered
	Trigger.OnEnteredFootprint(NWFootprintTrigger, function(actor, id)
		if actor.Owner == USSR and not nwFootTriggered then
			Trigger.RemoveFootprintTrigger(id)
			nwFootTriggered = true

			local nwBarracks1 = Reinforcements.Reinforce(GoodGuy, AlliedInfantry, { NWSpawn1.Location }, 0)
			Utils.Do(nwBarracks1, IdleHunt)
			local nwBarracks2 = Reinforcements.Reinforce(GoodGuy, AlliedInfantry, { NWSpawn2.Location }, 0)
			Utils.Do(nwBarracks2, IdleHunt)

			Utils.Do(VehicleSquad2, function(a)
				if not a.IsDead then
					a.AttackMove(SWCivSpawn1.Location)
					IdleHunt(a)
				end
			end)
		end
	end)

	Trigger.OnKilled(RadarDome, function()
		if not DomeCaptured then
			USSR.MarkFailedObjective(CaptureDome)
			Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
			ShockDrop.TargetParatroopers(CivRallyNW.CenterPosition, Angle.West)
		end
	end)

	Trigger.OnCapture(RadarDome, function()
		DomeCaptured = true
		USSR.MarkCompletedObjective(CaptureDome)
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		local nwTroops = Reinforcements.ReinforceWithTransport(USSR, "lst.reinforcement", NWWaterUnits, NWWaterPath, { NWWaterPath[1] })[2]
	end)

	Trigger.OnAllKilled(SouthPillboxes, function()
		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		local seTroops = Reinforcements.ReinforceWithTransport(USSR, "lst.reinforcement", SEWaterUnits, SEWaterPath, { SEWaterPath[1] })[2]

		Trigger.AfterDelay(DateTime.Seconds(10), function()
			Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
			Reinforcements.Reinforce(USSR, { "msub", "msub" }, { MSubEntry.Location, MSubStop.Location })
			ShockDrop.TargetParatroopers(SEShockDrop.CenterPosition, Angle.SouthWest)
		end)
	end)

	local swFootTriggered
	Trigger.OnEnteredFootprint(SWFootprintTrigger, function(actor, id)
		if actor.Owner == USSR and actor.Type ~= "badr" and not swFootTriggered then
			Trigger.RemoveFootprintTrigger(id)
			swFootTriggered = true

			local proxy = Actor.Create("powerproxy.parabombs", false, { Owner = USSR })
			proxy.TargetAirstrike(SWCivSpawn3.CenterPosition, Angle.South)
			proxy.Destroy()
			local swVillageCam = Actor.Create("camera", true, { Owner = USSR, Location = SWCivSpawn2.Location })

			local hiding = Reinforcements.Reinforce(Greece, { 'e1', 'e1', 'e3', 'e3', 'e3' }, { SWCivSpawn1.Location }, 0)
			Utils.Do(hiding, IdleHunt)

			Trigger.AfterDelay(DateTime.Seconds(2), function()
				if not SWVillage1.IsDead then
					Reinforcements.Reinforce(Spain, Utils.Random(CivSquads), { SWCivSpawn1.Location, CivRallySE.Location }, 0)
				end
				if not SWVillage2.IsDead then
					Reinforcements.Reinforce(Spain, Utils.Random(CivSquads), { SWCivSpawn2.Location, CivRallySE.Location }, 0)
				end
			end)
			Trigger.AfterDelay(DateTime.Seconds(4), function()
				if not SWVillage3.IsDead then
					Reinforcements.Reinforce(Spain, Utils.Random(CivSquads), { SWCivSpawn3.Location, CivRallySE.Location }, 0)
				end
				if not SWVillage4.IsDead then
					Reinforcements.Reinforce(Spain, Utils.Random(CivSquads), { SWCivSpawn4.Location, CivRallyNW.Location }, 0)
				end
			end)
			Trigger.AfterDelay(DateTime.Seconds(5), function()
				if not SWVillage5.IsDead then
					Reinforcements.Reinforce(Spain, Utils.Random(CivSquads), { SWCivSpawn5.Location, CivRallySE.Location }, 0)
				end
				if not SWVillage6.IsDead then
					Reinforcements.Reinforce(Spain, Utils.Random(CivSquads), { SWCivSpawn6.Location, CivRallyNW.Location }, 0)
				end
				if not SWVillage7.IsDead then
					Reinforcements.Reinforce(Spain, Utils.Random(CivSquads), { SWCivSpawn7.Location, CivRallySE.Location }, 0)
				end
			end)
			Trigger.AfterDelay(DateTime.Seconds(5), function()
				ShockDrop.TargetParatroopers(SouthwestLZ.CenterPosition, Angle.SouthWest)
				Utils.Do(VehicleSquad3, function(a)
					if not a.IsDead then
						a.AttackMove(NECivSpawn1.Location)
						IdleHunt(a)
					end
				end)
				swVillageCam.Destroy()
			end)
		end
	end)

	local seFootTriggered
	Trigger.OnEnteredFootprint(SEFootprintTrigger, function(actor, id)
		if actor.Owner == USSR and not seFootTriggered then
			Trigger.RemoveFootprintTrigger(id)
			seFootTriggered = true

			local proxy = Actor.Create("powerproxy.parabombs", false, { Owner = USSR })
			proxy.TargetAirstrike(SEBaseBombingRun.CenterPosition, Angle.East)
			proxy.Destroy()
		end
	end)
end

TroopsArrived = false
Tick = function()
	if USSR.HasNoRequiredUnits() and TroopsArrived then
		Greece.MarkCompletedObjective(BeatRussia)
	end

	if Greece.HasNoRequiredUnits() and GoodGuy.HasNoRequiredUnits() and Spain.HasNoRequiredUnits() then
		USSR.MarkCompletedObjective(KillAll)
	end
end

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
	Greece = Player.GetPlayer("Greece")
	GoodGuy = Player.GetPlayer("GoodGuy")
	Spain = Player.GetPlayer("Spain")

	InitObjectives(USSR)

	BeatRussia = AddPrimaryObjective(Greece, "")
	KillAll = AddPrimaryObjective(USSR, "destroy-opposition")
	CaptureDome = AddSecondaryObjective(USSR, "capture-enemy-radar-dome")

	Camera.Position = LZ.CenterPosition
	ShockDrop = Actor.Create("shockdrop", false, { Owner = USSR })
	MissionStart()
	MissionTriggers()
end
