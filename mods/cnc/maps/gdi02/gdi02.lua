nodInBaseTeam = { RushBuggy, RushRifle1, RushRifle2, RushRifle3 }
MobileConstructionVehicle = { "mcv" }
EngineerReinforcements = { "e6", "e6", "e6" }
VehicleReinforcements = { "jeep" }

AttackerSquadSize = 3

ReinforceWithLandingCraft = function(units, transportStart, transportUnload, rallypoint)
	local transport = Actor.Create("oldlst", true, { Owner = player, Facing = 0, Location = transportStart })
	local subcell = 0
	Utils.Do(units, function(a)
		transport.LoadPassenger(Actor.Create(a, false, { Owner = transport.Owner, Facing = transport.Facing, Location = transportUnload, SubCell = subcell }))
		subcell = subcell + 1
	end)

	transport.ScriptedMove(transportUnload)

	transport.CallFunc(function()
		Utils.Do(units, function()
			local a = transport.UnloadPassenger()
			a.IsInWorld = true
			a.MoveIntoWorld(transport.Location - CVec.New(0, 1))

			if rallypoint ~= nil then
				a.Move(rallypoint)
			end
		end)
	end)

	transport.Wait(5)
	transport.ScriptedMove(transportStart)
	transport.Destroy()
end

Reinforce = function(units)
	Media.PlaySpeechNotification(player, "Reinforce")
	ReinforceWithLandingCraft(units, lstStart.Location, lstEnd.Location)
end

BridgeheadSecured = function()
	Reinforce(MobileConstructionVehicle)
	Trigger.AfterDelay(DateTime.Seconds(15), NodAttack)
	Trigger.AfterDelay(DateTime.Seconds(30), function() Reinforce(EngineerReinforcements) end)
	Trigger.AfterDelay(DateTime.Seconds(120), function() Reinforce(VehicleReinforcements) end)
end

NodAttack = function()
	local nodUnits = enemy.GetGroundAttackers()
	if #nodUnits > AttackerSquadSize * 2 then
		local attackers = Utils.Skip(nodUnits, #nodUnits - AttackerSquadSize)
		Utils.Do(attackers, function(unit)
			unit.AttackMove(waypoint2.Location)
			Trigger.OnIdle(unit, unit.Hunt)
		end)
		Trigger.OnAllKilled(attackers, function() Trigger.AfterDelay(DateTime.Seconds(15), NodAttack) end)
	end
end

WorldLoaded = function()
	player = Player.GetPlayer("GDI")
	enemy = Player.GetPlayer("Nod")

	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)
	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerWon(player, function()
		Media.PlaySpeechNotification(player, "Win")
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlayMovieFullscreen("flag.vqa")
		end)
	end)

	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "Lose")
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlayMovieFullscreen("gameover.vqa")
		end)
	end)

	nodObjective = enemy.AddPrimaryObjective("Destroy all GDI troops")
	gdiObjective1 = player.AddPrimaryObjective("Eliminate all Nod forces in the area")
	gdiObjective2 = player.AddSecondaryObjective("Capture the Tiberium Refinery")

	-- Work around limitations with the yaml merger that prevent MustBeDestroyed from working on the silos
	siloARemoved = false
	Trigger.OnCapture(SiloA, function() siloARemoved = true end)
	Trigger.OnKilled(SiloA, function() siloARemoved = true end)

	siloBRemoved = false
	Trigger.OnCapture(SiloB, function() siloBRemoved = true end)
	Trigger.OnKilled(SiloB, function() siloBRemoved = true end)

	Trigger.OnCapture(NodRefinery, function() player.MarkCompletedObjective(gdiObjective2) end)
	Trigger.OnKilled(NodRefinery, function() player.MarkFailedObjective(gdiObjective2) end)

	Trigger.OnAllKilled(nodInBaseTeam, BridgeheadSecured)
end

Tick = function()
	if player.HasNoRequiredUnits() then
		enemy.MarkCompletedObjective(nodObjective)
	end
	if enemy.HasNoRequiredUnits() and siloARemoved and siloBRemoved then
		player.MarkCompletedObjective(gdiObjective1)
	end
end
