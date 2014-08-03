FirstAttackWave = { "e1", "e1", "e1", "e2", }
SecondThirdAttackWave = { "e1", "e1", "e2", }

MissionAccomplished = function()
	Mission.MissionOver({ player }, nil)
	Media.PlayMovieFullscreen("desflees.vqa")
end

MissionFailed = function()
	Mission.MissionOver(nil, { player })
	Media.PlayMovieFullscreen("flag.vqa")
end

SendFirstAttackWave = function()
	for FirstAttackWaveCount = 1, 4 do
		local waveunit = Actor.Create(FirstAttackWave[FirstAttackWaveCount], { Owner = enemy, Location = AttackWaveSpawnA.Location })
		Actor.AttackMove(waveunit, PlayerBase.Location)
	end
end

SendSecondAttackWave = function()
	for SecondAttackWaveCount = 1, 3 do
		local waveunit = Actor.Create(SecondThirdAttackWave[SecondAttackWaveCount], { Owner = enemy, Location = AttackWaveSpawnB.Location })
		Actor.AttackMove(waveunit, PlayerBase.Location)
	end
end

SendThirdAttackWave = function()
	for ThirdAttackWaveCount = 1, 3 do
		local waveunit = Actor.Create(SecondThirdAttackWave[ThirdAttackWaveCount], { Owner = enemy, Location = AttackWaveSpawnC.Location })
		Actor.AttackMove(waveunit, PlayerBase.Location)
	end
end

WorldLoaded = function()
	player = OpenRA.GetPlayer("Nod")
	enemy = OpenRA.GetPlayer("GDI")
	Media.PlayMovieFullscreen("nod3.vqa")
	OpenRA.RunAfterDelay(25 * 20, SendFirstAttackWave)
	OpenRA.RunAfterDelay(25 * 50, SendSecondAttackWave)
	OpenRA.RunAfterDelay(25 * 100, SendThirdAttackWave)
	Actor.OnCaptured(TechCenter, MissionAccomplished)
	Actor.OnKilled(TechCenter, MissionFailed)
end

Tick = function()
	if Mission.RequiredUnitsAreDestroyed(player) then
		MissionFailed()
	end
end
