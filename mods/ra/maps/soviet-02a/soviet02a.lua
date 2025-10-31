--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
CameraTriggerArea = { CPos.New(42, 45), CPos.New(43, 45), CPos.New(44, 45), CPos.New(45, 45), CPos.New(46, 45), CPos.New(47, 45), CPos.New(48, 45), CPos.New(48, 56), CPos.New(48, 57), CPos.New(48, 58), CPos.New(48, 59), CPos.New(40, 63), CPos.New(41, 63), CPos.New(42, 63), CPos.New(43, 63), CPos.New(44, 63), CPos.New(45, 63), CPos.New(46, 63), CPos.New(47, 63) }
PassingBridgeLocation = { CPos.New(59, 56), CPos.New(60, 56) }

CmdAtk = { Attacker1, Attacker2, Attacker3, Attacker4 }
FleeingUnits = { Fleeing1, Fleeing2 }
HuntingUnits = { Hunter1, Hunter2, Hunter3, Hunter4 }

AttackWaypoints = { AttackWaypoint1, AttackWaypoint2 }
AttackGroup = { }
AttackGroupSize = 3
AlliedInfantry = { "e1", "e1", "e3" }

SendAttackGroup = function()
	if #AttackGroup < AttackGroupSize then
		return
	end

	local way = Utils.Random(AttackWaypoints)
	Utils.Do(AttackGroup, function(unit)
		if not unit.IsDead then
			unit.AttackMove(way.Location)
			Trigger.OnIdle(unit, unit.Hunt)
		end
	end)

	AttackGroup = { }
end

ProduceInfantry = function()
	if Tent.IsDead then
		return
	end

	Greece.Build({ Utils.Random(AlliedInfantry) }, function(units)
		table.insert(AttackGroup, units[1])
		SendAttackGroup()
		Trigger.AfterDelay(DateTime.Seconds(10), ProduceInfantry)
	end)
end

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
	Greece = Player.GetPlayer("Greece")

	InitObjectives(USSR)

	CommandCenterIntact = AddPrimaryObjective(USSR, "protect-command-center")
	DestroyAllAllied = AddPrimaryObjective(USSR, "destroy-allied-units-structures")

	Camera.Position	= CameraWaypoint.CenterPosition

	Trigger.OnKilled(CommandCenter, function()
		USSR.MarkFailedObjective(CommandCenterIntact)
	end)

	Trigger.AfterDelay(0, function()
		local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == Greece and self.HasProperty("StartBuildingRepairs") end)
		Utils.Do(buildings, function(actor)
			Trigger.OnDamaged(actor, function(building, attacker)
				if building.Owner == Greece and building.Health < building.MaxHealth * 0.8 then
					building.StartBuildingRepairs()
					if attacker.Type ~= "yak" and not AlreadyHunting then
						AlreadyHunting = true
						Utils.Do(Greece.GetGroundAttackers(), function(unit)
							Trigger.OnIdle(unit, unit.Hunt)
						end)
					end
				end
			end)
		end)

		-- Find the bridge actors
		BridgePart1 = Map.ActorsInBox(Box1.CenterPosition, Box2.CenterPosition, function(self) return self.Type == "br1" end)[1]
		BridgePart2 = Map.ActorsInBox(Box1.CenterPosition, Box2.CenterPosition, function(self) return self.Type == "br2" end)[1]
	end)

	-- Discover the area around the bridge exposing the two german soldiers
	-- When the two infantry near the bridge are discovered move them across the bridge to waypoint4
	-- in the meanwhile one USSR soldier hunts them down
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Actor.Create("camera", true, { Owner = USSR, Location = Box1.Location })

		Utils.Do(FleeingUnits, function(unit)
			unit.Move(RifleRetreat.Location)
		end)
		Follower.AttackMove(RifleRetreat.Location)
	end)

	-- To make it look more smooth we will blow up the bridge when the barrel closest to it blows up
	Trigger.OnAnyKilled({ BridgeBarrel1, BridgeBarrel2 }, function()
		-- Destroy the bridge
		if not BridgePart1.IsDead then
			BridgePart1.Kill()
		end
		if not BridgePart2.IsDead then
			BridgePart2.Kill()
		end
	end)

	-- If player passes over the bridge, blow up the barrel and destroy the bridge
	Trigger.OnEnteredFootprint(PassingBridgeLocation, function(unit, id)
		if unit.Owner == USSR then
			Trigger.RemoveFootprintTrigger(id)

			-- Also don't if the bridge is already dead
			if BridgePart1.IsDead and BridgePart2.IsDead then
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
		local powerproxy = Actor.Create("powerproxy.paratroopers", true, { Owner = USSR })
		powerproxy.TargetParatroopers(ParadropLZ.CenterPosition, Angle.South)
		powerproxy.TargetParatroopers(ParadropLZ.CenterPosition, Angle.SouthEast)
		powerproxy.Destroy()
	end)

	Greece.Resources = 2000
	Trigger.AfterDelay(DateTime.Seconds(30), ProduceInfantry)
end

Tick = function()
	if Greece.HasNoRequiredUnits() then
		USSR.MarkCompletedObjective(CommandCenterIntact)
		USSR.MarkCompletedObjective(DestroyAllAllied)
	end

	if USSR.HasNoRequiredUnits() then
		USSR.MarkFailedObjective(DestroyAllAllied)
	end

	if Greece.Resources > Greece.ResourceCapacity / 2 then
		Greece.Resources = Greece.ResourceCapacity / 2
	end
end
