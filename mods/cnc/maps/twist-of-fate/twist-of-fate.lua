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

HeliTransEngineers = { "e6", "e6", "e6", "e6", "e6" }

CenterNodBaseEntrance = { CPos.New(35,22), CPos.New(33,23), CPos.New(34,23), CPos.New(35,23), CPos.New(36,23), CPos.New(33,24), CPos.New(34,24), CPos.New(35,24), CPos.New(33,25), CPos.New(34,25), CPos.New(32,27), CPos.New(33,27), CPos.New(32,28), CPos.New(33,28), CPos.New(31,29), CPos.New(32,29), CPos.New(33,29), CPos.New(31,30), CPos.New(32,30), CPos.New(33,30) }

GoDemolitionMan = function(rmbo)
	if rmbo.IsDead then
		return
	end
	local structures = Utils.Where(Map.ActorsInCircle(rmbo.CenterPosition , WDist.FromCells(6)), function(u) return u.HasProperty("StartBuildingRepairs") and u.Owner == Player1 end)
	if #structures > 0 then
		rmbo.Stop()
		rmbo.Demolish(Utils.Random(structures))
		rmbo.Hunt()
		Trigger.AfterDelay(DateTime.Seconds(10), function() GoDemolitionMan(rmbo) end)
	end
end

SendEngTeam = function()
	local cargo = Reinforcements.ReinforceWithTransport(EnemyAi, "tran", HeliTransEngineers, { CPos.New(0,32), waypoint11.Location }, { CPos.New(0,32) })[2]
	Utils.Do(cargo, function(engs)
		engs.Move(CPos.New(39,52))
		Trigger.OnIdle(engs, CaptureStructures)
	end)
end

CaptureStructures = function(actor)
	local structures = Player1.GetActorsByTypes(CapturableStructures)
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
	local targets = Player1.GetActorsByTypes({ "nuk2", "atwr", "weap", "proc" })
	if #targets < 1 then
		targets = Player1.GetGroundAttackers()
	end
	if not NodTmpl.IsDead then
		Media.PlaySpeechNotification(Player1, "NuclearWarheadApproaching")
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
	local target = GetAirstrikeTarget(Player1)
	if target then
		NodAirSupport.TargetAirstrike(target, Angle.SouthEast)
		if loop then
			Trigger.AfterDelay(DateTime.Seconds(AstkDelay), function() SendNodAirstrike(true) end)
		end
	else
		Trigger.AfterDelay(DateTime.Seconds(20), function() SendNodAirstrike(loop) end)
	end
end

CheckObjectives = function()
	if EnemyAi.HasNoRequiredUnits() then Player1.MarkCompletedObjective(EliminateAllNod) end
	if Player1.HasNoRequiredUnits() then Player1.MarkFailedObjective(EliminateAllNod) end
	Trigger.AfterDelay(50, CheckObjectives)
end

WorldLoaded = function()

	Camera.Position = DefaultCameraPosition.CenterPosition
	Player1 = Player.GetPlayer("GDI")
	EnemyAi = Player.GetPlayer("Nod")

	if Difficulty == "hard" then
		ProduceBuildingsDelay = 50
		ProduceUnitsDelay = 10
		ProductionCooldownSeconds = 30
		MinHarvs = 3
		AstkDelay = 180
		NukeDelay = 900
		ProcUpgrade = "AIHProcUpgrade"
		EnemyAi.Resources = 3000
		Player1.Cash = 3000
		MCVReinf = { "mcv" }
	end

	if Difficulty == "normal" then
		ProduceBuildingsDelay = 70
		ProduceUnitsDelay = 15
		ProductionCooldownSeconds = 55
		MinHarvs = 3
		AstkDelay = 220
		NukeDelay = 1200
		EnemyAi.Resources = 3000
		Player1.Cash = 4000
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
		EnemyAi.Resources = 2000
		Player1.Cash = 5000
		MCVReinf = { "mtnk", "mtnk", "mcv", "mtnk" }
		Actor137.Teleport(CPos.New(57,6))
		Actor203.Destroy()
	end

	InitObjectives(Player1)
	ClearPath = AddPrimaryObjective(Player1, "clear-path")
	EliminateAllNod = AddPrimaryObjective(Player1, "eliminate-nod")

	InitAI()
	CheckObjectives()

	Flare = Actor.Create("flare", true, { Owner = Player1, Location = DefaultFlareLocation.Location })

	Trigger.AfterDelay(1,function()
		AmbushTeam = Map.ActorsInBox(Map.CenterOfCell(CPos.New(46,5)), Map.CenterOfCell(CPos.New(60,53)), function(a) return a.Owner == EnemyAi and a.Type ~= "stnk" end)
		Trigger.OnAllKilled(AmbushTeam, function()
			Player1.MarkCompletedObjective(ClearPath)
			Trigger.AfterDelay(50, function()
				Trigger.AfterDelay(750, function()
					Flare.Destroy()
				end)
				Media.PlaySpeechNotification(Player1, "Reinforce")
				Reinforcements.Reinforce(Player1, MCVReinf, { CPos.New(56,2), waypoint0.Location }, 25, function(a)
					a.Move(waypoint1.Location, 2)
					a.Move(waypoint2.Location, 2)
					a.Move(CPos.New(49,44), 2)
					if a.HasProperty("Deploy") then
						a.Move(CPos.New(48,51))
						a.Deploy()
					end
				end)
				Utils.Do(ClearPathCameras, function(c)
					c.Destroy()
				end)
			end)
		end)
	end)

	NodAirSupport = Actor.Create("Astk.proxy", true, { Owner = EnemyAi })
	Trigger.AfterDelay(DateTime.Seconds(AstkDelay), function()
		SendNodAirstrike(true)
		Trigger.AfterDelay(DateTime.Seconds(20), function()
			Media.DisplayMessage(UserInterface.Translate("air-strikes-intel-report"))
			Trigger.AfterDelay(DateTime.Seconds(5), function()
				CaptureCommCenter = AddSecondaryObjective(Player1, "capture-nod-communications-center")
			end)
		end)
	end)

	Trigger.OnKilledOrCaptured(NodOutPostCYard, function()
		SendNodAirstrike(false)
	end)

	Utils.Do(NodGuns, function(g)
		Trigger.OnKilled(g, function()
			SendEngTeam()
			Utils.Do(NodGuns, function(gun) if not gun.IsDead then Trigger.Clear(gun, "OnKilled") end end)
		end)
	end)

	Trigger.OnEnteredFootprint(CenterNodBaseEntrance, function(a, id)
		if a.Owner == Player1 then
			local cargo = Reinforcements.ReinforceWithTransport(EnemyAi, "tran", { "rmbo" }, { CPos.New(0,32), waypoint12.Location }, { CPos.New(0,32) })[2]
			Trigger.OnIdle(cargo[1], function()
				Trigger.Clear(cargo[1], "OnIdle")
				Media.PlaySpeechNotification(Player1, "BaseAttack")
				Trigger.AfterDelay(5, function() GoDemolitionMan(cargo[1]) end)
			end)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnDamaged(NodCYard, function(_, atk, _)
		if atk.Owner == Player1 and not NukeLaunched then
			NukeLaunched = true
			LaunchNuke(false)
			Trigger.Clear(NodCYard, "OnDamaged")
			Trigger.AfterDelay(1, function()
				RepairBuilding(EnemyAi, NodCYard, 0.75)
				NodCYard.StartBuildingRepairs()
			end)
		end
	end)

	Trigger.OnKilledOrCaptured(NodTmpl, function()
		NukeDelay = 0
	end)

	Trigger.OnCapture(NodAstkHq, function()
		Player1.MarkCompletedObjective(CaptureCommCenter)
		Trigger.ClearAll(NodAstkHq)
		AstkDelay = 0
		Trigger.Clear(NodCYard, "OnDamaged")
		Trigger.AfterDelay(1, function()
			RepairBuilding(EnemyAi, NodCYard, 0.75)
			NodCYard.StartBuildingRepairs()
		end)
		Media.DisplayMessage(UserInterface.Translate("communications-center-captured-sams-located"))
		local activeSams = EnemyAi.GetActorsByType("sam")
		local miniCams = { }
		Utils.Do(activeSams, function(s)
			table.insert(miniCams, Actor.Create("camera.mini", true, { Owner = Player1, Location = s.Location }))
			table.insert(miniCams, Actor.Create("camera.mini", true, { Owner = Player1, Location = CPos.New(s.Location.X + 1, s.Location.Y) }))
		end)
		Trigger.AfterDelay(1, function()
				Utils.Do(miniCams, function(c)
					c.Destroy()
				end)
			end)
		Trigger.AfterDelay(200, function()
			DestroySams = AddSecondaryObjective(Player1, "destroy-sams")
		end)
		Trigger.OnAllKilled(NodSams, function()
			AirSupport = Actor.Create("Astk.proxy", true, { Owner = Player1 })
			Player1.MarkCompletedObjective(DestroySams)
		end)
	end)

	Trigger.OnKilled(NodAstkHq, function()
		if CaptureCommCenter ~= nil then
			Player1.MarkFailedObjective(CaptureCommCenter)
		else
			CaptureCommCenter = AddSecondaryObjective(Player1, "capture-nod-communications-center")
			Player1.MarkFailedObjective(CaptureCommCenter)
		end
		AstkDelay = 0
	end)
end
