Media = { }

Media.PlaySpeechNotification = function(notification, player)
	Internal.PlaySpeechNotification(player, notification)
end

Media.PlaySoundNotification = function(notification, player)
	Internal.PlaySoundNotification(player, notification)
end

Media.PlayRandomMusic = function()
	Internal.PlayRandomMusic()
end

Media.PlayMovieFullscreen = function(movie, onComplete)
	if onComplete == nil then
		onComplete = function() end
	end
	Internal.PlayMovieFullscreen(movie, onComplete)
end