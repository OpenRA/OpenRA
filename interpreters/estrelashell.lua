return {
		name = "Estrela Shell",
		description = "Estrela Lua Shell",
		api = {"wx","baselib"},
		fcmdline = function(filepath) 
				-- set shellbox for focus
				local bottomnotebook = ide.frame.vsplitter.splitter.bottomnotebook
				bottomnotebook:SetSelection(1)
				if ide.frame.menuBar:IsChecked(ID_CLEAROUTPUT) then
					local shellLog = bottomnotebook.shellbox.output
					shellLog:SetReadOnly(false)
					shellLog:ClearAll()
					shellLog:SetReadOnly(true)
				end
				
				ExecuteShellboxCode(nil,filepath)
				return nil
			end,
		fprojdir = function(fname)
				return fname:GetPath(wx.wxPATH_GET_VOLUME)
			end,
		fworkdir = function(filepath) 
				return filepath:GetPath(wx.wxPATH_GET_VOLUME)
			end, 
		capture = false,
		nohide  = true,
	}