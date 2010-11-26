local cgbinpath = ide.config.path.cgbin or os.getenv("CG_BIN_PATH")


return cgbinpath and {
	fninit = function(frame,menuBar)
	
		local myMenu = wx.wxMenu{
			{ ID "cg.profile.arb",		"&ARB VP/FP",	"ARB vertex/fragment program profile", wx.wxITEM_CHECK },
			{ ID "cg.profile.glsl",		"ARB &GLSL",		"ARB vertex/fragment program profile", wx.wxITEM_CHECK },
			{ ID "cg.profile.nv40",		"NV VP/FP&40",	"NV vertex/fragment program sm3 profile", wx.wxITEM_CHECK },
			{ ID "cg.profile.gp4",		"NV &GP4",		"NV vertex/fragment program sm4 profile", wx.wxITEM_CHECK },
			{ ID "cg.profile.gp5",		"NV &GP5",		"NV vertex/fragment program sm5 profile", wx.wxITEM_CHECK },
			{ },
			{ ID "cg.compile.input",	"&Custom Args\tCtrl-L",		"when set a popup for custom compiler args will be envoked", wx.wxITEM_CHECK },
			{ },
			{ ID "cg.compile.vertex",		"Compile &Vertex\tCtrl-U",		"Compile Vertex program (select entry word)" },
			{ ID "cg.compile.fragment",		"Compile &Fragment\tCtrl-I",	"Compile Fragment program (select entry word)" },
			{ ID "cg.compile.geometry",		"Compile &Geometry\tCtrl-J",	"Compile Geometry program (select entry word)" },
			{ ID "cg.compile.tessctrl",		"Compile T.Ctrl",	"Compile T.Ctrl program (select entry word)" },
			{ ID "cg.compile.tesseval",		"Compile T.Eval",	"Compile T.Eval program (select entry word)" },
			{ },
			{ ID "cg.format.asm",		"Annotate ASM",	"indent and add comments to Cg ASM output" },
		}
		menuBar:Append(myMenu, "&CgCompiler")
		
		local data = {}
		data.customarg = false
		data.profid = ID "cg.profile.arb"
		data.domains = {
			[ID "cg.compile.vertex"]   = 1,
			[ID "cg.compile.fragment"] = 2,
			[ID "cg.compile.geometry"] = 3,
			[ID "cg.compile.tessctrl"] = 4,
			[ID "cg.compile.tesseval"] = 5,
		}
		data.profiles = {
			[ID "cg.profile.arb"]  = {"arbvp1","arbfp1",false,false,false,ext=".glp"},
			[ID "cg.profile.glsl"] = {"glslv","glslf",false,false,false,ext=".glsl"},
			[ID "cg.profile.nv40"] = {"vp40","fp40",false,false,false,ext=".glp"},
			[ID "cg.profile.gp4"]  = {"gp4vp","gp4fp","gp4gp",false,false,ext=".glp"},
			[ID "cg.profile.gp5"]  = {"gp5vp","gp5fp","gp5gp","gp5tcp","gp5tep",ext=".glp"},
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
			
			
			if (not (filename and info.selword and cgbinpath)) then 
				DisplayOutput("Error: Cg Compile: Insufficient parameters (nofile / not selected entry function!\n")
				return 
			end
			
			-- popup for custom input
			local args = data.customarg and wx.wxGetTextFromUser("Compiler Args") or ""
			args = args:len() > 0 and args or nil
			
			local domain = data.domains[event:GetId()]
			local profile = data.profiles[data.profid]
			
			if (not profile[domain]) then return end
			
			local ext = "ext"
			local fullname = filename:GetFullPath()
			local glsl = fullname:match("%.glsl$") and true
			
			local cmdline = ""..fullname.." -profile "..profile[domain].." "
			cmdline = glsl and cmdline.."-oglsl " or cmdline
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
		frame:Connect(ID "cg.compile.tessctrl",wx.wxEVT_COMMAND_MENU_SELECTED,evcompile)
		frame:Connect(ID "cg.compile.tesseval",wx.wxEVT_COMMAND_MENU_SELECTED,evcompile)
		
		-- indent asm
		frame:Connect(ID "cg.format.asm", wx.wxEVT_COMMAND_MENU_SELECTED,
			function(event)
				local curedit = GetEditor()
				local tx = curedit:GetText()
				local newtx = ""
				local indent = 0
				local delta = 2
				local startindent = {
					"IF","REP","ELSE",
				}
				local endindent = {
					"ENDIF","ENDREP","ELSE",
				}
				
				local function checkstart(str,tab)
					local res = false
					for i,v in ipairs(tab) do
						res = res or ((string.find(str,v) or 0) == 1)
					end
					return res
				end
				
				local argregistry = {}
				
				local function checkargs(str)
					local comment = "#"
					for i in string.gmatch(str,"([%[%]%w]+)") do
						local descr = argregistry[i]
						if (descr) then
							comment = comment.." "..i.." = "..descr
						end
					end
					
					return comment ~= "#" and comment
				end

				for w in string.gmatch(tx, "[^\n]*\n") do
					local vtype,vname,sem,resource,pnum,pref = string.match(w,"#var (%w+) ([%[%]%._%w]+) : ([^%:]*) : ([^%:]*) : ([^%:]*) : (%d*)")
					if (pref == "1") then
						local descriptor = vtype.." "..vname
					
						-- check if resource is array
						local resstart,rescnt = string.match(resource,"c%[(%d+)%], (%d+)")
						resstart = tonumber(resstart)
						rescnt = tonumber(rescnt)
						
						-- check if texture
						local texnum = string.match(resource,"texunit (%d+)")
						
						local argnames = {}
						if (rescnt) then
							for i=0,(rescnt-1) do
								table.insert(argnames,"c["..tostring(resstart + i).."]")
							end
						elseif (texnum) then
							table.insert(argnames,"texture["..tostring(texnum).."]")
							table.insert(argnames,"texture"..tostring(texnum))
						else
							table.insert(argnames,resource)
						end
						
						for i,v in ipairs(argnames) do
							argregistry[v] = descriptor
						end
					end
				
					if (checkstart(w,endindent)) then
						indent = indent - delta
					end
					local firstchar = string.sub(w,1,1)
					local indentstr = (firstchar ~= " " and  string.rep(" ",indent) or "")
					local linestr = indentstr..w
					local argcomment = (firstchar ~= "#") and checkargs(w)
					newtx = newtx..(argcomment and (indentstr..argcomment.."\n") or "")
					newtx = newtx..linestr
					if (checkstart(w,startindent)) then
						indent = indent + delta
					end
				end
				
				curedit:SetText(newtx)
			end)
	end,

}