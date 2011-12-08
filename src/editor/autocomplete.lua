-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide
local statusBar = ide.frame.statusBar

-- api loading depends on Lua interpreter 
-- and loaded specs

------------
-- API

local function newAPI(api)
	api = api or {}
	for i,v in pairs(api) do
		api[i] = nil
	end
	-- tool tip info and reserved names
	api.tip = {
		staticnames = {},
		keys = {},
		finfo = {},
		finfoclass = {},
		shortfinfo = {},
		shortfinfoclass = {},
	}
	-- autocomplete hierarchy
	api.ac = {
		childs = {},
	}
	
	return api
end


local apis = {
	none = newAPI(),
	lua = newAPI(),
}

local config = ide.config.ac

function GetApi(apitype)
	return apis[apitype] or apis["none"]
end

----------
-- API loading

local function addAPI(apifile,only,subapis,known) -- relative to API directory
	local ftype,fname = apifile:match("api[/\\]([^/\\]+)[/\\](.*)%.")
	if not ftype then
		print("The API file must be located in a subdirectory of the API directory\n")
		return
	end
	if ((only and ftype ~= only) or (known and not known[ftype])) then return end
	if (subapis and not subapis[fname]) then return end

	local fn,err = loadfile(apifile)
	if err then
		print("API file '"..apifile.."' could not be loaded: "..err.."\n")
		return
	end
	local mt
	local env = apis[ftype] or newAPI()
	apis[ftype] = env
	env = env.ac.childs
	local suc,res = xpcall(function()return fn(env)end, function(err)
		DisplayOutput("Error while loading API file: "..apifile..":\n")
		DisplayOutput(debug.traceback(err))
		DisplayOutput("\n")
	end)
	
	if (suc and res) then
		local function gennames(tab,prefix)
			for i,v in pairs(tab) do
				v.classname = (prefix and (prefix..".") or "")..i
				if(v.childs) then
					gennames(v.childs,v.classname)
				end
			end
		end
		gennames(res)
		for i,v in pairs(res) do
			env[i] = v
		end
	end
	
end

local function loadallAPIs (only,subapis,known)
	for i,dir in ipairs(FileSysGet(".\\api\\*.*",wx.wxDIR)) do
		local files = FileSysGet(dir.."\\*.*",wx.wxFILE)
		for i,file in ipairs(files) do
			if file:match "%.lua$" then
				addAPI(file,only,subapis,known)
			end
		end
	end
end

---------
-- ToolTip and reserved words list
-- also fixes function descriptions


local function fillTips(api,apibasename,apiname)
	local apiac = api.ac
	local tclass = api.tip

	tclass.staticnames = {}
	tclass.keys = {}
	tclass.finfo = {}
	tclass.finfoclass = {}
	tclass.shortfinfo = {}
	tclass.shortfinfoclass = {}

	local staticnames = tclass.staticnames
	local keys = tclass.keys
	local finfo = tclass.finfo
	local finfoclass = tclass.finfoclass
	local shortfinfo = tclass.shortfinfo
	local shortfinfoclass = tclass.shortfinfoclass

	local function traverse (tab,libname)
		if not tab.childs then return end
		for key,info in pairs(tab.childs) do
			traverse(info,key)
			if info.type == "function" then
				local libstr = libname ~= "" and libname.."." or ""
				
				-- fix description
				local frontname = (info.returns or "(?)").." "..libstr..key.." "..(info.args or "(?)")
				frontname = frontname:gsub("\n"," ")
				frontname = frontname:gsub("\t","")
				frontname = frontname:gsub("("..("[^\n]"):rep(60)..".-[%s,%)%]:%.])([^%)])","%1\n   %2")
				
				info.description = info.description:gsub("\n\n","<br>")
				info.description = info.description:gsub("[^%s]\n[^%s]","")
				info.description = info.description:gsub("\n"," ")
				info.description = info.description:gsub("<br>","\n")
				info.description = info.description:gsub("[^%s]\t[^%s]"," ")
				info.description = info.description:gsub("\t%s","")
				info.description = info.description:gsub("%s\t","")
				info.description = info.description:gsub("("..("[^\n]"):rep(60)..".-[%s,%)%]:%.])","%1\n")
				
				-- build info
				local inf = frontname.."\n"..info.description
				local sentence = info.description:match("^([^\n]+)\n.*")
				local sentence = sentence and sentence:match("([^%.]+)%..*$")
				local infshort = frontname.."\n"..(sentence and sentence.."..." or info.description)
				local infshortbatch = (info.returns and info.args) and frontname or infshort
				
				-- add to infoclass 
				if not finfoclass[libname] then finfoclass[libname] = {} end
				if not shortfinfoclass[libname] then shortfinfoclass[libname] = {} end
				finfoclass[libname][key] = inf
				shortfinfoclass[libname][key] = infshort
				
				-- add to info
				if not finfo[key] or #finfo[key]<200 then 
					if finfo[key] then finfo[key] = finfo[key] .. "\n\n"
					else finfo[key] = "" end
					finfo[key] = finfo[key] .. inf
				elseif not finfo[key]:match("\n %(%.%.%.%)$") then
					finfo[key] = finfo[key].."\n (...)"
				end
				
				-- add to shortinfo
				if not shortfinfo[key] or #shortfinfo[key]<200 then 
					if shortfinfo[key] then shortfinfo[key] = shortfinfo[key] .. "\n"
					else shortfinfo[key] = "" end
					shortfinfo[key] = shortfinfo[key] .. infshortbatch
				elseif not shortfinfo[key]:match("\n %(%.%.%.%)$") then
					shortfinfo[key] = shortfinfo[key].."\n (...)"
				end
			end
			if info.type == "keyword" then
				keys[key] = true
			end
			staticnames[key] = true
		end
	end
	traverse(apiac,apibasename)
end

local function generateAPIInfo(only)
	for i,api in pairs(apis) do
		if ((not only) or i == only) then
			fillTips(api,"",i)
		end
	end
end

function UpdateAssignCache(editor)
	if (editor.spec.typeassigns and not editor.assignscache) then
		local assigns = editor.spec.typeassigns(editor)
		editor.assignscache = {
			assigns = assigns,
			line = editor:GetCurrentLine(),
		}
	end
end

-- assumes a tidied up string (no spaces, braces..)
local function resolveAssign(editor,tx)
	local ac = editor.api.ac
	local assigns = editor.assignscache and editor.assignscache.assigns
	local function getclass(tab,a)
		local key,rest = a:match("([%w_]+)[%.:](.*)")
		if (key and rest and tab.childs and tab.childs[key]) then
			return getclass(tab.childs[key],rest)
		end
		if (tab.valuetype) then
			return getclass(ac,tab.valuetype.."."..a)
		end
		return tab,a
	end
	
	local classname
	local c = ""
	if (assigns) then
		-- find assign
		for w,s in tx:gmatch("([%w_]*)([%.:]?)") do
			
			local old = classname
			classname = classname or (assigns[c..w])
			if (s ~= "" and old ~= classname) then
				c = classname..s
			else
				c = c..w..s
			end
		end
	else
		c = tx
	end
	-- then work from api
	return getclass(ac,c)
end

function GetTipInfo(editor, content, short)
	local caller = content:match("([%w_]+)%(%s*$")
	local class  = caller and content:match("([%w_%.]+)[%.:]"..caller.."%(%s*$")
	local tip = editor.api.tip
		
	local classtab = short and tip.shortfinfoclass or tip.finfoclass
	local funcstab = short and tip.shortfinfo or tip.finfo
	
	UpdateAssignCache(editor)
	
	if (editor.assignscache and not (class and classtab[class])) then
		local assigns = editor.assignscache.assigns
		class = assigns and assigns[class] or class
	end
	
	return caller and (class and classtab[class]) and classtab[class][caller] or funcstab[caller]
end

local function reloadAPI(only,subapis)
	newAPI(apis[only])
	loadallAPIs(only,subapis)
	generateAPIInfo(only)
end


function ReloadLuaAPI()
	local interpreterapi = ide.interpreters[ide.config.interpreter]
	interpreterapi = interpreterapi and interpreterapi.api
	if (interpreterapi) then
		local apinames = {}
		for i,v in ipairs(interpreterapi) do
			apinames[v] = true
		end
		interpreterapi = apinames
	end
	reloadAPI("lua",interpreterapi)
end

do
	local known = {}
	for n,spec in pairs(ide.specs) do
		if (spec.api) then
			known[spec.api] = true
		end
	end
	-- by defaul load every known api except lua
	known.lua = false
	
	loadallAPIs(nil,nil,known)
	generateAPIInfo()
end

-------------
-- Dynamic Words

local dywordentries = {}
local dynamicwords = {}
local function addDynamicWord (api,word )
	if ide.config.acandtip.nodynwords then return end
	if api.tip.staticnames[word] then return end
	local cnt = dywordentries[word]
	if cnt then 
		dywordentries[word] = cnt +1
		return 
	end
	dywordentries[word] = 1
	for i=0,#word do 
		local k = word : sub (1,i)
		dynamicwords[k] = dynamicwords[k] or {}
		table.insert(dynamicwords[k], word)
	end
end
local function removeDynamicWord (word)
	local cnt = dywordentries[word]
	if not cnt then return end
	
	if (cnt == 1) then
		dywordentries[word] = nil
		for i=0,#word do 
			local k = word : sub (1,i) : lower()
			if not dynamicwords[k] then break end
			for i=1,#dynamicwords[k] do
				if dynamicwords[i] == word then
					table.remove(dynamicwords,i)
					break
				end
			end
		end
	else
		dywordentries[word] = cnt - 1
	end
end
local function purgeDynamicWordlist ()
	dywordentries = {}
	dynamicwords = {}
end

function AddDynamicWordsCurrent(editor,content)
	local api = editor.api

	for word in content:gmatch "([a-zA-Z_]+[a-zA-Z_0-9]+)[^a-zA-Z0-9_\r\n]" do
		addDynamicWord(api,word)
	end
end

function RemDynamicWordsCurrent(editor,content)
	for word in content:gmatch "([a-zA-Z_]+[a-zA-Z_0-9]+)[^a-zA-Z0-9_\r\n]" do
		removeDynamicWord(word)
	end
end

function AddDynamicWords (editor)
	local api = editor.api
	local content = editor:GetText()

	--
	-- TODO check if inside comment
	for word in content:gmatch "([a-zA-Z_]+[a-zA-Z_0-9]+)" do
		addDynamicWord(api,word)
	end
end

------------
-- Final Autocomplete

local cache = {}
local laststrategy
local function getAutoCompApiList(childs,fragment)
	fragment = fragment:lower()
	local strategy = ide.config.acandtip.strategy 
	if (laststrategy ~= strategy) then cache = {}; laststrategy = strategy end
	
	if (strategy == 2) then
		local wlist = cache[childs]
		if not wlist then 
			wlist = " "
			for i,v in pairs(childs) do
				wlist = wlist..i.." "
			end
			cache[childs] = wlist
		end
		local ret = {}
		local g = string.gmatch
		local pat = fragment ~= "" and ("%s("..fragment:gsub(".",
			function(c) 
				local l = c:lower()..c:upper()
				return "["..l.."][%w_]*" 
			end)..")") or "([%w_]+)"
		pat = pat:gsub("%s","")
		for c in g(wlist,pat) do
			table.insert(ret,c)
		end
		
		return ret
	end
	
	if cache[childs] then 
		return cache[childs][fragment]
	end
	
	local t = {}
	cache[childs] = t
	
	local sub = strat == 1
	for key, info in pairs(childs) do
		local used = {}
		--
		local kl = key:lower()
		for i=0,#key do 
			local k = kl:sub(1,i)
			t[k] = t[k] or {}
			used[k] = true
			table.insert(t[k],key)
		end
		if (sub) then
			-- find camel case / _ separated subwords
			-- glfwGetGammaRamp -> g, gg, ggr
			-- GL_POINT_SPRIT -> g, gp, gps
			local last = ""
			for ks in string.gmatch(key,"([A-Z%d]*[a-z%d]*_?)") do
				local k = last..(ks:sub(1,1):lower())
				last = k
				
				t[k] = t[k] or {}
				if (not used[k]) then
					used[k] = true
					table.insert(t[k],key)
				end
			end
		end
	end
	
	return t
end

function ClearAutoCompCache()
	cache = {}
end

-- make syntype dependent
function CreateAutoCompList(editor,key) 
	local api = editor.api
	--DisplayOutput(key,"\n")
	local tip = api.tip
	local ac = api.ac
	
	-- ignore keywords
	if tip.keys[key] then return end
	
	UpdateAssignCache(editor)
	
--[[
	-- override class based on assign cache
	if (editor.assignscache) then
		local id,sep,rest = key:match("([%w_%.]+)%s*([:%.])%s*(.*)")
		-- replace for lookup
		if (id and rest) then
			key = (editor.assignscache.assigns[id] or id)..sep..rest
		end
	end
	
	-- search in api autocomplete list
	-- track recursion depth
	local depth = 0
	local function findtab (rest,tab)
		local key,krest = rest:match("([%w_]+)(.*)")
		
		--DisplayOutput("2> "..rest.." : "..(key or "nil").." : "..tostring(krest).."\n")
	
		-- check if we can go down hierarchy
		if krest and #(krest:gsub("[%s]",""))>0 and tab.childs and tab.childs[key] then 
			depth = depth + 1
			return findtab(krest,tab.childs[key]) 
		end
		
		return tab,rest
	end
]]
	local tab,rest = resolveAssign(editor,key)
	local progress = tab and tab.childs
	statusBar:SetStatusText(progress and tab.classname or "",1)
	if not (progress) then return end
	
	
	--DisplayOutput("AC",tab.classname,rest,"\n")
	
	if (tab == ac) then
		local obj,krest = rest:match("([%w_]+)[:%.]([%w_]+)%s*$")
		if (krest) then
			if (#krest < 3) then return end
			tab = tip.finfo
			rest = krest:gsub("[^%w_]","")
		else
			rest = rest:gsub("[^%w_]","")
		end
	else
		rest = rest:gsub("[^%w_]","")
	end

	local last = key:match "([%w_]+)%s*$"
	

	-- build dynamic word list 
	-- only if api search couldnt descend
	-- ie we couldnt find matching sub items
	local dw = ""
	if (tab == ac) then
		if dynamicwords[last] then
			local list = dynamicwords[last]
			table.sort(list,function(a,b)
				local ma,mb = a:sub(1,#last)==last, b:sub(1,#last)==last
				if (ma and mb) or (not ma and not mb) then return a<b end
				return ma
			end)
			dw = " " .. table.concat(list," ")
		end
	end

	-- list from api
	local apilist = getAutoCompApiList(tab.childs or tab,rest)
	local compstr = ""
	if apilist then
		if (#rest > 0) then
			local strategy = ide.config.acandtip.strategy
			
			if (strategy == 2 and #apilist < 128) then
				local pat = rest:gsub(".",function(c) 
					local l = c:lower()..c:upper()
					return "["..l.."]([^"..l.." ]*)" 
				end)

				local g = string.gsub
				table.sort(apilist,function(a,b)
					local ma,mb = 0,0
					g(a,pat,function(...)
						local l = {...}
						for i,v in ipairs(l) do
							ma = ma + ((v=="") and 0 or 1)
						end
					end)
					g(b,pat,function(...)
						local l = {...}
						for i,v in ipairs(l) do
							mb = mb + ((v=="") and 0 or 1)
						end
					end)
					
					if (ma == mb) then return a:lower()<b:lower() end
					return ma<mb
				end)
			else
				table.sort(apilist,function(a,b)
					local ma,mb = a:sub(1,#rest)==rest, b:sub(1,#rest)==rest
					if (ma and mb) or (not ma and not mb) then return a<b end
					return ma
				end)
			end
		else
			table.sort(apilist)
		end
		compstr = table.concat(apilist," ")
	end
	
	-- concat final, list complete first
--	DisplayOutput("1> "..(rest or "").."- "..tostring(dw).."\n")
	return compstr .. dw

end
