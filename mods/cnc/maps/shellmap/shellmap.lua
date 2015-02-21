CommandoTeam = { "rmbo", "rmbo" }
GDIAttackTeam = { "e1", "e1", "e2" }
GDIInfantryTeam = { "e1", "e1", "e1", "e2", "e2" }
GDIVehicleTeam = { "jeep", "jeep" }
NodInfantry =
	{
		{{"e1", "e1", "e3"},function(a)
			a.AttackMove(WestSide.Location)
			a.AttackMove(MCVDeploy.Location)
		end},
		{{"e1", "e1", "e1", "e3", "e3"},function(a)
			a.AttackMove(WestSide.Location)
			a.AttackMove(MCVDeploy.Location)
		end},
		{{"e4", "e4"},function(a)
			a.AttackMove(WestSide.Location)
			a.AttackMove(MCVDeploy.Location)
		end},
		{{"e1", "e1", "e1", "e1", "e3", "e3", "e4", "e4" },function(a)
			a.AttackMove(WestSide.Location)
			a.AttackMove(MCVDeploy.Location)
		end}
	}
NodVehicles =
	{
		{{"ltnk", "ltnk", "ltnk"},function(a)
			a.AttackMove(WestSide.Location)
			a.AttackMove(MCVDeploy.Location)
		end},
		{{"bike", "bike", "bike"},function(a)
			a.AttackMove(WestSide.Location)
			a.AttackMove(MCVDeploy.Location)
		end},
		{{"bike", "bike"},function(a)
			a.AttackMove(HandBuild.Location+CVec.New(3,5))
			a.AttackMove(MCVDeploy.Location)
		 end},
		{{"bggy", "bggy", "bggy"},function(a)
			a.AttackMove(HandBuild.Location+CVec.New(3,5))
			a.AttackMove(MCVDeploy.Location)
		 end}
	}
OutpostInfantry =
	{
		{{"e1", "e1", "e3"}, function(a) a.AttackMove(MCVDeploy.Location) end },
		{{"e1", "e1", "e1", "e3", "e3"}, function(a) a.AttackMove(MCVDeploy.Location) end },
		{{"e4", "e4"}, function(a) a.AttackMove(MCVDeploy.Location) end },
		{{"e1", "e1", "e1", "e1", "e3", "e3", "e4", "e4" }, function(a) a.AttackMove(MCVDeploy.Location) end }
	}

LSTREntryPath = {LSTRStart.Location, LSTRMove.Location, LSTRLand.Location}
LSTMEntryPath = {LSTMStart.Location, LSTMMove.Location, LSTMLand.Location}
LSTLEntryPath = {LSTLStart.Location, LSTLMove.Location, LSTLLand.Location}

LSTRExitPath = {CPos.New(LSTRMove.Location.X,LSTLStart.Location.Y), CPos.New(8,LSTLStart.Location.Y)}
LSTMExitPath = {LSTMMove.Location, CPos.New(8,LSTMStart.Location.Y)}
LSTLExitPath = {CPos.New(LSTLMove.Location.X,LSTRStart.Location.Y), CPos.New(8,LSTRStart.Location.Y)}

UnloadLST = function(lst,cargo)
	Utils.Do(cargo, function()
		local a = lst.UnloadPassenger()
		a.Teleport(lst.Location)
		a.IsInWorld = true
		a.Move(lst.Location - CVec.New(0, 1))
	end)
end

PlayMusic = function()
	Media.PlayMusic("map1", PlayMusic)
end

StayInLocation = function(a,loc,dist)
	if not a.IsDead then
		local vector = CVec.New(a.Location.X-loc.X,a.Location.Y-loc.Y)
		if math.abs(vector.X) > dist or math.abs(vector.Y) > dist then
			a.Stop()
			a.AttackMove(loc)
			Trigger.AfterDelay(DateTime.Seconds(5), function() StayInLocation(a,loc,dist) end)
		end
	end
end

MakeNod = function(fact,teams)
	nod.Cash = nod.Cash + 3000
	if not fact.IsDead then
		local teamtype = Utils.Random(teams)
		fact.Build(teamtype[1], function(team)
			Utils.Do(team, teamtype[2])
			MakeNod(fact,teams)
		end)
	end
end

MakeGDI = function(fact,teamtype)
	gdi.Cash = gdi.Cash + 3000
	fact.Build(teamtype, function(team)
		Utils.Do(team, function(a)
			a.Stance = "Defend"
			StayInLocation(a,MCVDeploy.Location-CVec.New(3,7),1)
		end)
		Trigger.OnAllKilled(team,function()
			if not fact.IsDead then
				MakeGDI(fact,teamtype)
			end
		end)
	end)
end

MakeUnitInvincible = function(unit)
	if not unit.HasProperty("AcceptsUpgrade") then
		unit.Destroy()
	end
	if unit.HasProperty("AcceptsUpgrade") and unit.AcceptsUpgrade("unkillable") then
		unit.GrantUpgrade("unkillable")
		if unit.Stance ~= nil then
			unit.Stance = "Defend"
		end
	end
end

SendCommandos = function()
	local commandos = Reinforcements.ReinforceWithTransport(gdi, "tran", CommandoTeam, 
	{ChinEntry.Location, ChinUnload.Location}, {ChinExit.Location})[2]
	
	Trigger.OnIdle(commandos[1], function()
			if not (Turret1.IsDead) then
				commandos[1].Demolish(Turret1)
			end
			if not (beachdude1.IsDead and beachdude2.IsDead) then
				commandos[1].AttackMove(CommMove.Location)
			end
			if not (Turret3.IsDead) then
				commandos[1].Demolish(Turret3)
				commandos[1].Move(CommFin.Location)
			end
			commandos[1].Move(CommFin.Location)
	end)

	Trigger.OnIdle(commandos[2], function()
			if not (Turret2.IsDead) then
				commandos[2].Demolish(Turret2)
			end
			if not (beachdude1.IsDead and beachdude2.IsDead) then
				commandos[2].AttackMove(CommMove.Location)
			end
			if not (Turret4.IsDead) then
				commandos[2].Demolish(Turret4)
				commandos[2].Move(CommFin.Location)
			end
			commandos[2].Move(CommFin.Location)
	end)

	Trigger.OnKilled(Turret1, function()
			beachdude1.AttackMove(BeachDudeMove.Location)
			beachdude2.AttackMove(BeachDudeMove.Location)
	end)

	Trigger.OnKilled(Turret3, function()
			commandos[1].Stop()
			commandos[1].ScriptedMove(CommFin.Location)
			Trigger.Clear(commandos[1], "OnIdle")
			Trigger.AfterDelay(25, Beachhead)
	end)

	Trigger.OnKilled(Turret4, function()
			commandos[2].Stop()
			commandos[2].ScriptedMove(CommFin.Location)
			Trigger.Clear(commandos[2], "OnIdle")
	end)
	Trigger.OnAllKilled({beachdude1, beachdude2}, function()
		local trans = Reinforcements.ReinforceWithTransport(gdi, "tran", nil, {EvacEntry.Location, EvacLand.Location})[1]
		Trigger.OnIdle(trans, function()
			if(trans.HasPassengers) then
				trans.Wait(25)
				trans.CallFunc(function() PlayMusic() end)
				trans.Move(EvacEntry.Location)
				trans.Destroy()
			else
				commandos[1].ScriptedMove(FlareHere.Location)
				commandos[2].ScriptedMove(FlareHere.Location)
				commandos[1].EnterTransport(trans)
				commandos[2].EnterTransport(trans)
			end
		end)
	end)
end

Beachhead = function()
	local ltank = Reinforcements.ReinforceWithTransport(gdi, "lst", {"htnk"}, LSTLEntryPath, LSTLExitPath, UnloadLST)[2]
	Trigger.OnAddedToWorld(ltank[1], LeftTankAction)
	Trigger.AfterDelay(55, function()
		local mcv = Reinforcements.ReinforceWithTransport(gdi, "lst", {"mcv"}, LSTMEntryPath, LSTMExitPath, UnloadLST)[2]
		Trigger.OnAddedToWorld(mcv[1], MCVAction)
	end)
	Trigger.AfterDelay(110, function()
		local rtank = Reinforcements.ReinforceWithTransport(gdi, "lst", {"htnk"}, LSTREntryPath, LSTRExitPath, UnloadLST)[2]
		Trigger.OnAddedToWorld(rtank[1], RightTankAction)
	end)
end

RightTankAction = function(tank)
	tank.ScriptedMove(LSTRLand.Location - CVec.New(0,4))
	tank.ScriptedMove(LSTLLand.Location - CVec.New(12,4))
	MakeUnitInvincible(tank)
end

LeftTankAction = function(tank)
	tank.ScriptedMove(LSTLLand.Location - CVec.New(0,5))
	tank.ScriptedMove(LSTLLand.Location - CVec.New(11,5))
	MakeUnitInvincible(tank)
end

MCVAction = function(mcv)
	mcv.Wait(250)
	mcv.ScriptedMove(MCVDeploy.Location)
	mcv.CallFunc(function() Trigger.AfterDelay(100, function() BuildBase() end) end)
	Trigger.OnIdle(mcv, mcv.Deploy)
end

BuildBase = function()
	local clock = Map.ActorsInCircle(Map.CenterOfCell(MCVDeploy.Location), WRange.New(512))[1]

	--Build a Power Plant
	clock.Wait(Actor.BuildTime("nuke"))
	clock.CallFunc(function() Actor.Create("nuke", true, { Owner = gdi, Location = MCVDeploy.Location + CVec.New(2,-1) }) end)

	--Build a Barracks then Guard Tower, Nod builds a Hand of Nod to the north then a turret
	clock.Wait(Actor.BuildTime("pyle"))
	clock.CallFunc(function()
		Bggy1.Move(HandBuild.Location+CVec.New(3,3))
		Bggy1.Stance = "Defend"
		Bggy2.Move(HandBuild.Location+CVec.New(3,3))
		Bggy2.Stance = "Defend"
		local barr = Actor.Create("pyle", true, { Owner = gdi, Location = MCVDeploy.Location + CVec.New(-2,-5) })
		barr.Wait(100)
		barr.CallFunc(function()
			barr.Build(GDIAttackTeam, function(team)
				Utils.Do(team, function(a) Trigger.OnIdle(a, function(a) a.AttackMove(HandBuild.Location) end) end)
				barr.RallyPoint = MCVDeploy.Location-CVec.New(3,7)
				barr.Wait(250)
				barr.CallFunc(function() MakeGDI(barr,GDIInfantryTeam) end)
			end)
		end)
	end)
	clock.CallFunc(function() Trigger.AfterDelay(Actor.BuildTime("hand"), function()
			local refnHand = Actor.Create("hand", true, { Owner = nod, Location = HandBuild.Location - CVec.New(0,1) })
			refnHand.Wait(100)
			refnHand.CallFunc(function()
				MakeNod(refnHand,OutpostInfantry)
			end)
			Trigger.AfterDelay(Actor.BuildTime("gun"), function()
				MakeUnitInvincible(Actor.Create("gun", true, { Owner = nod, Location = HandBuild.Location + CVec.New(4,4) }))
				if not Bggy1.IsDead then
					Bggy1.AttackMove(MCVDeploy.Location)
				end
				if not Bggy2.IsDead then
					Bggy2.AttackMove(MCVDeploy.Location)
				end
			end)
			Tnk1.AttackMove(NodHand.Location+CVec.New(2,3))
			Tnk1.AttackMove(WestSide.Location)
			Tnk1.AttackMove(MCVDeploy.Location)
			Tnk2.AttackMove(NodHand.Location+CVec.New(2,3))
			Tnk2.AttackMove(WestSide.Location)
			Tnk2.AttackMove(MCVDeploy.Location)
			Tnk3.AttackMove(NodHand.Location+CVec.New(2,3))
			Tnk3.AttackMove(WestSide.Location)
			Tnk3.AttackMove(MCVDeploy.Location)
		end)
	end)
	clock.CallFunc(function() Trigger.AfterDelay(Actor.BuildTime("gtwr"), function()
			MakeUnitInvincible(Actor.Create("gtwr", true, { Owner = gdi, Location = MCVDeploy.Location + CVec.New(-1,-6) }))
		end)
	end)

	--Build a Refinery and make harvester immune, then build another Guard Tower
	clock.Wait(Actor.BuildTime("proc"))
	clock.CallFunc(function()
		Actor.Create("proc", true, { Owner = gdi, Location = MCVDeploy.Location + CVec.New(4,-4) })
		Trigger.AfterDelay(1, function() 
			Utils.Do(Map.ActorsInBox(Map.TopLeft,Map.BottomRight), function(a)
				if(a.Owner == gdi and a.Type == "harv") then
					MakeUnitInvincible(a)
				end
			end)
		end)
	end)
	clock.CallFunc(function() Trigger.AfterDelay(Actor.BuildTime("gtwr"), function()
		MakeUnitInvincible(Actor.Create("gtwr", true, { Owner = gdi, Location = MCVDeploy.Location + CVec.New(8,-4) }))
	end) end)

	--Build another Power Plant
	clock.Wait(Actor.BuildTime("nuke"))
	clock.CallFunc(function() Actor.Create("nuke", true, { Owner = gdi, Location = MCVDeploy.Location + CVec.New(-3,-1) }) end)

	--Build a Factory and start Nod's main base up
	clock.Wait(Actor.BuildTime("weap"))
	clock.CallFunc(function()
		MakeNod(NodHand,NodInfantry)
		MakeNod(NodAfld,NodVehicles)
		local fact = Actor.Create("weap", true, { Owner = gdi, Location = MCVDeploy.Location + CVec.New(-8,-4) })
		fact.RallyPoint = MCVDeploy.Location-CVec.New(3,7)
		fact.Wait(100)
		fact.CallFunc(function() MakeGDI(fact,GDIVehicleTeam) end)
	end)

	--Build Adv Power, Comm Center, Repair Pad and the factory advanced guard tower
	clock.Wait(Actor.BuildTime("nuk2"))
	clock.CallFunc(function() Actor.Create("nuk2", true, { Owner = gdi, Location = MCVDeploy.Location + CVec.New(7,0) }) end)
	clock.Wait(Actor.BuildTime("hq"))
	clock.CallFunc(function()
		Actor.Create("hq", true, { Owner = gdi, Location = MCVDeploy.Location + CVec.New(9,0) })
		table.remove(GDIVehicleTeam,1)
		table.insert(GDIVehicleTeam,"mtnk")
		table.insert(GDIVehicleTeam,"mtnk")
		table.insert(GDIInfantryTeam,"e3")
		table.insert(GDIInfantryTeam,"e3")
		table.insert(NodVehicles, {{"ltnk"},function(a)
			a.AttackMove(HandBuild.Location+CVec.New(3,5))
			a.AttackMove(MCVDeploy.Location)
		 end})
		 table.insert(NodVehicles, {{"ftnk"},function(a)
			a.AttackMove(WestSide.Location)
			a.AttackMove(MCVDeploy.Location)
		 end})
	end)

	clock.Wait(Actor.BuildTime("fix"))
	clock.CallFunc(function()
		Actor.Create("fix", true, { Owner = gdi, Location = MCVDeploy.Location + CVec.New(-9,0) })
	 	Trigger.AfterDelay(Actor.BuildTime("atwr"), function()
			MakeUnitInvincible(Actor.Create("atwr", true, { Owner = gdi, Location = MCVDeploy.Location + CVec.New(-10,-4) }))
		end)
	end)

	--Build First Helipad and last Guard Tower
	clock.Wait(Actor.BuildTime("hpad"))
	clock.CallFunc(function() Actor.Create("hpad", true, { Owner = gdi, Location = MCVDeploy.Location + CVec.New(17,0) }) end)
	clock.CallFunc(function() Trigger.AfterDelay(Actor.BuildTime("gtwr"), function()
			MakeUnitInvincible(Actor.Create("gtwr", true, { Owner = gdi, Location = MCVDeploy.Location + CVec.New(17,-2) }))
		end)
	end)

	--Build Remaining Helipads
	clock.Wait(Actor.BuildTime("hpad"))
	clock.CallFunc(function() Actor.Create("hpad", true, { Owner = gdi, Location = MCVDeploy.Location + CVec.New(20,1) }) end)
	clock.Wait(Actor.BuildTime("hpad"))
	clock.CallFunc(function() Actor.Create("hpad", true, { Owner = gdi, Location = MCVDeploy.Location + CVec.New(18,2) }) end)
	clock.Wait(Actor.BuildTime("hpad"))
	clock.CallFunc(function() Actor.Create("hpad", true, { Owner = gdi, Location = MCVDeploy.Location + CVec.New(21,3) }) end)

	--todo: call some orcas maybe?
end

ticks = 0
speed = 5

Tick = function()
	ticks = ticks + 1
	local t = (ticks + 45) % (360 * speed) * (math.pi / 180) / speed;
	Camera.Position = viewportOrigin + WVec.New(15360 * math.sin(t), -4096 * math.cos(t), 0)
end

WorldLoaded = function()
	gdi = Player.GetPlayer("GDI")
	nod = Player.GetPlayer("Nod")
	viewportOrigin = Camera.Position
	SendCommandos()
	Media.PlayMusic()
end