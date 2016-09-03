IntroAttackers = { IntroSoldier1, IntroSoldier2, IntroSoldier3 }

BridgeShroudTrigger = { CPos.New(63, 71), CPos.New(64, 71), CPos.New(65, 71), CPos.New(69, 65), CPos.New(70, 65), CPos.New(71, 65) }
BridgeExplosionTrigger = { CPos.New(66, 69), CPos.New(67, 69), CPos.New(68, 69) }
TransportTrigger = { CPos.New(75, 58) }
EnemyBaseShroudTrigger = { CPos.New(64, 52), CPos.New(64, 53), CPos.New(64, 54), CPos.New(64, 55), CPos.New(64, 56), CPos.New(64, 57), CPos.New(64, 58), CPos.New(64, 59), CPos.New(64, 60), CPos.New(64, 61), CPos.New(64, 62), CPos.New(64, 63), CPos.New(64, 64) }
ParachuteTrigger = { CPos.New(80, 66), CPos.New(81, 66), CPos.New(82, 66), CPos.New(83, 66), CPos.New(84, 66), CPos.New(85, 66),CPos.New(86, 66), CPos.New(87, 66), CPos.New(88, 66), CPos.New(89, 66) }
EnemyBaseEntranceShroudTrigger = { CPos.New(80, 73), CPos.New(81, 73), CPos.New(82, 73), CPos.New(83, 73), CPos.New(84, 73), CPos.New(85, 73),CPos.New(86, 73), CPos.New(87, 73), CPos.New(88, 73), CPos.New(89, 73) }

SendUSSRParadrops = function()
	paraproxy1 = Actor.Create("powerproxy.paratroopers", false, { Owner = player })
	paraproxy1.SendParatroopers(ParachuteBaseEntrance.CenterPosition, false,  Facing.North)
	paraproxy1.Destroy()
end

SendUSSRParadropsBase = function()
	paraproxy2 = Actor.Create("powerproxy.paratroopers2", false, { Owner = player })
	paraproxy2.SendParatroopers(ParachuteBase1.CenterPosition, false, Facing.East)
	paraproxy2.Destroy()
	paraproxy3 = Actor.Create("powerproxy.paratroopers3", false, { Owner = player })
	paraproxy3.SendParatroopers(ParachuteBase2.CenterPosition, false, Facing.East)
	paraproxy3.Destroy()
end

Trigger.OnEnteredFootprint(BridgeShroudTrigger, function(a, id)
	if not bridgeShroudTrigger and a.Owner == player then
		bridgeShroudTrigger = true
		local cameraBridge = Actor.Create("camera", true, { Owner = player, Location = CameraBridge.Location })
		Trigger.AfterDelay(DateTime.Seconds(15), function()
			cameraBridge.Destroy()
		end)
	end
end)

Trigger.OnEnteredFootprint(BridgeExplosionTrigger, function(a, id)
	if not bridgeExplosionTrigger and a.Owner == player then
		bridgeExplosionTrigger = true
		if not BarrelBridge.IsDead then
			BarrelBridge.Kill()
		end
	end
end)

Trigger.OnEnteredFootprint(EnemyBaseEntranceShroudTrigger, function(a, id)
	if not enemyBaseEntranceShroudTrigger and a.Owner == player then
		enemyBaseEntranceShroudTrigger = true
		local cameraBaseEntrance = Actor.Create("camera", true, { Owner = player, Location = CameraBaseEntrance.Location })
		Trigger.AfterDelay(DateTime.Seconds(15), function()
			cameraBaseEntrance.Destroy()
		end)
	end
end)

Trigger.OnEnteredFootprint(EnemyBaseShroudTrigger, function(a, id)
	if not enemyBaseShroudTrigger and a.Owner == player then
		enemyBaseShroudTrigger = true
		local cameraBase1 = Actor.Create("camera", true, { Owner = player, Location = CameraBase1.Location })
		local cameraBase2 = Actor.Create("camera", true, { Owner = player, Location = CameraBase2.Location })
		Trigger.AfterDelay(DateTime.Seconds(15), function()
			cameraBase1.Destroy()
			cameraBase2.Destroy()
		end)
	end
end)

Trigger.OnEnteredFootprint(ParachuteTrigger, function(a, id)
	if not parachuteTrigger and a.Owner == player then
		parachuteTrigger = true
		SendUSSRParadrops()
		Media.PlaySpeechNotification(player, "ReinforcementsArrived")
	end
end)

Trigger.OnEnteredFootprint(TransportTrigger, function(a, id)
	if not transportTrigger and a.Type == "truk" then
		transportTrigger = true
		if not TransportTruck.IsDead then
			TransportTruck.Wait(DateTime.Seconds(5))
			TransportTruck.Move(TransportWaypoint2.Location)
			TransportTruck.Wait(DateTime.Seconds(5))
			TransportTruck.Move(TransportWaypoint3.Location)
			TransportTruck.Wait(DateTime.Seconds(5))
			TransportTruck.Move(TransportWaypoint1.Location)
		end
		Trigger.AfterDelay(DateTime.Seconds(10), function()
			transportTrigger = false
		end)
	end
end)

Trigger.OnKilled(BarrelBase, function()
		SendUSSRParadropsBase()
		Media.PlaySpeechNotification(player, "ReinforcementsArrived")
end)

Trigger.OnKilled(BarrelBridge, function()
	local bridgepart = Map.ActorsInBox(BridgeCheck1.CenterPosition, BridgeCheck2.CenterPosition, function(self) return self.Type == "br1" end)[1]
	if not bridgepart.IsDead then
		bridgepart.Kill()
	end
end)

Trigger.OnKilled(Church1, function()
	Actor.Create("moneycrate", true, { Owner = player, Location = TransportWaypoint3.Location })
end)

Trigger.OnKilled(Church2, function()
	Actor.Create("healcrate", true, { Owner = player, Location = Church2.Location })
end)

Trigger.OnKilled(ForwardCommand, function()
	enemy.MarkCompletedObjective(alliedObjective)
end)

Trigger.OnKilled(IntroSoldier1, function()
	local cameraIntro = Actor.Create("camera", true, { Owner = player, Location = CameraStart.Location })
	Trigger.AfterDelay(DateTime.Seconds(15), function()
		cameraIntro.Destroy()
	end)
end)

WorldLoaded = function()
	player = Player.GetPlayer("USSR")
	enemy = Player.GetPlayer("Germany")
	Utils.Do(IntroAttackers, function(actor)
		if not actor.IsDead then
			Trigger.OnIdle(actor, actor.Hunt)
		end
	end)
	Trigger.AfterDelay(0, function()
		local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == enemy and self.HasProperty("StartBuildingRepairs") end)
		Utils.Do(buildings, function(actor)
			Trigger.OnDamaged(actor, function(building, attacker)
				if building.Owner == enemy and building.Health < building.MaxHealth * 0.8 then
					building.StartBuildingRepairs()
				end
			end)
		end)
	end)
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
	end)
	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "Lose")
	end)
	alliedObjective = enemy.AddPrimaryObjective("Destroy all Soviet troops.")
	sovietObjective1 = player.AddPrimaryObjective("Protect the Command Center.")
	sovietObjective2 = player.AddPrimaryObjective("Destroy all Allied units and structures.")
end

Tick = function()
	if player.HasNoRequiredUnits() then
		enemy.MarkCompletedObjective(alliedObjective)
	end

	if enemy.HasNoRequiredUnits() then
		player.MarkCompletedObjective(sovietObjective1)
		player.MarkCompletedObjective(sovietObjective2)
	end

	if enemy.Resources >= enemy.ResourceCapacity * 0.75 then
		enemy.Cash = enemy.Cash + enemy.Resources - enemy.ResourceCapacity * 0.25
		enemy.Resources = enemy.ResourceCapacity * 0.25
	end
end
