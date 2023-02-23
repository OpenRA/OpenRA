--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

GDInuke1 = { type = "nuk2", pos = CPos.New(6,3), blocked = false }
GDIproc1 = { type = "proc", pos = CPos.New(8,12), blocked = false }
GDIpyle = { type = "pyle", pos = CPos.New(17,4), blocked = false }
GDIweap = { type = "weap", pos = CPos.New(10,9), blocked = false }
GDIproc2 = { type = "proc", pos = CPos.New(12,3), blocked = false }
GDInuke2 = { type = "nuk2", pos = CPos.New(4,11), blocked = false }
GDInuke3 = { type = "nuk2", pos = CPos.New(3,14), blocked = false }
GDIdef1 = { type = "atwr", pos = CPos.New(8,5), blocked = false }
GDIdef2 = { type = "atwr", pos = CPos.New(6,10), blocked = false }
GDIdef3 = { type = "atwr", pos = CPos.New(6,16), blocked = false }
GDIdef4 = { type = "gtwr", pos = CPos.New(9,8), blocked = false }
GDInuke4 = { type = "nuk2", pos = CPos.New(13,12), blocked = false }
GDIdef5 = { type = "atwr", pos = CPos.New(15,10), blocked = false }
GDIdef6 = { type = "atwr", pos = CPos.New(13,16), blocked = false }
GDInuke5 = { type = "nuk2", pos = CPos.New(11,14), blocked = false }
GDIsilo1 = { type = "silo", pos = CPos.New(3,6), blocked = false }
GDIsilo2 = { type = "silo", pos = CPos.New(5,17), blocked = false }
GDIdef7 = { type = "atwr", pos = CPos.New(19,5), blocked = false }
GDIdef8 = { type = "atwr", pos = CPos.New(10,21), blocked = false }
GDIsilo3 = { type = "silo", pos = CPos.New(17,2), blocked = false }
GDIsilo4 = { type = "silo", pos = CPos.New(9,17), blocked = false }
GDIdef9 = { type = "atwr", pos = CPos.New(18,20), blocked = false }
GDIhpad1 = { type = "hpad", pos = CPos.New(3,18), blocked = false }

GDIBase = { GDINuke1, GDIPyle, GDIWeap, GDINuke2, GDINuke3, GDIDef1, GDIDef2, GDIDef3, GDIDef4,
	GDINuke4, GDIDef5, GDIDef6, GDINuke5, GDISilo1, GDISilo2, GDIDef7, GDIDef8, GDISilo3, GDISilo4, GDIDef9 }
GDIRebuildList = { GDInuke1, GDIproc1, GDIpyle, GDIweap, GDIproc2, GDInuke2, GDInuke3, GDIdef1, GDIdef2, GDIdef3, GDIdef4,
	GDInuke4, GDIdef5, GDIdef6, GDInuke5, GDIsilo1, GDIsilo2, GDIdef7, GDIdef8, GDIsilo3, GDIsilo4, GDIdef9 }
BSizes = { nuk2 = CVec.New(2,3), proc = CVec.New(3,4), pyle = CVec.New(2,3), weap = CVec.New(3,3), atwr = CVec.New(1,1), gtwr = CVec.New(1,1), silo = CVec.New(2,1), hpad = CVec.New(2,2) }

ProductionBuildings = { infantry = GDIPyle, vehicle = GDIWeap, aircraft = GDICYard }

ProductionQueue = { infantry = { }, vehicle = { }, aircraft = { } }
NewTeam = { }
TeamJob = "attack"
AT1 = { "e2", "e2", "e2", "e3", "e3", "vehicle", "jeep", "jeep", "apc" }
AT2 = { "e2", "e2", "e2", "e3", "e3", "e1", "e1", "e1", "e1" }
AT3 = { "vehicle", "mtnk", "mtnk", "jeep", "jeep" }
AT4 = { "e3", "e3", "e1", "e1", "e1", "vehicle", "mtnk", "mtnk", "msam" }
AT5 = { "vehicle", "jeep", "jeep", "jeep", "apc", "apc" }
AT6 = { "e3", "e3", "e1", "e1", "e1", "vehicle", "htnk" }
AT7 = { "vehicle", "htnk", "htnk", "mtnk" }
AT8 = { "vehicle", "htnk", "htnk", "msam" }
AT9 = { "vehicle", "mtnk", "msam", "apc", "apc" }
AT10 = { "e2", "e2", "e2", "e2", "e2", "e2", "e2", "e2", "e2", "e2" }
OT1 = { "aircraft", "orca", "orca" }
OT2 = { "aircraft", "orca", "orca", "orca", "orca" }
AttackTeams = { AT1, AT2, AT3, AT4, AT5, AT6, AT7, AT8, AT9, AT10 }

PT1 = { "vehicle", "htnk" }
PT2 = { "vehicle", "mtnk", "mtnk" }
PT3 = { "e2", "e2", "e2", "e3", "e3" }
PatrolTeams = { PT1, PT2, PT3 }

DT1 = { "vehicle", "mtnk", "mtnk", "msam", "msam" }
DT2 = { "e3", "e3", "e1", "e1", "e1", "vehicle", "mtnk", "mtnk", "msam" }
DT3 = { "vehicle", "htnk", "htnk", "msam" }
DefenseTeams = { DT1, DT2, DT3 }

AddOrcasTo = { AT1, AT2, AT5, AT6, AT9, DT1, DT2, DT3 }

AP1 = { waypoint5.Location, waypoint9.Location, waypoint8.Location, waypoint13.Location }
AP2 = { waypoint2.Location, waypoint5.Location, waypoint4.Location, waypoint6.Location }
AP3 = { waypoint2.Location, waypoint3.Location, waypoint4.Location, waypoint6.Location }
AP4 = { waypoint5.Location, waypoint9.Location, waypoint14.Location, PlayerStart.Location, waypoint13.Location }
AP5 = { waypoint5.Location, waypoint9.Location, waypoint6.Location }
AttackPaths = { AP1, AP2, AP3, AP4, AP5 }

InitAi = function()

	if Difficulty == "hard" then
		PBDelay = 115
		PUDelay = 30
		ICDelay = 800
		ProdCDSecs = 30
		GDI.Resources = 10000
		MinHarvs = 3
		ProcUpgrade = "AIHProcUpgrade"
		CivsBuildingsToDestroy = 8
	end

	if Difficulty == "normal" then
		PBDelay = 140
		PUDelay = 50
		ICDelay = 1000
		ProdCDSecs = 40
		GDI.Resources = 7000
		MinHarvs = 2
		ProcUpgrade = "AINProcUpgrade"
		CivsBuildingsToDestroy = 11
	end

	if Difficulty == "easy" then
		PBDelay = 180
		PUDelay = 70
		ICDelay = 1200
		ProdCDSecs = 50
		GDI.Resources = 5000
		MinHarvs = 2
		Nod.Resources = 3000
		CivsBuildingsToDestroy = 13
	end

	RepairNamedActors(GDI, 0.75)
	SetAutoRebuild()

	Trigger.OnKilled(GDIPyle, function()
		ProductionQueue["infantry"] = { }
	end)
	Trigger.OnKilled(GDIWeap, function()
		ProductionQueue["vehicle"] = { }
	end)

	if ProcUpgrade then
		ProcUpg = Actor.Create(ProcUpgrade, true, { Owner = GDI })
	end

	Trigger.AfterDelay(DateTime.Seconds(PBDelay), function()

		CheckBase()

		Trigger.AfterDelay(DateTime.Seconds(PUDelay), function()
			ProduceUnits()
			ReduceProdCD()
		end)

		Trigger.AfterDelay(DateTime.Seconds(ICDelay), function()
			FireIonCannon(270)
		end)

		if not GDIAdvComCenter.IsDead then
			IonCannonOnline = true
			Media.DisplayMessage(UserInterface.Translate("destroy-ion-cannon-advise"))
			DestroyIonCannon = AddSecondaryObjective(Nod, "quickly-destroy-ion-cannon")
		end
	end)

end

ReduceProdCD = function()
	Trigger.AfterDelay(DateTime.Minutes(2), function()
		ProdCDSecs = ProdCDSecs - 1
		if ProdCDSecs > 10 then
			ReduceProdCD()
		end
	end)
end

PrepareOrcas = function()
	table.insert(GDIRebuildList, 11, GDIhpad1)
	Utils.Do(AddOrcasTo, function(t)
		Utils.Do(OT1, function(u)
			table.insert(t, u)
		end)
	end)
	table.insert(AttackTeams, OT2)
	table.insert(PatrolTeams, OT1)
end

--Building logic
SetAutoRebuild = function()
	Utils.Do(GDIBase, function(b)
		Trigger.OnKilled(b, CheckBase)
	end)
end

CheckBase = function()
	if GDICYard.IsDead then return end
	for i = 1, #GDIRebuildList do
		if GDIRebuildList[i].blocked then
			CheckBuildablePlace(GDIRebuildList[i])
		else
			local building = GDI.GetActorsByType(GDIRebuildList[i].type)
			if #building < 1 then
				BuildBuilding(GDIRebuildList[i], GDICYard)
				return
			end
			for ii = 1, #building do
				if not building[ii].IsDead and building[ii].Location == GDIRebuildList[i].pos then
					break
				end
				if ii == #building then
					BuildBuilding(GDIRebuildList[i], GDICYard)
					return
				end
			end
		end
	end
	if not CheckProgrammed then
		CheckProgrammed = true
		Trigger.AfterDelay(250, function()
			CheckProgrammed = false
			CheckBase()
		end)
	end
end

CheckBuildablePlace = function(b)
	local actors = Map.ActorsInBox(WPos.New(b.pos.X * 1024, b.pos.Y * 1024, 0), WPos.New((b.pos.X + BSizes[b.type].X) * 1024, (b.pos.Y + BSizes[b.type].Y) * 1024, 0), function(a) return a.Owner == Nod end)
	if #actors > 0 then
		b.blocked = true
		return false
	else
		b.blocked = false
		return true
	end
end

BuildBuilding = function(building, cyard)
	if CyardIsBuilding or GDI.Resources < Actor.Cost(building.type) then
		if Dontspam == true then
			return
		end
		Dontspam = true
		Trigger.AfterDelay(DateTime.Seconds(10), function()
			Dontspam = false
			CheckBase()
		end)
		return
	end

	CyardIsBuilding = true

	GDI.Resources = GDI.Resources - Actor.Cost(building.type)
	Trigger.AfterDelay(Actor.BuildTime(building.type), function()
		CyardIsBuilding = false

		if cyard.IsDead or cyard.Owner ~= GDI then
			GDI.Resources = GDI.Resources + Actor.Cost(building.type)
			return
		end

		if CheckBuildablePlace(building) == false then
			CheckBase()
			return
		end

		local newbuilding = Actor.Create(building.type, true, { Owner = GDI, Location = building.pos })
		Trigger.OnKilled(newbuilding, function() CheckBase() end)
		RepairBuilding(GDI, newbuilding, 0.75)
		GuardBuilding(newbuilding)

		if newbuilding.Type == "pyle" then
			ProductionBuildings["infantry"] = newbuilding
			Trigger.OnKilled(newbuilding, function() ProductionQueue["infantry"] = { } end)
			RestartUnitProduction()
		elseif newbuilding.Type == "weap" then
			ProductionBuildings["vehicle"] = newbuilding
			Trigger.OnKilled(newbuilding, function() ProductionQueue["vehicle"] = { } end)
			RestartUnitProduction()
			NeedHarv = false
		elseif newbuilding.Type == "hpad" then
			ProductionBuildings["aircraft"] = newbuilding
			Trigger.OnKilled(newbuilding, function() ProductionQueue["aircraft"] = { } end)
			RestartUnitProduction()
		end

		Trigger.AfterDelay(50, CheckBase)
	end)

end

--Units production logic
UniqueTeamsQueue = { }
ProdCooldown = false
CheckProduction = function()
	if #GDI.GetActorsByType("proc") < 1 and GDI.Resources < 6000 then
		Trigger.AfterDelay(250, CheckProduction)
		return
	elseif ProductionBuildings["infantry"].IsDead and ProductionBuildings["vehicle"].IsDead and ProductionBuildings["aircraft"].IsDead then
		return
	elseif not ProductionBuildings["vehicle"].IsDead and CheckForHarvester() then
		NeedHarv = true
		ProductionBuildings["vehicle"].Build( { "harv" }, function()
			CheckProduction()
			Trigger.AfterDelay(DateTime.Seconds(5), function()
				if not ProductionBuildings["vehicle"].IsDead then
					ProductionBuildings["vehicle"].Build( { "harv" } )
				end
			end)
		end)
		return
	end
	NeedHarv = false

	if #ProductionQueue["infantry"] + #ProductionQueue["vehicle"] + #ProductionQueue["aircraft"] < 1 and Producing then
		Producing = false
	end
	if ProdCooldown or GDI.Resources < 4000 then
		RestartUnitProduction()
	else
		ProduceUnits()
	end

end

ProduceUnits = function()
	if NeedHarv then
		RestartUnitProduction()
		return
	end
	if not Producing then
		NewTeam = { }
		CreateUnitsGroup()
		if #ProductionQueue["infantry"] + #ProductionQueue["vehicle"] + #ProductionQueue["aircraft"] > 0 then
			Producing = true
		else
			RestartUnitProduction()
			return
		end
	end

	if #ProductionQueue["infantry"] > 0 and not ProductionBuildings["infantry"].IsDead and not ProductionBuildings["infantry"].IsProducing("e2") then
		ProductionBuildings["infantry"].Build( { ProductionQueue["infantry"][1] }, function(u)
			table.insert(NewTeam, u[1])
			table.remove(ProductionQueue["infantry"], 1)
			CheckTeamCompleted()
		end)
	end
	if #ProductionQueue["vehicle"] > 0 and not ProductionBuildings["vehicle"].IsDead and not ProductionBuildings["vehicle"].IsProducing("mtnk") then
		ProductionBuildings["vehicle"].Build( { ProductionQueue["vehicle"][1] }, function(u)
			table.insert(NewTeam, u[1])
			table.remove(ProductionQueue["vehicle"], 1)
			CheckTeamCompleted()
		end)
	end
	if #ProductionQueue["aircraft"] > 0 and not ProductionBuildings["aircraft"].IsDead and not ProductionBuildings["aircraft"].IsProducing("orca") then
		ProductionBuildings["aircraft"].Build( { ProductionQueue["aircraft"][1] }, function(u)
			table.insert(NewTeam, u[1])
			table.remove(ProductionQueue["aircraft"], 1)
			CheckTeamCompleted()
		end)
	end

	RestartUnitProduction()

end

CheckTeamCompleted = function()
	if #ProductionQueue["infantry"] + #ProductionQueue["vehicle"] + #ProductionQueue["aircraft"] < 1 and #NewTeam > 0 then
		NewTeam = Utils.Where(NewTeam, function(u) return not u.IsDead end)
		if TeamJob == "attack" then
			SendAttackGroup(NewTeam)
		elseif TeamJob == "patrol" then
			TeamJob = "attack"
			PatrolTeam = NewTeam
			StartPatrol()
		elseif TeamJob == "defend" then
			TeamJob = "attack"
			BaseDefenseTeam = NewTeam
			StartGuard()
		end
		Producing = false
		ProdCooldown = true
		NewTeam = { }
		Trigger.AfterDelay(DateTime.Seconds(ProdCDSecs), function() ProdCooldown = false end)
	else
		ProduceUnits()
	end
end

SendAttackGroup = function(team)
	MoveAsGroup(team, Utils.Random(AttackPaths), 1, false)
end

CreateUnitsGroup = function()
	local team = AttackTeams
	if #UniqueTeamsQueue > 0 then
		team = UniqueTeamsQueue[1].team
		TeamJob = UniqueTeamsQueue[1].job
		table.remove(UniqueTeamsQueue, 1)
	end
	local pb = "infantry"
	Utils.Do(Utils.Random(team), function(u)
		if u == "vehicle" or u == "aircraft" then
			pb = u
		elseif ProductionBuildings[pb] and not ProductionBuildings[pb].IsDead and ProductionBuildings[pb].Owner == GDI then
			table.insert(ProductionQueue[pb], u)
		end
	end)
end

RestartUnitProduction = function()
	if not Restarting then
		Restarting = true
	else
		return
	end
	Trigger.AfterDelay(DateTime.Seconds(5), function()
		Restarting = false
		CheckProduction()
	end)
end

CheckForHarvester = function()
	local harv = GDI.GetActorsByType("harv")
	return #harv < MinHarvs
end
