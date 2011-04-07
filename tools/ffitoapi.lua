-- author: Christoph Kubisch
---------------------------------------------------------

local function ffiToApi(ffidef)
	local str = ffidef
	str = ffidef:match("(%-%-%[%[.+%]%])")
	local header = ffidef:match("[^\r\n]+")
	ffidef = StripCommentsC(ffidef)

	local enums = {}
	local funcs = {}
	local values = {}
	
	-- extract function names
	local curfunc
	local function registerfunc()
		local fn = curfunc
		-- parse args
		local args = fn.ARGS:match("%(%s*(.-)%s*%);")
		fn.ARGS = "("..args..")"
		
		-- skip return void types
		fn.RET = fn.RET == "void" and "" or fn.RET
		fn.RET = "("..fn.RET..")"
		fn.DESCR = ""
		
		table.insert(funcs,curfunc)
		curfunc = nil
	end
	
	local outer = ffidef:gsub("(%b{})","{}")
	
	for l in outer:gmatch("[^\r\n]+") do 
		-- extern void func(blubb);
		-- extern void ( * func )(blubb);
		-- void func(blubb);
		-- void ( * func )(blubb);
		local typedef = l:match("typedef%s+")
		local ret,name,args = string.match(typedef and "" or l,
			"[_%w]-%s*([_%w]+)%s+%(?[%s%*]*([_%w]+)%s*%)?%s*(%([^%(]*;)")
			-- FIXME pattern doesnt recognize multiline args
		if (not (curfunc or typedef) and (ret and name and args)) then
			curfunc = {RET=ret,NAME=name,ARGS=args}
			if (args:match(";")) then
				registerfunc()
			end
		elseif (curfunc) then
			curfunc.ARGS = curfunc.ARGS..l
			if (curfunc.ARGS:match(";")) then
				registerfunc()
			end
		elseif (not typedef) then
			local name,val = l:match("static%s+const%s+[_%w]+%s+([_%w]+)%s*=%s*([_%w]+)%s*")
			local name = name or l:match("([_%w]+)%s*;")
			if (name) then
				table.insert(values,{NAME=name, DESCR=val or ""})
			end
		end
	end 
	
	-- search for enums
	for def in ffidef:gmatch("enum[_%w%s\r\n]*(%b{})[_%w%s\r\n]*;") do
		for enum in def:gmatch("([_%w]+).-[,}]") do
			table.insert(enums,{NAME=enum})
		end
	end
	
	-- serialize api string
	local function serialize(str,id,tab)
		for i,k in ipairs(tab) do
			str = str..string.gsub(id,"%$([%w]+)%$",k)
		end
		return str
	end
	
	str = str..[[

--auto-generated api from ffi headers

local api = {
]]
	local value = 
[[  ["$NAME$"] = { type ='value', description = "$DESCR$", },
]]
	local enum = 
[[  ["$NAME$"] = { type ='value', },
]]
	local funcdef =
[[  ["$NAME$"] = { type ='function', 
      description = "$DESCR$", 
      returns = "$RET$",
      args = "$ARGS$", },
]]
	str = serialize(str,value,values)
	str = serialize(str,enum,enums)
	str = serialize(str,funcdef,funcs)

	str = str..[[
}

return {
]]

	local class =
[[
  $NAME$ = {
    type = 'lib',
    description = "$DESCR$",
    childs = $API$,
  },
]]

	local description = header:match("|%s*(.*)")
	local prefixes = header:match("(.-)%s*|")
	local classes = {}
	for prefix in prefixes:gmatch("([_%w]+)") do 
		local p = {NAME=prefix, DESCR = description, API="api"}
		table.insert(classes,p)
	end

	str = serialize(str,class,classes)
	str = str..[[
}
]]

	return str
end

local function exec(wxfname,projectdir)
	-- get cur editor text
	local editor = GetEditor()
	if (not editor) then end
	local tx = editor:GetText()
	tx = ffiToApi(tx)
	-- replace text
	editor:SetText(tx)
end

if (RELPATH) then
	ffitoapiExec = exec
end

return {
	exec = {
		name = "luajit ffi string to Estrela api",
		description = "converts current file to api",
		fn = exec,
	},
}