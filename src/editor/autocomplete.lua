------------
-- API

local function newAPI()
	return {
		-- tool tip info and reserved names
		tip = {
			staticnames = {},
			keys = {},
			finfo = {},
			finfoclass = {},
			shortfinfo = {},
			shortfinfoclass = {},
		},
		-- autocomplete hierarchy
		ac = {
			childs = {},
		},
	}
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

local function key ()
	return {type="keyword"}
end

local function fn (description) 
	local description2,returns,args = description:match("(.+)%-%s*(%b())%s*(%b())")
	if not description2 then
		return {type="function",description=description,
			returns="(?)"} 
	end
	return {type="function",description=description2,
		returns=returns:gsub("^%s+",""):gsub("%s+$",""), args = args} 
end

local function val (description)
	return {type="value",description = description}
end



function addAPI(apifile) -- relative to API directory
	local ftype = apifile:match("api[/\\]([^/\\]+)")
	if not ftype then
		print("The API file must be located in a subdirectory of the API directory\n")
		return
	end
	local fn,err = loadfile(apifile)
	if err then
		print("API file '"..apifile.."' could not be loaded: "..err.."\n")
		return
	end
	local env = apis[ftype] or newAPI()
	apis[ftype] = env
	env = env.ac.childs
	setfenv(fn,env)
	xpcall(function()fn(env)end, function(err)
		DisplayOutput("Error while loading API file: "..apifile..":\n")
		DisplayOutput(debug.traceback(err))
		DisplayOutput("\n")
	end)
end

function loadallAPIs ()
	for i,dir in ipairs(FileSysGet(".\\api\\*.*",wx.wxDIR)) do
		local files = FileSysGet(dir.."\\*.*",wx.wxFILE)
		for i,file in ipairs(files) do
			if file:match "%.lua$" then
				addAPI(file)
			end
		end
	end
end
loadallAPIs()



-- Lua wx specific
do 
	apis.lua.ac.childs.wx = {
		type = "lib",
		description = "WX lib",
		childs = {}
	}
	
	local wxchilds = apis.lua.ac.childs.wx.childs
	for key in pairs(wx) do
		wxchilds[key] = {
			type = "function",
			description = "unknown",
			returns = "unknown",
		}
	end
	
end

---------
-- ToolTip and reserved words list
-- also fixes function descriptions


local function fillTips(api,apibasename)
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

for i,api in pairs(apis) do
	fillTips(api,"")
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

function GetTipInfo(editor, content, short)
	local caller = content:match("([a-zA-Z_0-9]+)%(%s*$")
	local class  = caller and content:match("([a-zA-Z_0-9%.]+)[%.:]"..caller.."%(%s*$")
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
function removeDynamicWord (word)
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
function purgeDynamicWordlist ()
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
local function buildcache(childs)
	if cache[childs] then return cache[childs] end
	--DisplayOutput("1> build cache\n")
	cache[childs] = {}
	local t = cache[childs]
	
	for key, info in pairs(childs) do
		local kl = key:lower()
		--DisplayOutput("1> cache:"..kl.."\n")
		for i=0,#key do 
			local k = kl:sub(1,i)
			t[k] = t[k] or {}
			t[k][#t[k]+1] = key
		end
	end
	
	return t
end

-- make syntype dependent
function CreateAutoCompList(editor,key) 
	local api = editor.api
	--DisplayOutput(key,"\n")
	local tip = api.tip
	local ac = api.ac
	
	-- ignore keywords
	if tip.keys[key] then return end
	
	-- search in api autocomplete list
	-- track recursion depth
	local depth = 0
	
	UpdateAssignCache(editor)
	
	if (editor.assignscache) then
		local id,rest = key:match("([%w_.]+):(.*)")
		-- replace for lookup
		if (id and rest) then
			key = (editor.assignscache.assigns[id] or id)..":"..rest
		end
	end
	
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
	local tab,rest = findtab (key,ac)
	if not (tab and tab.childs) then return end
	
	if (depth < 1) then
		local obj,krest = rest:match("([a-zA-Z0-9_]+)[:%.]([a-zA-Z0-9_]+)%s*$")
		if (krest) then
			if (#krest < 3) then return end
			tab = tip.finfo
			rest = krest:gsub("[^a-zA-Z0-9_]","")
		else
			rest = rest:gsub("[^a-zA-Z0-9_]","")
		end
	else
		rest = rest:gsub("[^a-zA-Z0-9_]","")
	end

	-- final list (cached)
	local complete = buildcache(tab.childs or tab)

	
	
	local last = key : match "([a-zA-Z0-9_]+)%s*$"
		

	-- build dynamic word list 
	-- only if api search couldnt descend
	-- ie we couldnt find matching sub items
	local dw = ""
	if (depth < 1) then
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
	
	local compstr = ""
	if complete and complete[rest:lower()] then
		local list = complete[rest:lower()]
		
		if (#rest > 0) then
			table.sort(list,function(a,b)
				local ma,mb = a:sub(1,#rest)==last, b:sub(1,#rest)==rest
				if (ma and mb) or (not ma and not mb) then return a<b end
				return ma
			end)
		else
			table.sort(list)
		end
		compstr = table.concat(list," ")
	end
	
	-- concat final, list complete first
--	DisplayOutput("1> "..(rest or "").."- "..tostring(dw).."\n")
	return compstr .. dw

end
