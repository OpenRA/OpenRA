local cgbinpath = os.getenv("CG_BIN_PATH")


return cgbinpath and {
	fninit = function(frame,menuBar)
	
		local myMenu = wx.wxMenu{
			{ ID "cg.profile.arb",		"&ARB VP/FP",	"ARB vertex/fragment program profile", wx.wxITEM_CHECK },
			{ ID "cg.profile.glsl",		"ARB &GLSL",		"ARB vertex/fragment program profile", wx.wxITEM_CHECK },
			{ ID "cg.profile.nv40",		"NV VP/FP&40",	"NV vertex/fragment program 4 profile", wx.wxITEM_CHECK },
			{ ID "cg.profile.gp4",		"EXT &GP4",		"EXT vertex/fragment program 4 profile", wx.wxITEM_CHECK },
			{ },
			{ ID "cg.compile.input",	"&Custom Args\tCtrl-L",		"when set a popup for custom compiler args will be envoked", wx.wxITEM_CHECK },
			{ },
			{ ID "cg.compile.vertex",		"Compile &Vertex\tCtrl-U",		"Compile Vertex program (select entry word)" },
			{ ID "cg.compile.fragment",		"Compile &Fragment\tCtrl-I",	"Compile Fragment program (select entry word)" },
			{ ID "cg.compile.geometry",		"Compile &Geometry\tCtrl-J",	"Compile Geometry program (select entry word)" },
		}
		menuBar:Append(myMenu, "&CgCompiler")
		
		local data = {}
		data.customarg = false
		data.profid = ID "cg.profile.arb"
		data.domains = {
			[ID "cg.compile.vertex"]   = 1,
			[ID "cg.compile.fragment"] = 2,
			[ID "cg.compile.geometry"] = 3,
		}
		data.profiles = {
			[ID "cg.profile.arb"]  = {"arbvp1","arbfp1",false,".glp"},
			[ID "cg.profile.glsl"] = {"glslv","glslf",false,".glsl"},
			[ID "cg.profile.nv40"] = {"vp40","fp40",false,".glp"},
			[ID "cg.profile.gp4"]  = {"gp4vp","gp4fp","gp4gp",".glp"},
		}
		-- Profile related
		menuBar:Check(data.profid, true)
		
		local function selectProfile (id)
			for id,profile in pairs(data.profiles) do
				menuBar:Check(id, false)
			end
			menuBar:Check(id, true)
			data.profid = id
		end
		
		local function evSelectProfile (event)
			local chose = event:GetId()
			selectProfile(chose)
		end
		
		for id,profile in pairs(data.profiles) do
			frame:Connect(id,wx.wxEVT_COMMAND_MENU_SELECTED,evSelectProfile)
		end
		
		-- Compile Arg
		frame:Connect(ID "cg.compile.input",wx.wxEVT_COMMAND_MENU_SELECTED,
					function(event)
						data.customarg = event:IsChecked()
					end)
		-- Compile 
		local function evcompile(event)
			local filename,info = GetEditorFileAndCurInfo()
			
			if (not (filename and info.selword and cgbinpath)) then return end
			
			-- TODO popup for custom input
			local args = data.customarg and wx.wxGetTextFromUser("Compiler Args") or ""
			args = args:len() > 0 and args or nil
			
			local domain = data.domains[event:GetId()]
			local profile = data.profiles[data.profid]
			
			if (not profile[domain]) then return end
			
			local ext = 4
			local fullname = filename:GetFullPath()
			local cmdline = ""..fullname.." -profile "..profile[domain].." "
			cmdline = args and cmdline..args.." " or cmdline
			cmdline = cmdline.."-o "..fullname.."."..info.selword.."^"
			cmdline = args and cmdline..args:gsub("%s+%-",";-")..";^" or cmdline
			cmdline = cmdline..profile[domain]..profile[ext].." "
			cmdline = cmdline.."-entry "..info.selword
			
			cmdline = cgbinpath.."/cgc.exe "..cmdline
			
			-- run process
			RunCommandLine(cmdline,nil,true)
		end
		frame:Connect(ID "cg.compile.vertex",wx.wxEVT_COMMAND_MENU_SELECTED,evcompile)
		frame:Connect(ID "cg.compile.fragment",wx.wxEVT_COMMAND_MENU_SELECTED,evcompile)
		frame:Connect(ID "cg.compile.geometry",wx.wxEVT_COMMAND_MENU_SELECTED,evcompile)
	end,

}