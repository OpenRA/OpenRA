return {
		name = "Estrela Shell",
		description = "Estrela Lua Shell",
		api = {"wxwidgets","baselib"},
		frun = function(self,wfilename) 
				-- set shellbox for focus
				local bottomnotebook = ide.frame.vsplitter.splitter.bottomnotebook
				bottomnotebook:SetSelection(1)
				if ide.frame.menuBar:IsChecked(ID_CLEAROUTPUT) then
					local shellLog = bottomnotebook.shellbox.output
					shellLog:SetReadOnly(false)
					shellLog:ClearAll()
					shellLog:SetReadOnly(true)
				end
				
				ShellExecuteCode(nil,wfilename)
			end,
		fprojdir = function(self,wfilename)
				return wfilename:GetPath(wx.wxPATH_GET_VOLUME)
			end,
	}