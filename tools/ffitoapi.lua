-- author: Christoph Kubisch
---------------------------------------------------------

local function ffiToApi(ffidef)
	ffidef = StripCommentsC(ffidef)

	local enums = {}
	local funcs = {}

	-- extract function names
	local curfunc
	local function closefunc()
		-- parse args
		-- skip return void types
		
		table.insert(funcs,curfunc)
		curfunc = nil
	end
	for l in ffidef:gmatch("[^\r\n]+") do 
		local typedef = l:match("typedef%s+")
		local ret,name,args = string.match(typedef and "" or l,"([_%w]+)%s+%(?[%s%*]*([_%w]+)%s*%)?%s*(%(.*")
		if (not (curfunc or typedef) and (ret and name and args)) then
			curfunc = {ret=ret,name=name,args=args}
			if (args:match(";")) then
				registerfunc()
			end
		elseif (curfunc) then
			curfunc.args = curfunc.args..l
			if (curfunc.args:match(";")) then
				registerfunc()
			end
		end
	end
	
	-- search for enums
	
	-- serialize api string

end


return {
	exec = {
		name = "luajit ffi string to Estrela api",
		description = "converts current file to api",
		fn = function(wxfname,projectdir)
			-- get cur editor text
			-- replace text
		end,
	},
}