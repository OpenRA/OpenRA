FirstAttackWave = { "e1", "e1", "e2", }
SecondAttackWave = { "e1", "e1", "e1", }
ThirdAttackWave = { "e1", "e1", "e1", "e2", }

MissionAccomplished = function()
	Mission.MissionOver({ player }, nil)
	Media.PlayMovieFullscreen("desflees.vqa")
end

MissionFailed = function()
	Mission.MissionOver(nil, { player })
	Media.PlayMovieFullscreen("flag.vqa")
end

SendFirstAttackWave = function()
	local wave = Reinforcements.Reinforce(enemy, FirstAttackWave, GDIBarracksSpawn.Location, WP0.Location, 0)
	Utils.Do(wave, function(soldier)
		Actor.Move(soldier, WP1.Location)
		Actor.Move(soldier, WP2.Location)
		Actor.Move(soldier, WP3.Location)
		Actor.Move(soldier, WP4.Location)
		--Actor.Move(soldier, WP5.Location)
		Actor.AttackMove(soldier, PlayerBase.Location)
	end)
end

SendSecondAttackWave = function()
	local wave = Reinforcements.Reinforce(enemy, SecondAttackWave, GDIBarracksSpawn.Location, WP0.Location, 0)
	Utils.Do(wave, function(soldier)
		Actor.Move(soldier, WP1.Location)
		Actor.Move(soldier, WP5.Location)
		Actor.Move(soldier, WP6.Location)
		Actor.Move(soldier, WP7.Location)
		Actor.Move(soldier, WP9.Location)
		Actor.AttackMove(soldier, PlayerBase.Location)
	end)
end

SendThirdAttackWave = function()
	local wave = Reinforcements.Reinforce(enemy, ThirdAttackWave, GDIBarracksSpawn.Location, WP0.Location, 0)
	Utils.Do(wave, function(soldier)
		Actor.Move(soldier, WP1.Location)
		Actor.Move(soldier, WP2.Location)
		Actor.Move(soldier, WP3.Location)
		Actor.Move(soldier, WP4.Location)
		Actor.AttackMove(soldier, PlayerBase.Location)
	end)
end

WorldLoaded = function()
	player = OpenRA.GetPlayer("Nod")
	enemy = OpenRA.GetPlayer("GDI")
	Media.PlayMovieFullscreen("nod3.vqa")
	OpenRA.RunAfterDelay(25 * 40, SendFirstAttackWave)
	OpenRA.RunAfterDelay(25 * 80, SendSecondAttackWave)
	OpenRA.RunAfterDelay(25 * 140, SendThirdAttackWave)
	Actor.OnCaptured(TechCenter, MissionAccomplished)
	Actor.OnKilled(TechCenter, MissionFailed)
end

Tick = function()
	if Mission.RequiredUnitsAreDestroyed(player) then
		MissionFailed()
	end
end
