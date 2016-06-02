CameraTriggerArea = { CPos.New(42, 45), CPos.New(43, 45), CPos.New(44, 45), CPos.New(45, 45), CPos.New(46, 45), CPos.New(47, 45), CPos.New(48, 45), CPos.New(48, 56), CPos.New(48, 57), CPos.New(48, 58), CPos.New(48, 59), CPos.New(40, 63), CPos.New(41, 63), CPos.New(42, 63), CPos.New(43, 63), CPos.New(44, 63), CPos.New(45, 63), CPos.New(46, 63), CPos.New(47, 63) }
PassingBridgeLocation = { CPos.New(59, 56), CPos.New(60, 56) }

CmdAtk = { Attacker1, Attacker2, Attacker3, Attacker4 }
FleeingUnits = { Fleeing1, Fleeing2 }
HuntingUnits = { Hunter1, Hunter2, Hunter3, Hunter4 }

WorldLoaded = function()
	player = Player.GetPlayer("USSR")
	germany = Player.GetPlayer("Germany")

	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)
	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	CommandCenterIntact = player.AddPrimaryObjective("Protect the Command Center.")
	DestroyAllAllied = player.AddPrimaryObjective("Destroy all Allied units and structures.")

	Trigger.OnPlayerWon(player, function()
		Media.PlaySpeechNotification(player, "MissionAccomplished")
	end)
	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "MissionFailed")
	end)

	Camera.Position	= CameraWaypoint.CenterPosition

	Trigger.OnKilled(CommandCenter, function()
		player.MarkFailedObjective(CommandCenterIntact)
	end)

	Trigger.AfterDelay(0, function()
		local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == germany and self.HasProperty("StartBuildingRepairs") end)
		Utils.Do(buildings, function(actor)
			Trigger.OnDamaged(actor, function(building, attacker)
				if building.Owner == germany and building.Health < building.MaxHealth * 0.8 then
					building.StartBuildingRepairs()
					if attacker.Type ~= "yak" and not AlreadyHunting then
						AlreadyHunting = true
						Utils.Do(germany.GetGroundAttackers(), function(unit)
							Trigger.OnIdle(unit, unit.Hunt)
						end)
					end
				end
			end)
		end)

		-- Find the bridge actors
		bridgepart1 = Map.ActorsInBox(waypoint23.CenterPosition, waypoint49.CenterPosition, function(self) return self.Type == "br1" end)[1]
		bridgepart2 = Map.ActorsInBox(waypoint23.CenterPosition, waypoint49.CenterPosition, function(self) return self.Type == "br2" end)[1]
	end)

	-- Discover the area around the bridge exposing the two german soldiers
	-- When the two infantry near the bridge are discovered move them across the bridge to waypoint4
	-- in the meanwhile one USSR soldier hunts them down
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Actor.Create("camera", true, { Owner = player, Location = waypoint23.Location })

		Utils.Do(FleeingUnits, function(unit)
			unit.Move(waypoint4.Location)
		end)
		Follower.AttackMove(waypoint4.Location)
	end)

	-- To make it look more smooth we will blow up the bridge when the barrel closest to it blows up
	Trigger.OnAnyKilled({ BridgeBarrel1, BridgeBarrel2 }, function()
		-- Destroy the bridge
		if not bridgepart1.IsDead then
			bridgepart1.Kill()
		end
		if not bridgepart2.IsDead then
			bridgepart2.Kill()
		end
	end)

	-- If player passes over the bridge, blow up the barrel and destroy the bridge
	Trigger.OnEnteredFootprint(PassingBridgeLocation, function(unit, id)
		if unit.Owner == player then
			Trigger.RemoveFootprintTrigger(id)

			-- Also don't if the bridge is already dead
			if bridgepart1.IsDead and bridgepart2.IsDead then
				return
			end

			-- Don't "shoot" at the barrels if there is no-one to shoot
			if not FleeingUnits[1].IsDead then
				FleeingUnits[1].Attack(Barrel, true, true)
			elseif not FleeingUnits[2].IsDead then
				FleeingUnits[2].Attack(Barrel, true, true)
			end
		end
	end)

	-- Four infantry from the small island move towards the USSR command center and attack it after 24 Seconds
	Trigger.AfterDelay(DateTime.Seconds(24), function()
		Utils.Do(CmdAtk, function(unit)
			unit.AttackMove(AttackWaypoint1.Location)
			Trigger.OnIdle(unit, unit.Hunt)
		end)
	end)

	-- Start hunting
	Hunter4.AttackMove(AttackWaypoint2.Location) -- Move the unit in the correct direction first
	Utils.Do(HuntingUnits, function(unit)
		Trigger.OnIdle(unit, unit.Hunt)
	end)

	-- When destroying the allied radar dome or the refinery drop 2 badgers with 5 grenadiers each
	Trigger.OnAnyKilled({ AlliedDome, AlliedProc }, function()
		local powerproxy = Actor.Create("powerproxy.paratroopers", true, { Owner = player })
		powerproxy.SendParatroopers(ParadropLZ.CenterPosition, false, Facing.South)
		powerproxy.SendParatroopers(ParadropLZ.CenterPosition, false, Facing.SouthEast)
		powerproxy.Destroy()
	end)
end

Tick = function()
	if germany.HasNoRequiredUnits() then
		player.MarkCompletedObjective(CommandCenterIntact)
		player.MarkCompletedObjective(DestroyAllAllied)
	end

	if player.HasNoRequiredUnits() then
		player.MarkFailedObjective(DestroyAllAllied)
	end

	if germany.Resources > germany.ResourceCapacity / 2 then
		germany.Resources = germany.ResourceCapacity / 2
	end
end
