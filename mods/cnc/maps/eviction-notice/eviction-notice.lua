--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

WelcomeTeam = { Wel1, Wel2, Wel3, Wel4, Wel5 }

Civilians = { Actor111, Actor112, Actor113, Actor114, Actor115, Actor116, Actor117, Actor118, Actor119, Actor120, Actor121 }
RunForHelpCivs = { Actor109, Actor110 }
CiviliansBuildings = { Actor35, Actor36, Actor37, Actor38, Actor39, Actor40, Actor41, Actor42, Actor43, Actor44, Actor45, Actor54, Actor55, Actor56, Actor57, Actor58 }
OilDerricks = { Actor49, Actor50, Actor51, Actor52 }
CivsMoneyBuildings = { Actor37, Actor41, Actor42, Actor44, Actor58 }

ReinforcementsMammoths = { "htnk", "htnk" }
ReinforcementsEngineers = { "e6", "e6", "e6", "e6", "e6" }
CiviliansHelpTeam = { "mtnk", "mtnk", "msam" }
BaseDefenseTeam = { Def1, Def2, Def3, Def4 }
PatrolTeam = { Actor103, Actor122, Actor123 }
PatrolPath = { waypoint2.Location, waypoint3.Location, waypoint4.Location, waypoint5.Location }

CapturableStructures = { "afld", "hand", "hq", "nuke", "silo", "proc", "sam" }
IonCannonTargets = { { "obli", "gun", "gtwr" }, { "harv" } }

WelcomeTeamCellTrigger = { CPos.New(17,59), CPos.New(18,59), CPos.New(19,59), CPos.New(17,60), CPos.New(18,60), CPos.New(19,60), CPos.New(25,53), CPos.New(25,54), CPos.New(25,55), CPos.New(26,53), CPos.New(26,54), CPos.New(26,55) }
CivsGDIHelpCellTrigger = { CPos.New(13,7) }
GDIBaseEntranceCells = { CPos.New(16,5), CPos.New(16,6), CPos.New(16,7), CPos.New(16,8), CPos.New(17,4), CPos.New(17,5), CPos.New(17,6), CPos.New(17,7), CPos.New(17,8), CPos.New(18,4), CPos.New(18,5), CPos.New(19,4), CPos.New(8,15), CPos.New(9,15), CPos.New(10,15), CPos.New(11,15), CPos.New(12,15), CPos.New(13,15), CPos.New(14,15), CPos.New(9,16), CPos.New(10,16), CPos.New(11,16), CPos.New(12,16), CPos.New(13,16), CPos.New(14,16) }
InnerGDIBaseEntranceCells = { CPos.New(7,5), CPos.New(7,6), CPos.New(7,7), CPos.New(8,6), CPos.New(8,7), CPos.New(9,6), CPos.New(9,7) }

CaptureStructures = function(actor)
	local structures = Nod.GetActorsByTypes(CapturableStructures)
	local distance = 500
	local captst = nil
	Utils.Do(structures, function(st)
		if not actor.IsDead and not st.IsDead and distance > (math.abs((actor.Location - st.Location).X) + math.abs((actor.Location - st.Location).Y)) then
			distance = math.abs((actor.Location - st.Location).X) + math.abs((actor.Location - st.Location).Y)
			captst = st
		end
	end)
	if captst then
		actor.Capture(captst)
	end
end

SendGDIAirstrike = function()
	if not GDIhq.IsDead and GDIhq.Owner == GDI then
		local target = GetAirstrikeTarget(Nod)
		if target then
			GDIhq.TargetAirstrike(target, Angle.SouthEast)
		else
			Trigger.AfterDelay(DateTime.Seconds(5), SendGDIAirstrike)
		end
	end
end

OilDerricksAstkSent = false
OilDerricksAirstrike = function()
	if not OilDerricksAstkSent then
		SendGDIAirstrike()
		OilDerricksAstkSent = true
	end
end

CivsRunning = false
RunForHelp = function()
	if not CivsRunning then
		Utils.Do(RunForHelpCivs, function(actor)
			actor.Move(CPos.New(53,45))
			actor.Move(waypoint6.Location)
			actor.Move(waypoint4.Location)
			actor.Move(waypoint9.Location)
			actor.Move(waypoint2.Location)
			actor.Move(waypoint12.Location)
			actor.Move(waypoint6.Location)
		end)
		Trigger.OnEnteredFootprint(CivsGDIHelpCellTrigger, function(a, id)
			if a == RunForHelpCivs[1] or a == RunForHelpCivs[2] then
				Reinforcements.Reinforce(GDI, CiviliansHelpTeam, { CPos.New(2,9), CPos.New(3,9) }, 30, function(a)
					a.AttackMove(waypoint2.Location)
					a.AttackMove(waypoint9.Location)
					a.AttackMove(waypoint8.Location)
					IdleHunt(a)
				end)
				Trigger.RemoveFootprintTrigger(id)
			end
		end)
		CivsRunning = true
		local cam = Actor.Create("camera", true, { Owner = Nod, Location = CPos.New(53,44) })
		Trigger.AfterDelay(125, cam.Destroy)
		Media.DisplayMessage(UserInterface.Translate("civilians-runs"), UserInterface.Translate("nod-soldier"))
	end
end

CivsBuildingsToDestroy = 0
CheckVillageDestruction = function()
	CivsBuildingsToDestroy = CivsBuildingsToDestroy - 1
	if CivsBuildingsToDestroy == 2 then
		Media.DisplayMessage(UserInterface.Translate("village-destruction-warning"))
	elseif CivsBuildingsToDestroy == 0 then
		Reinforcements.Reinforce(GDI, ReinforcementsMammoths, { CPos.New(2,9), CPos.New(3,9) }, 40, function(a)
			a.AttackMove(waypoint11.Location)
			a.AttackMove(waypoint5.Location)
			a.AttackMove(waypoint4.Location)
			a.AttackMove(waypoint6.Location)
			IdleHunt(a)
		end)
	end
end

GuardBase = function()
	Utils.Do(GDIBase, function(building)
		GuardBuilding(building)
	end)
end

GuardBuilding = function(building)
	Trigger.OnDamaged(building, function(slf, atk, dmg)
		if atk.Type ~= "player" and not atk.IsDead and atk.Owner == Nod then
			Utils.Do(BaseDefenseTeam, function(guard)
				if not guard.IsDead and not building.IsDead then
					if guard.Stance == "Defend" then
						guard.Stop()
						guard.Stance = "AttackAnything"
						guard.AttackMove(atk.Location, 3)
						Trigger.OnIdle(guard, function()
							guard.AttackMove(waypoint12.Location, 3)
							guard.Stance = "Defend"
							Trigger.ClearAll(guard)
						end)
					end
				end
			end)
		end
	end)
end

StartGuard = function()
	Trigger.OnAllKilled(BaseDefenseTeam, function()
		table.insert(UniqueTeamsQueue, { team = DefenseTeams, job = "defend" })
	end)
	Utils.Do(BaseDefenseTeam, function(guard)
		guard.Stance = "Defend"
	end)
end

StartPatrol = function()
	Trigger.OnAllKilled(PatrolTeam, function()
		table.insert(UniqueTeamsQueue, { team = PatrolTeams, job = "patrol" })
	end)
	Utils.Do(PatrolTeam, function(a)
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

MoveAsGroup = function(team, path, i, loop)
	if i == 1 and not loop then
		Utils.Do(team, function(u)
			Trigger.OnDamaged(u, function()
				Utils.Do(team, function(u)
					if not u.IsDead then
						Trigger.Clear(u, "OnDamaged")
						IdleHunt(u)
					end
				end)
			end)
		end)
	end
	Utils.Do(team, function(a)
		if not a.IsDead then
			a.Stance = "AttackAnything"
			a.AttackMove(path[i], 2)
			Trigger.OnIdle(a, function()
				Trigger.Clear(a, "OnIdle")
				a.Stance = "Defend"
				local teamNumber = 0
				local regrouped = false
				Utils.Do(team, function(a)
					if a.IsDead or a.Stance == "Defend" then
						teamNumber = teamNumber + 1
						if teamNumber == #team then
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

IonCannonOnline = false
ICShotsCount = 0
FireIonCannon = function(timer)
	if IonCannonOnline then
		local ii = Utils.RandomInteger(1, 4)
		local targets = { }
		if ii < 3 then
			targets = Nod.GetActorsByTypes(IonCannonTargets[ii])
		else
			targets = Nod.GetGroundAttackers()
		end
		if #targets > 0 then
			local rand = Utils.RandomInteger(1, #targets + 1)
			if not targets[rand].IsDead then
				GDIAdvComCenter.ActivateIonCannon(targets[rand].Location)
				if ICShotsCount < 2 then
					ICShotsCount = ICShotsCount + 1
					if ICShotsCount > 1 then
						Nod.MarkFailedObjective(DestroyIonCannon)
						Trigger.ClearAll(GDIAdvComCenter)
					end
				end
				Trigger.AfterDelay(DateTime.Seconds(timer), function() FireIonCannon(timer) end)
				return
			end
		end
		Trigger.AfterDelay(DateTime.Seconds(1), function() FireIonCannon(timer) end)
	end
end

CheckObjectives = function()
	if GDI.HasNoRequiredUnits() then Nod.MarkCompletedObjective(EliminateAllGDI) end
	if Nod.HasNoRequiredUnits() then Nod.MarkFailedObjective(EliminateAllGDI) end
	Trigger.AfterDelay(25, CheckObjectives)
end

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")
	Camera.Position = PlayerStart.CenterPosition
	Flare = Actor.Create("flare", true, { Owner = Nod, Location = DefaultFlareLocation.Location })
	Trigger.AfterDelay(DateTime.Minutes(1), Flare.Destroy)
	InitObjectives(Nod)
	EliminateAllGDI = AddPrimaryObjective(Nod, "eliminate-gdi-forces")
	FindAllCivMoney = AddSecondaryObjective(Nod, "take-civilians-money-crates")
	CheckObjectives()
	InitAi()

	Trigger.OnEnteredFootprint(WelcomeTeamCellTrigger, function(a, id)
		if a.Owner == Nod then
			Utils.Do(WelcomeTeam, function(a)
				if not a.IsDead then
					a.AttackMove(PlayerStart.Location)
					IdleHunt(a)
				end
			end)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnAllKilled(Civilians, function()
		local cargo = Reinforcements.ReinforceWithTransport(GDI, "tran", ReinforcementsEngineers, { CPos.New(0,32), waypoint10.Location }, { CPos.New(0,32) })[2]
		Utils.Do(cargo, function(engs)
			if engs.Type == "e6" then
				Trigger.OnIdle(engs, CaptureStructures)
			end
		end)
	end)

	Utils.Do(CiviliansBuildings, function(b)
		Trigger.OnKilled(b, CheckVillageDestruction)
	end)

	Utils.Do(OilDerricks, function(actor)
		Trigger.OnKilledOrCaptured(actor, OilDerricksAirstrike)
	end)

	Utils.Do(RunForHelpCivs, function(actor)
		Trigger.OnDiscovered(actor, RunForHelp)
	end)

	Trigger.OnEnteredFootprint(GDIBaseEntranceCells, function(a, id)
		if a.Owner == Nod and not BombTriggered then
			BombTriggered = true
			Trigger.AfterDelay(25, SendGDIAirstrike)
			Trigger.AfterDelay(150, SendGDIAirstrike)
			Trigger.AfterDelay(275, SendGDIAirstrike)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnCapture(GDIhq, function()
		GDIhq.GrantCondition("captured")
	end)

	Trigger.OnEnteredFootprint(InnerGDIBaseEntranceCells, function(a, id)
		if a.Owner == Nod and not InnerBaseEntered then
			InnerBaseEntered = true
			Reinforcements.Reinforce(GDI, ReinforcementsMammoths, { CPos.New(2,9), CPos.New(3,9) }, 40, IdleHunt)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.AfterDelay(5, function()
		StartPatrol()
		GuardBase()
		StartGuard()
	end)

	Trigger.OnAllKilled(CivsMoneyBuildings, function()
		Trigger.AfterDelay(1, function()
			Trigger.OnAllRemovedFromWorld(Utils.Where(Map.ActorsInWorld, function(a) return a.Type == "moneycrate" or a.Type == "smallmcrate" end), function()
				Nod.MarkCompletedObjective(FindAllCivMoney)
			end)
		end)
	end)

	Trigger.OnKilledOrCaptured(GDIAdvComCenter, function()
		if IonCannonOnline then
			IonCannonOnline = false
		else
			DestroyIonCannon = AddSecondaryObjective(Nod, "quickly-destroy-ion-cannon")
		end
		Nod.MarkCompletedObjective(DestroyIonCannon)
		PrepareOrcas()
	end)
end
