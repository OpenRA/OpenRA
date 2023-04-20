--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

ProductionTypes = { "hand", "pyle", "afld", "weap", "hpad" }
PowerTypes = { "nuk2", "nuke" }

NodCYards = { NodCYard, NodOutPostCYard }
NodBase = { NodNuke1, NodProc1, NodHand, NodAfld, NodProc2, NodHpad1, NodNuke2, NodHq, NodGun3, NodGun4, NodNuke3, NodObli1, NodObli2, NodNuke4, NodObli3, NodObli4, NodNuke5, NodGun1, NodGun2, NodGun5, NodNuke6, NodGun6, NodNuke7, NodNuke8, NodSilo1, NodSilo2, NodSilo3, NodSilo4 }
NodRebuildList = { }
BuildingSizes = { nuk2 = CVec.New(2,3), proc = CVec.New(3,4), hand = CVec.New(2,3), afld = CVec.New(4,3), hpad = CVec.New(2,3), hq = CVec.New(2,3), obli = CVec.New(1,1), gun = CVec.New(1,1), silo = CVec.New(2,1) }

ProductionBuildings = { infantry = NodHand, vehicle = NodAfld, aircraft = NodHpad1 }
ProductionQueue = { infantry = { }, vehicle = { }, aircraft = { } }
NewTeam = { }
InitPatrolTeam = { Pat1, Pat2, Pat3 }
InitPatrolTeamAlive = true
PatrolTeam = { Pat1, Pat2, Pat3 }
BaseDefenseTeam = { Def1, Def2, Def3, Def4, Def5, Def6, Def7, Def8, Def9, Def10 }
StealthTeam = { }
TeamJob = "attack"

AT1 = { "e1", "e1", "e1", "e1", "e3", "e3", "e3", "e3", "e4", "e4", "e5", "e5" }
AT2 = { "e3", "e3", "e3", "e4", "e4", "vehicle", "ftnk", "ltnk", "ltnk", "aircraft", "heli" }
AT3 = { "vehicle", "mlrs", "bggy", "bggy", "ltnk", "ltnk", "ltnk", "arty" }
AT4 = { "vehicle", "ftnk", "ftnk", "ltnk", "ltnk", "ltnk", "aircraft", "heli", "heli" }
AT5 = { "e5", "e5", "e5", "e5", "vehicle", "bike", "bike", "ltnk", "arty" }
AT6 = { "vehicle", "ftnk", "ltnk", "ltnk", "arty", "arty", "mlrs", "stnk" }
AT7 = { "e1", "e1", "e1", "e5", "e5", "e5", "vehicle", "arty", "bike", "bike", "aircraft", "heli", "heli" }
AT8 = { "e5", "e5", "e5", "vehicle", "bike", "bike", "bike", "stnk", "stnk", "stnk" }
AT9 = { "e5", "e5", "e5", "e3", "e3", "vehicle", "ltnk", "ltnk", "arty" }
AT10 = { "vehicle", "stnk", "stnk", "stnk", "stnk", "stnk" }
AttackTeams = { AT1, AT2, AT3, AT4, AT5, AT6, AT7, AT8, AT9, AT10 }

PT1 = { "vehicle", "stnk", "stnk" }
PT2 = { "vehicle", "bike", "bike", "bike" }
PT3 = { "aircraft", "heli", "heli" }
PatrolTeams = { PT1, PT2, PT3, PT1, PT2, PT3 }

DT1 = { "vehicle", "ftnk", "ltnk", "ltnk", "arty", "arty", "aircraft", "heli", "heli", "heli" }
DT2 = { "e5", "e5", "e5", "e3", "e3", "vehicle", "arty", "arty", "bggy", "bggy", "bike" }
DT3 = { "vehicle", "bike", "bike", "bike", "bggy", "bggy", "aircraft", "heli", "heli" }
DefenseTeams = { DT1, DT2, DT3, DT1, DT2, DT3 }

ST1 = { "vehicle", "stnk", "stnk" }
ST2 = { "vehicle", "stnk", "stnk", "stnk" }
StealthTeams = { ST1, ST2, ST1, ST2 }

AP1 = { waypoint10.Location, waypoint7.Location, waypoint14.Location }
AP2 = { waypoint5.Location, waypoint7.Location, waypoint14.Location }
AP3 = { waypoint10.Location, waypoint7.Location, waypoint8.Location }
AP4 = { waypoint5.Location, waypoint7.Location, waypoint8.Location }
AP5 = { waypoint10.Location, waypoint7.Location, waypoint9.Location, waypoint2.Location, waypoint8.Location }
AP6 = { waypoint5.Location, waypoint7.Location, waypoint9.Location, waypoint2.Location, waypoint8.Location }
AttackPaths = { AP1, AP2, AP3, AP4, AP5, AP6 }

PatrolPath = { waypoint13.Location, waypoint6.Location, waypoint2.Location, waypoint9.Location }

SP1 = { waypoint7.Location, CPos.New(14,29), CPos.New(15,15), CPos.New(49,14), CPos.New(60,38), waypoint4.Location }
SP2 = { waypoint7.Location, CPos.New(14,29), CPos.New(15,15), CPos.New(27,13), waypoint9.Location }
SneakPaths = { SP1 }

ABP1 = { CPos.New(41,22), CPos.New(60,22), CPos.New(60,43), waypoint4.Location }
ABP2 = { CPos.New(28,27), waypoint10.Location, waypoint11.Location, waypoint12.Location }
ApacheBackdoorPaths = { ABP1, ABP2, ABP1, ABP2 }

InitAI = function()
	Utils.Do(NodBase, function(b)
		GuardBuilding(b)
		Trigger.OnKilled(b, function(bd)
			AddToRebuildQueue(bd)
			CheckBase()
		end)
	end)
	RepairNamedActors(Nod, 0.75)
	AiProcsNumber = 2
	if ProcUpgrade then
		ProcUpg = Actor.Create(ProcUpgrade, true, { Owner = Nod })
	end
	AiAnyhqPrerequisite = Actor.Create("AiAnyhqPrerequisite", true, { Owner = Nod })
	AiTmplPrerequisite = Actor.Create("AiTmplPrerequisite", true, { Owner = Nod })
	Trigger.OnKilled(NodHand, function()
		ProductionQueue["infantry"] = { }
		CheckTeamCompleted()
	end)
	Trigger.OnKilled(NodAfld, function()
		ProductionQueue["vehicle"] = { }
		CheckTeamCompleted()
	end)
	Trigger.OnKilled(NodHpad1, function()
		ProductionQueue["aircraft"] = { }
		CheckTeamCompleted()
	end)
	StartGuard()
	Trigger.OnAnyKilled(InitPatrolTeam, function()
		InitPatrolTeamAlive = false
	end)
	Trigger.AfterDelay(DateTime.Seconds(ProduceBuildingsDelay), function()
		CheckBase()
		Trigger.AfterDelay(DateTime.Seconds(NukeDelay), function()
			LaunchNuke(true)
		end)
		Trigger.AfterDelay(DateTime.Seconds(ProduceUnitsDelay), function()
			if InitPatrolTeamAlive then
				StartPatrol()
			else
				table.insert(UniqueTeamsQueue, { team = PatrolTeams, job = "patrol" })
			end
			if Difficulty == "easy" then
				StealthTeams = { ST1 }
			end
			table.insert(UniqueTeamsQueue, { team = StealthTeams, job = "sneakAttack" })
			Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(90,270) + ProductionCooldownSeconds), ApacheBackdoor)
			CheckProduction()
			ReduceProdCD()
			ApacheGuard(ApacheG, NodHpad2, CPos.New(3,32), CPos.New(25,60), true)
			ApacheGuard(OutpostApacheG, NodOutPostHpad, CPos.New(24,18), CPos.New(50,38), true)
		end)
	end)
end

-- Ai units logic
ApacheBackdoor = function()
	if NodOutPostHpad.IsDead or NodOutPostHpad.Owner ~= Nod then
		return
	end
	if not CheckProduction then
		Trigger.AfterDelay(DateTime.Seconds(10), ApacheBackdoor)
	end
	local path = Utils.Random(ApacheBackdoorPaths)
	NodOutPostHpad.Build({ "heli", "heli" }, function(team)
		Utils.Do(team, function(h)
			h.Stance = "AttackAnything"
			for i = 1, #path do
				if i < #path then
					h.Move(path[i], 2)
				else
					h.AttackMove(path[i], 2)
				end
			end
			IdleHunt(h)
		end)
	end)
	Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(90,270) + ProductionCooldownSeconds), ApacheBackdoor)
end

ApacheGuard = function(apache, hpad, topleft, botright, init)
	if apache.IsDead then
		return
	end
	if init then
		Trigger.OnKilled(apache, function()
			Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(120,220)), function()
				ApacheRebuild(hpad, topleft, botright)
			end)
		end)
	end
	local targets = Map.ActorsInBox(Map.CenterOfCell(topleft), Map.CenterOfCell(botright), function(a) return a.Owner == GDI and a.Type ~= "camera.small" end)
	if #targets > 0 then
		apache.Hunt()
	else
		if not hpad.IsDead then
			apache.Stop()
			apache.ReturnToBase(hpad)
		end
	end
	Trigger.AfterDelay(Utils.RandomInteger(25,50), function()
		ApacheGuard(apache, hpad, topleft, botright, false)
	end)
end

ApacheRebuild = function(hpad, topleft, botright)
	if hpad.IsDead and hpad.Owner == Nod then
		return
	end
	if CheckProduction() then
		hpad.Build({ "heli" }, function(h)
			ApacheGuard(h[1], hpad, topleft, botright, true)
		end)
	else
		Trigger.AfterDelay(250, function()
			ApacheRebuild(hpad, topleft, botright)
		end)
	end
end

GuardBuilding = function(building)
	Trigger.OnDamaged(building, function(_, atk, _)
		if atk.Type ~= "player" and not atk.IsDead and atk.Owner == GDI then
			Utils.Do(BaseDefenseTeam, function(guard)
				if not guard.IsDead and not building.IsDead then
					if guard.Stance == "Defend" then
						guard.Stop()
						guard.Stance = "AttackAnything"
						guard.AttackMove(atk.Location, 3)
						Trigger.OnIdle(guard, function()
							guard.AttackMove(CPos.New(10,48))
							guard.Stance = "Defend"
							Trigger.ClearAll(guard)
						end)
					end
				end
			end)
		end
	end)
end

MoveAsGroup = function(team, path, i, loop)
	if i == 1 and not loop then
		Utils.Do(team, function(u)
			Trigger.OnDamaged(u, function(_, atk, _)
				if atk.Owner == GDI then
					Trigger.AfterDelay(2, function()
						Utils.Do(team, function(u)
							if not u.IsDead then
								Trigger.Clear(u, "OnDamaged")
								IdleHunt(u)
							end
						end)
					end)
				end
			end)
		end)
	end
	Utils.Do(team, function(a)
		if not a.IsDead then
			a.Stance = "AttackAnything"
			a.AttackMove(path[i], 3)
			Trigger.OnIdle(a, function()
				Trigger.Clear(a, "OnIdle")
				a.Stance = "Defend"
				local ii = 0
				local regrouped = false
				Utils.Do(team, function(a)
					if a.IsDead or a.Stance == "Defend" then
						ii = ii + 1
						if ii == #team then
							regrouped = true
						end
					end
				end)
				if regrouped then
					if i == #path then
						if loop == true then
							i = 1
						else
							Trigger.AfterDelay(5, function()
								Utils.Do(team, function(a)
									if not a.IsDead then
										a.Stance = "AttackAnything"
										IdleHunt(a)
									end
								end)
							end)
							return
						end
					else
						i = i + 1
					end
					Trigger.AfterDelay(20, function()
						MoveAsGroup(team, path, i, loop)
					end)
				end
			end)
		end
	end)
end

SendStealthTanks = function()
	Trigger.OnAllKilled(StealthTeam, function()
		Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(90,270) + ProductionCooldownSeconds), function()
			table.insert(UniqueTeamsQueue, { team = StealthTeams, job = "sneakAttack" })
		end)
	end)
	local SneakPath = Utils.Random(SneakPaths)
	Utils.Do(StealthTeam, function(u)
		Trigger.OnDamaged(u, function()
			u.Stop()
			u.Hunt()
		end)
		u.Stance = "AttackAnything"
		for i = 1, #SneakPath do
			if i < #SneakPath then
				u.Move(SneakPath[i], 2)
			else
				u.AttackMove(SneakPath[i], 2)
			end
		end
		IdleHunt(u)
	end)
end

StartGuard = function()
	Trigger.OnAllKilled(BaseDefenseTeam, function()
		table.insert(UniqueTeamsQueue, { team = DefenseTeams, job = "defend" })
	end)
	Utils.Do(BaseDefenseTeam, function(guard)
		guard.Stance = "Defend"
		Trigger.OnKilled(guard, function()
			if #Utils.Where(BaseDefenseTeam, function(g) return g.IsDead == false end) < 6 then
				Trigger.AfterDelay(5, function()
					Utils.Do(BaseDefenseTeam, function(a)
						if not a.IsDead then
							a.Stance = "AttackAnything"
							Trigger.Clear(a, "OnKilled")
							IdleHunt(a)
						end
					end)
				end)
			end
		end)
	end)
end

StartPatrol = function()
	Trigger.OnAllKilled(PatrolTeam, function()
		Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(60, 150)), function()
			table.insert(UniqueTeamsQueue, { team = PatrolTeams, job = "patrol" })
		end)
	end)
	Utils.Do(PatrolTeam, function(a)
		a.Move(CPos.New(3,28))
		Trigger.OnKilled(a, function()
			Utils.Do(PatrolTeam, function(a)
				if not a.IsDead then
					a.Stop()
					IdleHunt(a)
				end
			end)
		end)
	end)
	MoveAsGroup(PatrolTeam, PatrolPath, 1, true)
end

-- Building logic
AddToRebuildQueue = function(b)
	local index = #NodRebuildList + 1
	if b.Type == "proc" then
		index = 1
	elseif Utils.Any(ProductionTypes, function(pt) return pt == b.Type end) then
		for i = 1, #NodRebuildList do
			if NodRebuildList[i].type ~= "proc" then
				index = i
				break
			end
		end
	elseif Utils.Any(PowerTypes, function(pt) return pt == b.Type end) then
		for i = 1, #NodRebuildList do
			local lastIndex = #NodRebuildList - (i - 1)
			if NodRebuildList[lastIndex].type ~= "silo" or lastIndex == 1 then
				index = lastIndex
				break
			end
		end
	elseif b.Type ~= "silo" then
		for i = 1, #NodRebuildList do
			local lastIndex = #NodRebuildList - (i - 1)
			if NodRebuildList[lastIndex].type ~= "silo" and Utils.Any(PowerTypes, function(pt) return pt == b.Type end) or lastIndex == 1 then
				index = lastIndex
				break
			end
		end
	end
	table.insert(NodRebuildList, index, { type = b.Type, pos = b.Location, power = b.Power, blocked = false })
end

CheckBuildablePlace = function(b)
	local blockingActors = Map.ActorsInBox(WPos.New(b.pos.X * 1024, b.pos.Y * 1024, 0), WPos.New((b.pos.X + BuildingSizes[b.type].X) * 1024, (b.pos.Y + BuildingSizes[b.type].Y) * 1024, 0), function(a) return a.Owner == GDI end)
	if #blockingActors > 0 then
		b.blocked = true
		return false
	else
		b.blocked = false
		return true
	end
end

CheckBase = function()

	if Utils.All(NodCYards, function(cy) return cy.IsDead or cy.Owner ~= Nod end) then
		return
	end

	local queuedProcs = 0
	for i = 1, #NodRebuildList do
		if queuedProcs >= AiProcsNumber and Nod.Resources < 4000 then
			break
		elseif NodRebuildList[i].type == "proc" then
			queuedProcs = queuedProcs + 1
		end

		if NodRebuildList[i].blocked then
			CheckBuildablePlace(NodRebuildList[i])
		elseif Nod.PowerProvided - Nod.PowerDrained + NodRebuildList[i].power > 0 or Utils.Any(PowerTypes, function(pt) return pt == NodRebuildList[i].type end) or NodRebuildList[i].type == "proc" then
			BuildBuilding(NodRebuildList[i], NodCYards)
			break
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

BuildBuilding = function(building, cyards)
	if CyardIsBuilding or Nod.Resources < Actor.Cost(building.type) then
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
	Nod.Resources = Nod.Resources - Actor.Cost(building.type)

	Trigger.AfterDelay(Actor.BuildTime(building.type), function()
		CyardIsBuilding = false

		if Utils.All(cyards, function(cy) return cy.IsDead or cy.Owner ~= Nod end) then
			Nod.Resources = Nod.Resources + Actor.Cost(building.type)
			return
		end

		if CheckBuildablePlace(building) == false then
			CheckBase()
			return
		end

		local newBuilding = Actor.Create(building.type, true, { Owner = Nod, Location = building.pos })
		for i = 1, #NodRebuildList do
			if NodRebuildList[i].pos == building.pos then
				table.remove(NodRebuildList, i)
				break
			end
		end
		Trigger.OnKilled(newBuilding, function(b)
			AddToRebuildQueue(b)
			CheckBase()
		end)
		RepairBuilding(Nod, newBuilding, 0.75)
		GuardBuilding(newBuilding)

		if newBuilding.Type == "hand" then
			ProductionBuildings["infantry"] = newBuilding
			Trigger.OnKilled(newBuilding, function()
				ProductionQueue["infantry"] = { }
				CheckTeamCompleted()
			end)

			RestartUnitProduction()
		elseif newBuilding.Type == "afld" then
			ProductionBuildings["vehicle"] = newBuilding
			Trigger.OnKilled(newBuilding, function()
				ProductionQueue["vehicle"] = { }
				CheckTeamCompleted()
				WaitingAirDrop = false
			end)
			RestartUnitProduction()
			NeedHarv = false
		elseif newBuilding.Type == "hpad" then
			ProductionBuildings["aircraft"] = newBuilding
			Trigger.OnKilled(newBuilding, function()
				ProductionQueue["aircraft"] = { }
				CheckTeamCompleted()
			end)
			RestartUnitProduction()
		end

		Trigger.AfterDelay(50, function() CheckBase() end)
	end)

end

-- Units production logic
UniqueTeamsQueue = { }
ProductionCooldown = false
CheckProduction = function()
	if Utils.All(ProductionBuildings, function(b) return b.IsDead end) then
		return
	elseif #Nod.GetActorsByType("proc") < 1 and Nod.Resources < 6000 then
		RestartUnitProduction()
		return
	elseif not ProductionBuildings["vehicle"].IsDead and not ProductionBuildings["vehicle"].IsProducing("harv") and CheckForHarvester() then
		NeedHarv = true
		WaitingAirDrop = true
		ProductionBuildings["vehicle"].Build({ "harv" }, function()
			Trigger.AfterDelay(2, function()
				WaitingAirDrop = false
			end)
			CheckProduction()
			if Utils.RandomInteger(0,2) == 1 then
				Trigger.AfterDelay(DateTime.Seconds(5), function()
					if not ProductionBuildings["vehicle"].IsDead then
						ProductionBuildings["vehicle"].Build({ "harv" })
					end
				end)
			end
		end)
		RestartUnitProduction()
		return
	end
	NeedHarv = false

	if Producing and #ProductionQueue["infantry"] + #ProductionQueue["vehicle"] + #ProductionQueue["aircraft"] < 1 then
		Producing = false
	end
	if ProductionCooldown and #UniqueTeamsQueue < 1 or Nod.Resources < 4000 then
		RestartUnitProduction()
		return false
	else
		ProduceUnits()
		return true
	end
end

ReduceProdCD = function()
	Trigger.AfterDelay(DateTime.Minutes(2), function()
		ProductionCooldownSeconds = ProductionCooldownSeconds - 1
		if ProductionCooldownSeconds > 10 then
			ReduceProdCD()
		end
	end)
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

	if #ProductionQueue["infantry"] > 0 and not ProductionBuildings["infantry"].IsDead and not ProductionBuildings["infantry"].IsProducing("e1") then
		ProductionBuildings["infantry"].Build({ ProductionQueue["infantry"][1] }, function(u)
			table.insert(NewTeam, u[1])
			table.remove(ProductionQueue["infantry"], 1)
			CheckTeamCompleted()
		end)
	end
	if #ProductionQueue["vehicle"] > 0 and not ProductionBuildings["vehicle"].IsDead and not ProductionBuildings["vehicle"].IsProducing("harv") and not WaitingAirDrop then
		ProductionBuildings["vehicle"].Build({ ProductionQueue["vehicle"][1] }, function(u)
			if u[1].Type == "harv" then
				return
			end
			table.insert(NewTeam, u[1])
			table.remove(ProductionQueue["vehicle"], 1)
			WaitingAirDrop = false
			CheckTeamCompleted()
		end)
		WaitingAirDrop = true
	end
	if #ProductionQueue["aircraft"] > 0 and not ProductionBuildings["aircraft"].IsDead and not ProductionBuildings["aircraft"].IsProducing("tran") then
		ProductionBuildings["aircraft"].Build({ ProductionQueue["aircraft"][1] }, function(u)
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
			ProductionCooldown = true
			Trigger.AfterDelay(DateTime.Seconds(ProductionCooldownSeconds), function() ProductionCooldown = false end)
		elseif TeamJob == "patrol" then
			TeamJob = "attack"
			PatrolTeam = NewTeam
			StartPatrol()
		elseif TeamJob == "defend" then
			TeamJob = "attack"
			BaseDefenseTeam = NewTeam
			StartGuard()
		elseif TeamJob == "sneakAttack" then
			TeamJob = "attack"
			StealthTeam = NewTeam
			SendStealthTanks()
		end
		Producing = false
		NewTeam = { }
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
		elseif ProductionBuildings[pb] and not ProductionBuildings[pb].IsDead and ProductionBuildings[pb].Owner == Nod then
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
	local harv = Nod.GetActorsByType("harv")
	return #harv < MinHarvs
end
