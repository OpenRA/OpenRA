--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

CapturableStructures = { "weap", "pyle", "hq", "nuke", "nuk2", "silo", "proc" , "fact" }

ClearPathCameras = { Cam1, Cam2, Cam3, Cam4, Cam5 }
NodSams = { NodSam1, NodSam2, NodSam3, NodSam4, NodSam5, NodSam6, NodSam7 }
NodGuns = { Actor68, Actor69, NodGun4, Actor66 }
OldGDIBase = { OldGDIProc, OldGDIWeap, OldGDIPyle, OldGDIGtwr1, OldGDIGtwr2, OldGDIGtwr3 }

HeliTransEngineers = { "e6", "e6", "e6", "e6", "e6" }

CenterNodBaseEntrance = { CPos.New(35,22), CPos.New(33,23), CPos.New(34,23), CPos.New(35,23), CPos.New(36,23), CPos.New(33,24), CPos.New(34,24), CPos.New(35,24), CPos.New(33,25), CPos.New(34,25), CPos.New(32,27), CPos.New(33,27), CPos.New(32,28), CPos.New(33,28), CPos.New(31,29), CPos.New(32,29), CPos.New(33,29), CPos.New(31,30), CPos.New(32,30), CPos.New(33,30) }

GoDemolitionMan = function(rmbo)
	if rmbo.IsDead then
		return
	end

	local structures = Utils.Where(Map.ActorsInCircle(rmbo.CenterPosition , WDist.FromCells(6)), function(u) return u.HasProperty("StartBuildingRepairs") and u.Owner == GDI end)
	if #structures > 0 then
		rmbo.Stop()
		rmbo.Demolish(Utils.Random(structures))
		rmbo.Hunt()
		Trigger.AfterDelay(DateTime.Seconds(10), function() GoDemolitionMan(rmbo) end)
	end
end

EngineersSent = false
SendEngTeam = function()
	if not EngineersSent then
		EngineersSent = true
		local cargo = Reinforcements.ReinforceWithTransport(Nod, "tran", HeliTransEngineers, { CPos.New(0,32), waypoint11.Location }, { CPos.New(0,32) })[2]
			Utils.Do(cargo, function(engs)
			engs.Move(CPos.New(39,52))
			Trigger.OnIdle(engs, CaptureStructures)
		end)
	end
end

CaptureStructures = function(actor)
	local structures = GDI.GetActorsByTypes(CapturableStructures)
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

LaunchNuke = function(loop)
	if NukeDelay == 0 then
		return
	end

	local targets = GDI.GetActorsByTypes({ "nuk2", "atwr", "weap", "proc" })
	if #targets < 1 then
		targets = GDI.GetGroundAttackers()
	end

	if not NodTmpl.IsDead then
		Media.PlaySpeechNotification(GDI, "NuclearWarheadApproaching")
		NodTmpl.ActivateNukePower(Utils.Random(targets).Location)
	end

	if loop then
		Trigger.AfterDelay(DateTime.Seconds(NukeDelay), function() LaunchNuke(true) end)
	end
end

SendNodAirstrike = function(loop)
	if AstkDelay == 0 then
		return
	end

	local target = GetAirstrikeTarget(GDI)
	if target then
		NodAirSupport.TargetAirstrike(target, Angle.SouthEast)
		if loop then
			Trigger.AfterDelay(DateTime.Seconds(AstkDelay), function() SendNodAirstrike(true) end)
		end
	else
		Trigger.AfterDelay(DateTime.Seconds(20), function() SendNodAirstrike(loop) end)
	end
end

SendEasyReinforcements = function(i)
	local team = { }
	if i < 4 then
		team = ReinforceTeams[i]
	else
		team = ReinforceTeams[Utils.RandomInteger(1, 4)]
	end

	Media.PlaySpeechNotification(GDI, "Reinforce")
	Reinforcements.Reinforce(GDI, team, { CPos.New(56,2), waypoint0.Location }, 25, function(a)
		a.Move(waypoint1.Location, 2)
		a.Move(waypoint2.Location, 2)
		a.Move(CPos.New(49,44), 2)
	end)

	Trigger.AfterDelay(DateTime.Seconds(ReinforceDelay), function()
		SendEasyReinforcements(i + 1)
	end)
end

CheckObjectives = function()
	if Nod.HasNoRequiredUnits() then GDI.MarkCompletedObjective(EliminateAllNod) end
	if GDI.HasNoRequiredUnits() then GDI.MarkFailedObjective(EliminateAllNod) end
	Trigger.AfterDelay(50, CheckObjectives)
end

CompleteCaptureCommCenterObjective = function()
	GDI.MarkCompletedObjective(CaptureCommCenter)
	if not NodCYard.IsDead and NodCYard.Owner == Nod then
		Trigger.Clear(NodCYard, "OnDamaged")
		Trigger.AfterDelay(1, function()
			RepairBuilding(Nod, NodCYard, 0.75)
			NodCYard.StartBuildingRepairs()
		end)
	end

	Media.DisplayMessage(UserInterface.Translate("communications-center-captured-sams-located"))
	local activeSams = Nod.GetActorsByType("sam")
	local miniCams = { }
	if #activeSams > 0 then
		Utils.Do(activeSams, function(s)
			table.insert(miniCams, Actor.Create("camera.mini", true, { Owner = GDI, Location = s.Location }))
			table.insert(miniCams, Actor.Create("camera.mini", true, { Owner = GDI, Location = CPos.New(s.Location.X + 1, s.Location.Y) }))
		end)

		Trigger.AfterDelay(1, function()
			Utils.Do(miniCams, function(c)
				c.Destroy()
			end)
		end)
	end
end

WorldLoaded = function()
	Camera.Position = DefaultCameraPosition.CenterPosition
	GDI = Player.GetPlayer("GDI")
	Nod = Player.GetPlayer("Nod")

	if Difficulty == "hard" then
		ProduceBuildingsDelay = 50
		ProduceUnitsDelay = 10
		ProductionCooldownSeconds = 30
		MinHarvs = 3
		AstkDelay = 180
		NukeDelay = 900
		ProcUpgrade = "AIHProcUpgrade"
		Nod.Resources = 3000
		GDI.Cash = 3000
		MCVReinf = { "mcv" }
	end

	if Difficulty == "normal" then
		ProduceBuildingsDelay = 70
		ProduceUnitsDelay = 15
		ProductionCooldownSeconds = 55
		MinHarvs = 3
		AstkDelay = 220
		NukeDelay = 1200
		Nod.Resources = 3000
		GDI.Cash = 4000
		ProcUpgrade = "AINProcUpgrade"
		MCVReinf = { "mtnk", "mcv" }
	end

	if Difficulty == "easy" then
		ProduceBuildingsDelay = 100
		ProduceUnitsDelay = 35
		ProductionCooldownSeconds = 85
		MinHarvs = 2
		AstkDelay = 250
		NukeDelay = 1500
		Nod.Resources = 2000
		GDI.Cash = 5000
		MCVReinf = { "mtnk", "mtnk", "mcv", "mtnk" }
		RT1 = { "jeep", "jeep", "apc" }
		RT2 = { "mtnk", "msam" }
		RT3 = { "htnk" }
		ReinforceTeams = { RT1, RT2, RT3 }
		ReinforceDelay = 240
		Actor137.Teleport(CPos.New(57,6))
		Actor203.Destroy()
	end

	GDI.PlayLowPowerNotification = false

	InitObjectives(GDI)
	ClearPath = AddPrimaryObjective(GDI, "clear-path")
	Trigger.AfterDelay(DateTime.Seconds(5), function()
		EliminateAllNod = AddPrimaryObjective(GDI, "eliminate-nod")
	end)

	InitAI()
	CheckObjectives()

	Flare = Actor.Create("flare", true, { Owner = GDI, Location = DefaultFlareLocation.Location })

	Trigger.AfterDelay(1, function()
		AmbushTeam = Map.ActorsInBox(Map.CenterOfCell(CPos.New(46,5)), Map.CenterOfCell(CPos.New(60,53)), function(a) return a.Owner == Nod and a.Type ~= "stnk" end)
		Trigger.OnAllKilled(AmbushTeam, function()
			GDI.MarkCompletedObjective(ClearPath)

			Trigger.AfterDelay(DateTime.Seconds(2), function()
				Trigger.AfterDelay(DateTime.Seconds(30), function()
					Flare.Destroy()
				end)

				Media.PlaySpeechNotification(GDI, "Reinforce")
				Reinforcements.Reinforce(GDI, MCVReinf, { CPos.New(56,2), waypoint0.Location }, 25, function(a)
					a.Move(waypoint1.Location, 2)
					a.Move(waypoint2.Location, 2)
					a.Move(CPos.New(49,44), 2)
					if a.HasProperty("Deploy") then
						a.Move(CPos.New(48,51))
						a.Deploy()

						Trigger.OnRemovedFromWorld(a, function()
							if not a.IsDead then
								GDI.PlayLowPowerNotification = true
							end
						end)
					end
				end)

				Utils.Do(ClearPathCameras, function(c)
					c.Destroy()
				end)

				if Difficulty == "easy" then
					Trigger.AfterDelay(DateTime.Seconds(ReinforceDelay), function()
						SendEasyReinforcements(1)
					end)
				end
			end)
			Trigger.AfterDelay(DateTime.Seconds(8), function()
				RecoverOldBase = AddSecondaryObjective(GDI, "recover-old-base")
			end)
		end)
	end)

	NodAirSupport = Actor.Create("Astk.proxy", true, { Owner = Nod })
	Trigger.AfterDelay(DateTime.Seconds(AstkDelay), function()
		if AstkDelay > 0 then
			SendNodAirstrike(true)
			Trigger.AfterDelay(DateTime.Seconds(15), function()
				Media.DisplayMessage(UserInterface.Translate("air-strikes-intel-report"))
				Trigger.AfterDelay(DateTime.Seconds(8), function()
					CaptureCommCenter = AddSecondaryObjective(GDI, "capture-nod-communications-center")
					if NodAstkHq.IsDead then
						GDI.MarkFailedObjective(CaptureCommCenter)
						return
					end

					if NodAstkHq.Owner == GDI then
						Trigger.AfterDelay(DateTime.Seconds(4), CompleteCaptureCommCenterObjective)
					end
				end)
			end)
		end
	end)

	Trigger.OnKilled(NodOutPostCYard, function()
		SendNodAirstrike(false)
		if not RecoverOldBase then
			RecoverOldBase = AddSecondaryObjective(GDI, "recover-old-base")
		end
		GDI.MarkFailedObjective(RecoverOldBase)
	end)

	Trigger.OnCapture(NodOutPostCYard, function()
		Trigger.ClearAll(NodOutPostCYard)
		Utils.Do(OldGDIBase, function(building)
			if not building.IsDead then
				building.Owner = GDI
			end
		end)

		GDI.MarkCompletedObjective(RecoverOldBase)
		table.insert(SneakPaths, SP2)
		table.insert(SneakPaths, SP1)
		table.insert(SneakPaths, SP2)
	end)

	Utils.Do(NodGuns, function(g)
		Trigger.OnKilled(g, function()
			SendEngTeam()
			Utils.Do(NodGuns, function(gun) if not gun.IsDead then Trigger.Clear(gun, "OnKilled") end end)
		end)
	end)

	RamboSent = false
	Trigger.OnEnteredFootprint(CenterNodBaseEntrance, function(a, id)
		if a.Owner == GDI and not RamboSent then
			RamboSent = true
			local cargo = Reinforcements.ReinforceWithTransport(Nod, "tran", { "rmbo" }, { CPos.New(0,32), waypoint12.Location }, { CPos.New(0,32) })[2]
			Trigger.OnIdle(cargo[1], function()
				Trigger.Clear(cargo[1], "OnIdle")
				Media.PlaySpeechNotification(GDI, "BaseAttack")
				Trigger.AfterDelay(5, function() GoDemolitionMan(cargo[1]) end)
			end)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnDamaged(NodCYard, function(_, atk, _)
		if atk.Owner == GDI and not NukeLaunched then
			NukeLaunched = true
			LaunchNuke(false)
			Trigger.Clear(NodCYard, "OnDamaged")
			Trigger.AfterDelay(1, function()
				RepairBuilding(Nod, NodCYard, 0.75)
				NodCYard.StartBuildingRepairs()
			end)
		end
	end)

	Trigger.OnKilledOrCaptured(NodTmpl, function()
		NukeDelay = 0
	end)

	Trigger.OnCapture(NodAstkHq, function()
		AstkDelay = 0
		Trigger.ClearAll(NodAstkHq)
		if CaptureCommCenter then
			CompleteCaptureCommCenterObjective()
		end
	end)

	Trigger.OnKilled(NodAstkHq, function()
		if CaptureCommCenter then
			GDI.MarkFailedObjective(CaptureCommCenter)
		end
		AstkDelay = 0
	end)
end
