--from original ini
CameraTriggerArea = { CPos.New(42, 45), CPos.New(43, 45), CPos.New(44, 45), CPos.New(45, 45), CPos.New(46, 45), CPos.New(47, 45), CPos.New(48, 45), CPos.New(48, 56), CPos.New(48, 57), CPos.New(48, 58), CPos.New(48, 59), CPos.New(40, 63), CPos.New(41, 63), CPos.New(42, 63), CPos.New(43, 63), CPos.New(44, 63), CPos.New(45, 63), CPos.New(46, 63), CPos.New(47, 63)} --when player passes these locations the base will be revealed
waypoint7location = { CPos.New(73, 49) } --when the CmdAtk from the small island reach this position they will attack the command center
PassingBridgeLocation = { CPos.New(59, 56), CPos.New(60, 56) } --Bridge location

CmdAtk = {Actor109, Actor110, Actor111, Actor112} --4 infantry on small island
Flee = {Actor94, Actor95} --2 infantry north of bridge

ParadropUnitTypes = { "e2", "e2", "e2", "e2", "e2" } --5 grenadiers

HuntingUnits = {Actor105, Actor106, Actor107, Actor108} --Units that are hunting

WorldLoaded = function()
	--setup the players
	player = Player.GetPlayer("USSR")
	germany = Player.GetPlayer("Germany")

	--mission objectives stuff
	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)
	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)
	
	--add the primary mission objectives
	CommandcenterIntact = player.AddPrimaryObjective("Protect the commandcenter")
	DestroyallAllied = player.AddPrimaryObjective("Destroy all Allied units and structures")
	
	--after 5 minutes you are reminded what to do
	Trigger.AfterDelay(DateTime.Minutes(5), function()
		Media.DisplayMessage("Destroy all Allied units and structures", "")
	end)
	
	--notify player of win or loose
	Trigger.OnPlayerWon(player, function()
		Media.PlaySpeechNotification(player, "MissionAccomplished")
	end)
	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "MissionFailed")
	end)

	--camera starting position (in original game always at waypoint98)
	Camera.Position	= waypoint98.CenterPosition
	
	--if the command center is destroyed player loses
	--then it is destroyed player looses
	Trigger.OnAnyKilled({ Actor43 }, function (dontcare)
		player.MarkFailedObjective(CommandcenterIntact)
	end)

	--discover the area around the bridge exposing the 2 german soldiers
	Trigger.AfterDelay(DateTime.Seconds(1), function() Actor.Create("camera", true, { Owner = player, Location = waypoint23.Location }) end)
	
	--when the 2 infantry near the bridge are discovered move them accross the bridge to waypoint42, in the meanwhile 1 USSR soldier hunts them down
	--TODO there is no discovered trigger yet so using afterdelay (after all they get discovered straight away)
	Trigger.AfterDelay(DateTime.Seconds(1.5), function()
		Utils.Do(Flee, function(unit)
			unit.Move(waypoint4.Location)
		end)
	end)

	--to make it look more smooth we will blow up the bridge when the barrel closest to it blows up
	Trigger.OnAnyKilled({ Actor69, Actor65 }, function(dontcare)
		--find the bridge actor
		bridgepart1 = Map.ActorsInBox(waypoint23.CenterPosition, waypoint49.CenterPosition, function(self) return self.Type == "br1" end)[1]
		bridgepart2 = Map.ActorsInBox(waypoint23.CenterPosition, waypoint49.CenterPosition, function(self) return self.Type == "br2" end)[1]
	
		--destroy the bridge
		if not bridgepart1.IsDead then
			bridgepart1.Kill()
		end
		if not bridgepart2.IsDead then
			bridgepart2.Kill()
		end		
		
		--move units towards the oilpump
		Utils.Do(Flee, function(unit)
			if not unit.IsDead then
				unit.Move(waypoint53.Location)
			end
		end)
	end)
	
	--if player passes over the bridge, blow up the barrels(actor72) and destroy the bridge
	Trigger.OnEnteredFootprint(PassingBridgeLocation, function(unit, id)
		if unit.Owner == player then
			Trigger.RemoveFootprintTrigger(id)
		
			--TODO have the units fire directly onto the barrel to destroy it
			--TODO check with yak if it explodes the bridge and otherwise prevent this from happening
			if not Actor72.IsDead then
				Actor72.Kill()
			end
		end
	end)

	--4 infantry from the small island move towards the USSR command center and attack it after 0.4 minutes
	Trigger.AfterDelay(DateTime.Seconds(24),  function()
		--for each unit in CmdAtk
		Utils.Do(CmdAtk, function(unit)
			--move the unit and make sure they keep moving to waypoint 7
			unit.AttackMove(waypoint7.Location)
			Trigger.OnIdle(unit, function() unit.AttackMove(waypoint7.Location) end)
		end)
		
		--on reaching waypoint 7 remove the trigger to keep moving or substitue with new instruction
		Trigger.OnEnteredFootprint(waypoint7location, function(unit, id)
			Trigger.RemoveFootprintTrigger(id)
			
			--for each unit in CmdAtk
			Utils.Do(CmdAtk, function(unit)
				if not unit.IsDead then
					--remove the attackmove trigger
					--Trigger.Clear(unit, "OnIdle")

					--TODO: Order the unit to attack the commandcenter, there is currently no command to do so
					Trigger.OnIdle(unit, function() unit.Hunt() end)
				end
			end)	
		end)
	end)
	
	--Start hunting
	Actor108.AttackMove(waypoint101.Location) --move the unit in the correct direction first
	Utils.Do(HuntingUnits, function(unit)
		Trigger.OnIdle(unit, function() unit.Hunt() end)
	end)
	
	--when destroying the allied radar dome or the refinery drop 2 badgers with 5 grenadiers each
	Trigger.OnAnyKilled({ Actor36, Actor38 }, function(dontcare)
		BadgerStartPos1 = CPos.New(45, 38)
		BadgerStartPos2 = CPos.New(43, 38)
	
		local lz = waypoint2.Location
		local start1 = Map.CenterOfCell(BadgerStartPos1) + WVec.New(0, 0, Actor.CruiseAltitude("badr"))
		local start2 = Map.CenterOfCell(BadgerStartPos2) + WVec.New(0, 0, Actor.CruiseAltitude("badr"))
		local transport1 = Actor.Create("badr", true, { CenterPosition = start1, Owner = player, Facing = (Map.CenterOfCell(lz) - start1).Facing })
		local transport2 = Actor.Create("badr", true, { CenterPosition = start2, Owner = player, Facing = (Map.CenterOfCell(lz) - start2).Facing })

		--create the units to be dropped
		Utils.Do(ParadropUnitTypes, function(type)
			local a = Actor.Create(type, false, { Owner = player })
			local b = Actor.Create(type, false, { Owner = player })
		
			--load the units into the badger
			transport1.LoadPassenger(a)
			transport2.LoadPassenger(b)
		end)

		--now drop it
		transport1.Paradrop(lz)
		transport2.Paradrop(lz)
	end)
	
	--when passing the map coordinates in CameraTriggerArea reveal the allied base
	Trigger.OnEnteredFootprint(CameraTriggerArea, function(unit, id)
		if unit.Owner == player then
			Trigger.RemoveFootprintTrigger(id)
			Actor.Create("camera", true, { Owner = player, Location = waypoint42.Location })
		end
	end)
end

Tick = function()
	--mark objective completed when it's done
	if germany.HasNoRequiredUnits() then
		player.MarkCompletedObjective(CommandcenterIntact)
		player.MarkCompletedObjective(DestroyallAllied)
	end
	
	if player.HasNoRequiredUnits() then
		player.MarkFailedObjective(DestroyallAllied)
	end
end