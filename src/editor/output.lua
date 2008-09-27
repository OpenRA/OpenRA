local frame = ide.frame
local splitter = frame.vsplitter.splitter
local notebook = splitter.notebook
local bottomnotebook = splitter.bottomnotebook
local errorlog = bottomnotebook.errorlog

-------
-- setup errorlog
errorlog:Show(true)
errorlog:SetFont(ide.ofont)
errorlog:StyleSetFont(wxstc.wxSTC_STYLE_DEFAULT, ide.ofont)
errorlog:StyleClearAll()
errorlog:SetMarginWidth(1, 16) -- marker margin
errorlog:SetMarginType(1, wxstc.wxSTC_MARGIN_SYMBOL);
errorlog:MarkerDefine(CURRENT_LINE_MARKER, wxstc.wxSTC_MARK_ARROWS, wx.wxBLACK, wx.wxWHITE)
errorlog:SetReadOnly(true)
StylesApplyToEditor(ide.config.stylesoutshell,errorlog,ide.ofont,ide.ofontItalic)


function ClearOutput(event)
	errorlog:SetReadOnly(false)
	errorlog:ClearAll()
	errorlog:SetReadOnly(true)
end

function DisplayOutput(message, dont_add_marker)
	--if splitter:IsSplit() == false then
	--	local w, h = ide.frame:GetClientSizeWH()
	--	splitter:SplitHorizontally(notebook, bottomnotebook, h - ide.config.view.splitterheight)
	--end
	if not dont_add_marker then
		errorlog:MarkerAdd(errorlog:GetLineCount()-1, CURRENT_LINE_MARKER)
	end
	errorlog:SetReadOnly(false)
	errorlog:AppendText(message)
	errorlog:SetReadOnly(true)
	errorlog:GotoPos(errorlog:GetLength())
end


local streamin   = nil
local streamerr  = nil
local streamout  = nil

local customproc = nil
local custompid  = -1


function RunCommandLine(cmd,wdir,tooutput)
	if (customproc and tooutput) and customproc.Exists(tonumber(tostring(custompid))) then
		DisplayOutput("Conflicting Process still running: "..cmd.."\n")
		return
	end

	DisplayOutput("Running program: "..cmd.."\n")
	
	
	local pid = -1
	local proc = nil
	
	if (tooutput) then
		customproc = wx.wxProcess(errorlog)
		customproc:Redirect()

		proc = customproc
	end
	
	-- manipulate working directory
	local oldcwd 
	if (wdir) then
		oldcwd = wx.wxFileName.GetCwd()
		oldcwd = wx.wxFileName.SetCwd(wdir) and oldcwd
	end
	
	-- launch process
	local pid = proc and wx.wxExecute(cmd, wx.wxEXEC_ASYNC,proc) or
						 wx.wxExecute(cmd, wx.wxEXEC_ASYNC)

	if (oldcwd) then
		wx.wxFileName.SetCwd(oldcwd)
	end
	
	-- check process
	if not pid or pid == -1 then
		DisplayOutput("Unknown ERROR Running program!\n", true)
		customproc = nil
		return
	else
		DisplayOutput("Process id is: "..tostring(pid).."\n", true)
	end
	
	streamin  = proc and proc:GetInputStream()
	streamerr = proc and proc:GetErrorStream()
	--streamout = proc and proc:GetOutputStream()
	
	custompid = proc and pid or -1
	DisplayOutput("Process streams are: "..tostring(streamin).."/"..tostring(streamerr).."\n", true)
end

local function getStreams()
	if (streamin) then
		while(streamin:CanRead()) do
			str = streamin:Read(4096)
			DisplayOutput(str,true)
		end
	end
	if (streamerr) then
		while (streamerr:CanRead()) do
			str = streamerr:Read(4096)
			DisplayOutput(str,true)
		end
	end
end

errorlog:Connect(wx.wxEVT_END_PROCESS, function(event)
			if (event:GetPid() == custompid) then
				getStreams()
				streamin  = nil
				streamerr = nil
				streamout = nil
				customproc = nil
				DisplayOutput("proc end "..custompid.."\n")
			end
		end)

errorlog:Connect(wx.wxEVT_IDLE, function(event)
		if (streamin or streamerr) then
			getStreams()
		end
	end)

local jumptopatterns = {
    -- <filename>(line,linepos): 
	"%s*([%w:/%\\_%-%.]+)%((%d+),(%d+)%):",
	-- <filename>(line): 
	"%s*([%w:/%\\_%-%.]+)%((%d+).*%):",
	-- <filename>:line:
	"%s*([%w:/%\\_%-%.]+):(%d+):",
	--[string "<filename>"]:line:
	'.*%[string "([%w:/%\\_%-%.]+)"%]:(%d+):',
}

errorlog:Connect(wxstc.wxEVT_STC_DOUBLECLICK,
		function(event)
			local line = errorlog:GetCurrentLine()
			local linetx = errorlog:GetLine(line)
			-- try to detect a filename + line
			-- in linetx
			
			local fname
			local jumpline 
			local jumplinepos
			
			for i,pattern in ipairs(jumptopatterns) do
				fname,jumpline,jumplinepos = linetx:match(pattern)
				if (fname and jumpline) then
					break
				end
			end
			
			
			if (fname and jumpline) then
				LoadFile(fname,nil,true)
				local editor = GetEditor()
				if (editor) then
					jumpline = tonumber(jumpline)
					jumplinepos = tonumber(jumplinepos)
					
					--editor:ScrollToLine(jumpline)
					editor:GotoPos(editor:PositionFromLine(jumpline) + (jumplinepos or 0))
				end
			end
			
		end)