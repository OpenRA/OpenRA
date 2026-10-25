-- Diagnostic fixture. Tokens are filled by prepare.py.
local ticks = 0

function Tick()
	ticks = ticks + 1
end

local function StartNext()
	print('PROGRESSION-MOVIE-COMPLETE ticks=' .. ticks)
	if '@CASE@' == 'missing' then
		print('PROGRESSION-MISSING accepted=' .. tostring(Map.StartMission('not-a-map')))
		print('PROGRESSION-EMPTY accepted=' .. tostring(Map.StartMission('')))
		return
	end

	print('PROGRESSION-REQUEST accepted=' .. tostring(Map.StartMission('@NEXT_UID@')))
	print('PROGRESSION-DUPLICATE accepted=' .. tostring(Map.StartMission('@NEXT_UID@')))
	print('PROGRESSION-RETURNED ticks=' .. ticks)
end

function ResultsDismissed(won)
	print('PROGRESSION-RESULT won=' .. tostring(won) .. ' ticks=' .. ticks)
	if not won then
		return
	end

	if '@CASE@' == 'movie' then
		print('PROGRESSION-MOVIE-START ticks=' .. ticks)
		Media.PlayMovieFullscreen('content|progression-probe.vqa', StartNext)
	else
		StartNext()
	end
end

function WorldLoaded()
	Camera.Position = Unit.CenterPosition
	local player = Player.GetPlayer('GDI')
	local objective = player.AddPrimaryObjective('Reach the results screen, then continue to mission two')
	print('PROGRESSION-FIRST difficulty=' .. Map.LobbyOption('difficulty'))
	print('PROGRESSION-LIVE accepted=' .. tostring(Map.StartMission('@NEXT_UID@')))
	Trigger.AfterDelay(3, function()
		print('PROGRESSION-END ticks=' .. ticks)
		if '@CASE@' == 'loss' or '@CASE@' == 'restart' then
			player.MarkFailedObjective(objective)
		else
			player.MarkCompletedObjective(objective)
		end
		Trigger.AfterDelay(25, function() print('UNEXPECTED-FIRST-TIMER') end)
	end)
end
