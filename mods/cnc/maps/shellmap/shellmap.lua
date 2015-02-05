CommandoTeam = {"rmbo", "rmbo"}
OrcaSquadron = {"orca", "orca", "orca", "orca"}

LSTREntryPath = {LSTRStart.Location, LSTRMove1.Location, LSTRMove2.Location, LSTRLand.Location}
LSTMEntryPath = {LSTMStart.Location, LSTMMove1.Location, LSTMMove2.Location, LSTMLand.Location}
LSTLEntryPath = {LSTLStart.Location, LSTLMove1.Location, LSTLMove2.Location, LSTLLand.Location}

LSTRExitPath = {LSTRMove2.Location + CVec.New(0,1), CPos.New(8,LSTRStart.Y)}
LSTMExitPath = {LSTMMove2.Location + CVec.New(0,1), CPos.New(8,LSTMStart.Y)}
LSTLExitPath = {LSTLMove2.Location + CVec.New(0,1), CPos.New(8,LSTLStart.Y)}

--DEBUG Functions: Remove when Shellmap deemed "complete"
SkipCommandos = function()
	Turret1.Destroy()
	Turret2.Destroy()
	Turret3.Destroy()
	Turret4.Destroy()
	beachdude1.Destroy()
	beachdude2.Destroy()
	Beachhead()
end
--DEBUG End

MakeUnitInvincible = function(unit)
	if unit.HasProperty("AcceptsUpgrade") and unit.AcceptsUpgrade("unkillable") then
		unit.GrantUpgrade("unkillable")
		unit.Stance = "Defend"
	end
end

WakeNodRefn = function()
	refnHand = Reinforcements.Reinforce(nod, { "hand" }, { (HandBuild.Location-CVec.New(0,1)) })[1]
	Trigger.OnDamaged(refnHand, function(a) a.StartBuildingRepairs(a.Owner) end)

	--todo: Nod outpost does more things
end

WakeNodMain = function()
	--todo: Big Nod Base starts doing things
end

GDIStartInfantry = function()
	--todo: GDI starts making infantry
end

GDIStartVehicles = function()
	--todo: GDI starts making vehicles
end

SendCommandos = function()
	local commandos = Reinforcements.ReinforceWithTransport(gdi, "tran", CommandoTeam, 
	{ChinEntry.Location, ChinUnload.Location}, {ChinExit.Location})[2]
	
	Trigger.OnIdle(commandos[1], function()
			if(Map.NamedActor("Turret1") ~= nil) then
				commandos[1].GoDemolish(Turret1)
			end
			if(Map.NamedActor("beachdude1") ~= nil or Map.NamedActor("beachdude2") ~= nil) then
				commandos[1].AttackMove(CommMove.Location)
			end
			if(Map.NamedActor("Turret3") ~= nil) then
				commandos[1].GoDemolish(Turret3)
				commandos[1].AttackMove(CommFin.Location)
			end
			commandos[1].AttackMove(CommFin.Location)
		 end)

	Trigger.OnIdle(commandos[2], function()
			if(Map.NamedActor("Turret2") ~= nil) then
				commandos[2].GoDemolish(Turret2)
			end
			if(Map.NamedActor("beachdude1") ~= nil or Map.NamedActor("beachdude2") ~= nil) then
				commandos[2].AttackMove(CommMove.Location)
			end
			if(Map.NamedActor("Turret4") ~= nil) then
				commandos[2].GoDemolish(Turret4)
				commandos[2].AttackMove(CommFin.Location)
			end
			commandos[2].AttackMove(CommFin.Location)
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
					trans.Move(EvacEntry.Location)
					trans.Destroy()
				else
					commandos[1].ScriptedMove(FlareHere.Location)
					commandos[2].ScriptedMove(FlareHere.Location)
					commandos[1].GetInTransport(trans)
					commandos[2].GetInTransport(trans)
				end
			end)
		end)
end

Beachhead = function()

	--todo: Make custom reinforce function to handle LST Dropoff properly
	ltank = Reinforcements.ReinforceWithTransport(gdi, "lst", {"htnk"}, LSTLEntryPath, LSTLExitPath)[2]
	Trigger.OnAddedToWorld(ltank[1], function(unit) LeftTankAction(unit) end)
	Trigger.AfterDelay(25, function() 
		mcv = Reinforcements.ReinforceWithTransport(gdi, "lst", {"mcv"}, LSTMEntryPath, LSTMExitPath)[2]
		Trigger.OnAddedToWorld(mcv[1], function(unit) MCVAction(unit) end)
	end)
	Trigger.AfterDelay(50, function() 
		rtank = Reinforcements.ReinforceWithTransport(gdi, "lst", {"htnk"}, LSTREntryPath, LSTRExitPath)[2]
		Trigger.OnAddedToWorld(rtank[1], function(unit) RightTankAction(unit) end)
	end)
end

RightTankAction = function(tank)
	tank.ScriptedMove(LSTRLand.Location - CVec.New(0,5))
	tank.ScriptedMove(LSTLLand.Location - CVec.New(12,6))
	MakeUnitInvincible(tank)
end

LeftTankAction = function(tank)
	tank.ScriptedMove(LSTLLand.Location - CVec.New(0,4))
	tank.ScriptedMove(LSTLLand.Location - CVec.New(11,7))
	MakeUnitInvincible(tank)
end

MCVAction = function(mcv)
	mcv.ScriptedMove(LSTMLand.Location - CVec.New(0,2))
	mcv.Wait(200)
	mcv.ScriptedMove(MCVDeploy.Location)
	mcv.CallFunc(function() Trigger.AfterDelay(100, function() BuildBase() end) end)
	Trigger.OnIdle(mcv, function() mcv.Deploy()	end)
end

BuildBase = function()
	clock = Map.ActorsInCircle(Map.CenterOfCell(MCVDeploy.Location), WRange.New(512))[1]

	--Build 2 Powers
	clock.Wait(300)
	gdi.TakeMoney(500)
	clock.CallFunc(function() Reinforcements.Reinforce(gdi, { "nuke" }, { (MCVDeploy.Location+CVec.New(2,-1)) }) end)
	clock.Wait(300)
	gdi.TakeMoney(500)
	clock.CallFunc(function() Reinforcements.Reinforce(gdi, { "nuke" }, { (MCVDeploy.Location+CVec.New(-3,-1)) }) end)

	--Build a Barracks then Guard Tower
	--clock.Wait(300)
	gdi.TakeMoney(500)
	clock.CallFunc(function() Reinforcements.Reinforce(gdi, { "pyle" }, { (MCVDeploy.Location+CVec.New(-2,-5)) }) end)
	clock.CallFunc(function() Trigger.AfterDelay(100, function() GDIStartInfantry() end) end)
	clock.CallFunc(function() Trigger.AfterDelay(100, function()
		gdi.TakeMoney(600)
		Reinforcements.Reinforce(gdi, { "gtwr" }, { (MCVDeploy.Location+CVec.New(-1,-6)) })
		WakeNodRefn()
	end) end)

	--Build a Refinery and make harvester immune, then build another Guard Tower
	clock.Wait(900)
	gdi.TakeMoney(1500)
	clock.CallFunc(function() 
		Reinforcements.Reinforce(gdi, { "proc" }, { (MCVDeploy.Location+CVec.New(4,-4)) }) 
		Trigger.AfterDelay(1, function() 
			Utils.Do(Map.ActorsInBox(Map.TopLeft,Map.BottomRight), function(a)
				if(a.Owner == gdi and a.Type == "harv") then
					MakeUnitInvincible(a)
				end
			end)
		end)
	end)
	clock.CallFunc(function() Trigger.AfterDelay(600, function()
		gdi.TakeMoney(600)
		Reinforcements.Reinforce(gdi, { "gtwr" }, { (MCVDeploy.Location+CVec.New(8,-4)) })
	end) end)

	--Build a Factory then yet another Guard Tower
	clock.Wait(1200)
	gdi.TakeMoney(2000)
	clock.CallFunc(function() Reinforcements.Reinforce(gdi, { "weap" }, { (MCVDeploy.Location+CVec.New(-8,-4)) }) end)
	clock.CallFunc(function() Trigger.AfterDelay(100, function() GDIStartVehicles() end) end)
	clock.CallFunc(function() Trigger.AfterDelay(600, function()
		gdi.TakeMoney(600)
		Reinforcements.Reinforce(gdi, { "gtwr" }, { (MCVDeploy.Location+CVec.New(-10,-3)) })
		WakeNodMain()
	end) end)

	--Build 2 Adv Powers & Comm Center
	clock.Wait(500)
	gdi.TakeMoney(800)
	clock.CallFunc(function() Reinforcements.Reinforce(gdi, { "nuk2" }, { (MCVDeploy.Location+CVec.New(-10,-7)) }) end)
	clock.Wait(500)
	gdi.TakeMoney(800)
	clock.CallFunc(function() Reinforcements.Reinforce(gdi, { "nuk2" }, { (MCVDeploy.Location+CVec.New(-8,-7)) }) end)
	clock.Wait(600)
	gdi.TakeMoney(1000)
	clock.CallFunc(function() Reinforcements.Reinforce(gdi, { "hq" }, { (MCVDeploy.Location+CVec.New(-6,-7)) }) end)

	--Build Repair Pad & 2 more Adv Powers
	clock.Wait(300)
	gdi.TakeMoney(500)
	clock.CallFunc(function() Reinforcements.Reinforce(gdi, { "fix" }, { (MCVDeploy.Location+CVec.New(-9,0)) }) end)
	clock.Wait(500)
	gdi.TakeMoney(800)
	clock.CallFunc(function() Reinforcements.Reinforce(gdi, { "nuk2" }, { (MCVDeploy.Location+CVec.New(7,0)) }) end)
	clock.Wait(500)
	gdi.TakeMoney(800)
	clock.CallFunc(function() Reinforcements.Reinforce(gdi, { "nuk2" }, { (MCVDeploy.Location+CVec.New(9,0)) }) end)

	--Build First Helipad and last Guard Tower
	clock.Wait(600)
	gdi.TakeMoney(1000)
	clock.CallFunc(function() Reinforcements.Reinforce(gdi, { "hpad" }, { (MCVDeploy.Location+CVec.New(17,0)) }) end)
	clock.CallFunc(function() Trigger.AfterDelay(600, function()
		gdi.TakeMoney(600)
		Reinforcements.Reinforce(gdi, { "gtwr" }, { (MCVDeploy.Location+CVec.New(17,-2)) })
	end) end)

	--Build Remaining Helipads
	clock.Wait(600)
	gdi.TakeMoney(1000)
	clock.CallFunc(function() Reinforcements.Reinforce(gdi, { "hpad" }, { (MCVDeploy.Location+CVec.New(20,1)) }) end)
	clock.Wait(600)
	gdi.TakeMoney(1000)
	clock.CallFunc(function() Reinforcements.Reinforce(gdi, { "hpad" }, { (MCVDeploy.Location+CVec.New(18,2)) }) end)
	clock.Wait(600)
	gdi.TakeMoney(1000)
	clock.CallFunc(function() Reinforcements.Reinforce(gdi, { "hpad" }, { (MCVDeploy.Location+CVec.New(21,3)) }) end)

	--todo: call orcas
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

	--SkipCommandos()
	SendCommandos()

	Media.PlayMusic()
end

