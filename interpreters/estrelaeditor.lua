return {
		name = "Estrela Editor",
		description = "Estrela Editor as run target (IDE development)",
		api = {"wx","baselib"},
		fcmdline = function(filepath) 
				return ide.editorFilename and '"'..ide.editorFilename..'" '..(filepath or "")..' -cfg "singleinstance=false;"' or nil
			end,
		fprojdir = function(fname)
				return fname:GetPath(wx.wxPATH_GET_VOLUME)
			end,
		fworkdir = function() end, -- better not
		capture = false,
		nohide  = true,
	}