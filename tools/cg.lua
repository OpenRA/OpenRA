-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

local binpath = ide.config.path.cgbin or os.getenv("CG_BIN_PATH")
local cgprofile 
local cgglsles 

return binpath and {
  fninit = function(frame,menuBar)
    cgprofile = ide.config.cgprofile or "gp5"
    cgglsles = ide.config.cgglsles
    
    if (wx.wxFileName(binpath):IsRelative()) then
      local editorDir = string.gsub(ide.editorFilename:gsub("[^/\\]+$",""),"\\","/")
      binpath = editorDir..binpath
    end

    local myMenu = wx.wxMenu{
      { ID "cg.profile.arb", "&ARB VP/FP", "ARB program profile", wx.wxITEM_CHECK },
      { ID "cg.profile.glsl", "ARB &GLSL", "ARB GLSL program profile", wx.wxITEM_CHECK },
      { ID "cg.profile.nv40", "NV VP/FP&40", "NV program sm3 profile", wx.wxITEM_CHECK },
      { ID "cg.profile.gp4", "NV &GP4", "NV program sm4 profile", wx.wxITEM_CHECK },
      { ID "cg.profile.gp5", "NV &GP5", "NV program sm5 profile", wx.wxITEM_CHECK },
      { ID "cg.profile.dx_2x", "DX SM&2_x", "DirectX sm2_x profile", wx.wxITEM_CHECK },
      { ID "cg.profile.dx_3", "DX SM&3_0", "DirectX sm3_0 profile", wx.wxITEM_CHECK },
      { ID "cg.profile.dx_4", "DX SM&4_0", "DirectX sm4_0 profile", wx.wxITEM_CHECK },
      { ID "cg.profile.dx_5", "DX SM&5_0", "DirectX sm5_0 profile", wx.wxITEM_CHECK },
      { },
      { ID "cg.compile.input", "&Custom Args", "when set a popup for custom compiler args will be envoked", wx.wxITEM_CHECK },
      { ID "cg.compile.gles", "GLSL-ES", "When GLSL file is source use GLSL-ES path", wx.wxITEM_CHECK },
      { },
      { ID "cg.compile.vertex", "Compile &Vertex", "Compile Vertex program (select entry word)" },
      { ID "cg.compile.fragment", "Compile &Fragment", "Compile Fragment program (select entry word)" },
      { ID "cg.compile.geometry", "Compile &Geometry", "Compile Geometry program (select entry word)" },
      { ID "cg.compile.tessctrl", "Compile T.Ctrl", "Compile T.Ctrl program (select entry word)" },
      { ID "cg.compile.tesseval", "Compile T.Eval", "Compile T.Eval program (select entry word)" },
      { ID "cg.compile.compute", "Compile Compute", "Compile Compute program (select entry word)" },
      { },
      { ID "cg.format.asm", "Annotate ASM", "indent and add comments to Cg ASM output" },
      { ID "cg.format.master", "Build from master", "Creates a new cg file from a master containing special include instrctions." },
    }
    menuBar:Append(myMenu, "&Cg")

    local data = {}
    data.customarg = false
    data.custom = ""
    data.profid = ID ("cg.profile."..cgprofile)
    data.gles = cgglsles and true or false
    data.domains = {
      [ID "cg.compile.vertex"] = 1,
      [ID "cg.compile.fragment"] = 2,
      [ID "cg.compile.geometry"] = 3,
      [ID "cg.compile.tessctrl"] = 4,
      [ID "cg.compile.tesseval"] = 5,
      [ID "cg.compile.compute"] = 6,
    }
    data.profiles = {
      [ID "cg.profile.arb"] = {"arbvp1","arbfp1",false,false,false,false,ext=".glp", asm=true,},
      [ID "cg.profile.glsl"] = {"glslv","glslf","glslg",false,false,false,ext=".glsl"},
      [ID "cg.profile.nv40"] = {"vp40","fp40",false,false,false,false,ext=".glp",nvperf=true, asm=true,},
      [ID "cg.profile.gp4"] = {"gp4vp","gp4fp","gp4gp",false,false,false,ext=".glp",nvperf=true, asm=true,},
      [ID "cg.profile.gp5"] = {"gp5vp","gp5fp","gp5gp","gp5tcp","gp5tep","gp5cp",ext=".glp", asm=true,},
      [ID "cg.profile.dx_2x"] = {"vs_2_0","ps_2_x",false,false,false,false,ext=".txt"},
      [ID "cg.profile.dx_3"] = {"vs_3_0","ps_3_0",false,false,false,false,ext=".txt"},
      [ID "cg.profile.dx_4"] = {"vs_4_0","ps_4_0","gs_4_0",false,false,false,ext=".txt"},
      [ID "cg.profile.dx_5"] = {"vs_5_0","ps_5_0","gs_5_0","ds_5_0","hs_5_0",false,ext=".txt"},
    }
    data.domaindefs = {
      " -D_VERTEX_ ",
      " -D_FRAGMENT_ ",
      " -D_GEOMETRY_ ",
      " -D_TESS_CONTROL_ ",
      " -D_TESS_EVAL_ ",
      " -D_COMPUTE_ ",
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

    menuBar:Check(ID "cg.compile.gles", data.gles)

    local function evSelectProfile (event)
      local chose = event:GetId()
      selectProfile(chose)
    end

    for id,profile in pairs(data.profiles) do
      frame:Connect(id,wx.wxEVT_COMMAND_MENU_SELECTED,evSelectProfile)
    end

    -- check for NvPerf
    local perfexe = "/NVShaderPerf.exe"
    local fn = wx.wxFileName(binpath..perfexe)
    local hasperf = fn:FileExists()

    -- master file generator

    local function buildFromMaster(filenamein, filenameout)
      local path = GetPathWithSep(filenamein)
      if (not filenameout) then
        local name = filenamein:GetName()
        name = name:match("(.+).master$")
        if (not name) then return end
        filenameout = path..name.."."..filenamein:GetExt()
      end

      local masterfile = io.open(filenamein:GetFullPath(), "rb")
      local outfile = io.open(filenameout, "wb")

      local function out(str)
        --str = string.match(str,"
        return str
      end

      local function handleInclude(fname,defs)
        local defcnt = 0
        for i,v in pairs(defs) do
          defcnt = defcnt + 1
        end

        local incfile = io.open(path..fname, "rb")
        if (defcnt > 0) then
          local write = nil
          for line in FileLines(incfile) do
            if (write) then
              local cap = string.match(line,"#endif%s+//%s*([%w_]+)")
              if (cap == write) then
                outfile:write("//$"..write.." END$\n")
                break
              end
              outfile:write(line)
            else
              local cap = string.match(line,"#ifdef%s+([%w_]+)")
              if (cap and defs[cap]) then
                write = cap
                outfile:write("//$"..write.." BEGIN$\n")
              end
            end
          end
        else
          for line in FileLines(incfile) do
            outfile:write(line)
          end
        end

        incfile:close()
      end

      DisplayOutput("Cg Master Generating...\n")
      local master = nil
      for line in FileLines(masterfile) do
        local masterbegin = string.find(line,'//$MASTER-INCLUDE-BEGIN$',nil, true)
        local masterend = string.find(line,'//$MASTER-INCLUDE-END$', nil, true)
        if (masterbegin) then
          master = {}
          outfile:write(line)
        elseif(masterend) then
          master = nil
        end

        if (master) then
          local linein = " "..line
          local defadd = string.match(linein,'[^/]#define ([_%w]+)')
          local defrem = string.match(linein,'[^/]#undef ([_%w]+)')
          if (defadd) then master[defadd] = true end
          if (defrem) then master[defrem] = nil end
          DisplayOutput(defadd,defrem, "\n")

          local incfile = string.match(linein,'[^/]#include "(.+)"')
          if (incfile) then
            handleInclude(incfile, master, "\n")
          end
        else
          outfile:write(line)
        end
      end
      DisplayOutput("Written:",filenameout,"\n")
      outfile:close()
      masterfile:close()
    end

    local function beautifyAsm(tx)
      local newtx = ""
      local indent = 0
      local maxindent = 0
      local startindent = {
        ["IF"]=true,["REP"]=true,["ELSE"]=true,["LOOP"]=true,["BB"]=true,
      }
      local endindent = {
        ["ENDIF"]=true,["ENDREP"]=true,["ELSE"]=true,["ENDLOOP"]=true,["END"]=true,["RET"]=true,
      }

      local function checknesting(str,tab)
        local res
        local chk = str:match("%s*(BB)%d+.*:")
        chk = chk or str:match("%s*(%w+)")
        res = chk and tab[chk] and chk

        return res
      end

      local argregistry = {}
      local argbuffersfixed = false

      local registercc
      local registermem

      local function fixargbuffers()
        if (argbuffersfixed) then return end

        local argnew = {}
        for i,v in pairs(argregistry) do
          local buf,bufstart = string.match(i,"buf(%d+)%[(%d+)%]")
          if (buf and bufstart) then
            bufstart = tonumber(bufstart)/16
            argnew["buf"..buf.."["..tostring(bufstart).."]"] = v
          else
            argnew[i] = v
          end
        end
        argregistry = argnew
        argbuffersfixed = true
      end

      local function checkregistry(w)
        local regsuccess = true

        local vtype,vname,sem,resource,pnum,pref = string.match(w,"#var ([_%w]+) ([%[%]%._%w]+) : ([^%:]*) : ([^%:]*) : ([^%:]*) : (%d*)")
        local funcname,subroutine = string.match(w,"#function %d+ ([_%w]+)%((%d+)%)")
        if (pref == "1") then
          local descriptor = vtype.." "..vname

          -- check if resource is array
          local resstart,rescnt = string.match(resource,"c%[(%d+)%], (%d+)")
          resstart = tonumber(resstart)
          rescnt = tonumber(rescnt)

          -- check if resource is buffer/buffer array
          local buf,bufstart,bufcnt = string.match(resource,"buffer%[(%d+)%]%[(%d+)%],? ?(%d*)")
          buf = tonumber(buf)
          bufstart = tonumber(bufstart)
          bufcnt = tonumber(bufcnt)

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
          elseif (buf) then
            table.insert(argnames,"buf"..tostring(buf).."["..tostring(bufstart).."]")
          else
            table.insert(argnames,resource)
          end

          for i,v in ipairs(argnames) do
            argregistry[v] = descriptor
          end
        elseif (funcname and subroutine) then
          argregistry["SUBROUTINENUM("..subroutine..")"] = "function "..funcname
        elseif string.find(w,"BUFFER4") then
          fixargbuffers()
        elseif string.find(w,"TEMP") then
          --TEMP R0, R1, R2, R3, R4, R5, R6, R7, R8, R9, R10, R11;
          --TEMP RC, HC;
          --TEMP lmem[9];
          registercc = registercc or 0
          for i in string.gmatch(w,"C") do
            registercc = registercc + 1
          end
          registermem = tonumber(string.match(w,"lmem%[(%d+)%]"))
        else
          regsuccess = false
        end

        return regsuccess
      end

      local function checkargs(str)
        local comment = "#"
        local declared = {}
        for i in string.gmatch(str,"([%[%]%(%)%w]+)") do
          local descr = argregistry[i]
          if (descr and not declared[i]) then
            comment = comment.." "..i.." = "..descr
            declared[i] = true
          end
        end

        return comment ~= "#" and comment
      end

      local registerlevels = {{}}
      local function checkregisters(str,indent)
        if (indent < 0) then return end
        local cur = registerlevels[indent+1]
        for i in string.gmatch(str,"R(%d+)") do
          cur[i] = true
        end
      end

      local function clearregisters(indent)
        registerlevels[math.max(0,indent)+1] = {}
      end

      local function outputregisters(indent)
        if (indent < 0) then return "" end
        local tab = registerlevels[indent+1]
        local out = {}
        for i,v in pairs(tab) do
          table.insert(out,i)
        end
        table.sort(out)
        local cnt = #out
        if (cnt < 1) then return "" end

        local str = string.rep(" ",indent).."# "..tostring(cnt).." R used: "
        for i,v in ipairs(out) do
          str = str..tostring(v)..((i==cnt) and "" or ", ")
        end
        return str.."\n"
      end

      -- check declarations
      local lastline = ""
      for w in string.gmatch(tx, "[^\n]*\n") do
        if (not checkregistry(w)) then

          if (checknesting(w,endindent)) then
            newtx = newtx..outputregisters(indent)
            if (indent == 0) then clearregisters(indent) end
            indent = math.max(0,indent - 1)
          end

          local firstchar = string.sub(w,1,1)
          local indentstr = (firstchar ~= " " and firstchar ~= "\t" and string.rep(" ",indent) or "")
          local linestr = indentstr..w
          local argcomment = (firstchar ~= "#") and checkargs(w)

          checkregisters(w,indent)

          newtx = newtx..(argcomment and (indentstr..argcomment.."\n") or "")
          newtx = newtx..linestr

          if (checknesting(w,startindent)) then
            indent = indent + 1
            maxindent = math.max(maxindent,indent)
            clearregisters(indent)
          end
        else
          newtx = newtx..w
        end
        lastline = w
      end

      local registers = tonumber(string.match(lastline, "(%d+) R%-regs")) or 0
      registermem = registermem or 0
      registercc = registercc or 0
      local stats = "# "..tostring(registercc).." C-regs, "..tostring(registermem).." L-regs\n"
      stats = stats.."# "..tostring(registercc + registermem + registers).." maximum registers\n"
      stats = stats.."# "..maxindent.." maximum nesting level\n"
      newtx = newtx..stats

      return newtx,lastline..stats
    end

    local function beautifyAsmFile(filePath)
      local file_text = ""
      local statlines = ""
      local handle = io.open(filePath, "rb")
      if handle then
        file_text = handle:read("*a")
        file_text,statlines = beautifyAsm(file_text)
        handle:close()
      end

      if (file_text == "") then return end

      local handle = io.open(filePath, "wb")
      if handle then
        handle:write(file_text)
        handle:close()
      end
      return statlines
    end

    -- Compile Arg
    frame:Connect(ID "cg.compile.input",wx.wxEVT_COMMAND_MENU_SELECTED,
      function(event)
        data.customarg = event:IsChecked()
      end)

    frame:Connect(ID "cg.compile.gles",wx.wxEVT_COMMAND_MENU_SELECTED,
      function(event)
        data.gles = event:IsChecked()
      end)
    -- Compile
    local function evCompile(event)
      local filename,info = GetEditorFileAndCurInfo()
      local editor = GetEditor()
      local glsl = editor and editor.spec and editor.spec.apitype and editor.spec.apitype == "glsl"
      local entryname = (glsl and "main" or info.selword)

      if (not (filename and entryname and binpath)) then
        DisplayOutput("Error: Cg Compile: Insufficient parameters (nofile / not selected entry function!\n")
        return
      end

      local domain = data.domains[event:GetId()]
      local profile = data.profiles[data.profid]
      if (not profile[domain]) then return end

      -- popup for custom input
      data.custom = data.customarg and wx.wxGetTextFromUser("Compiler Args","Cg",data.custom) or data.custom
      local args = data.customarg and data.custom or ""
      args = args:len() > 0 and args or nil

      local fullname  = filename:GetFullPath()
      local outname = fullname.."."..entryname.."^"
      outname = args and outname..args:gsub("%s*[%-%/]",";-")..";^" or outname
      outname = outname..profile[domain]..profile.ext
      outname = '"'..outname..'"'

      local cmdglsl = data.gles and "-ogles -glslWerror -DGL_ES" or "-oglsl -glslWerror -po PaBO2 "
      local cmdline = ' "'..fullname..'" -profile '..profile[domain].." "
      cmdline = glsl and cmdline..cmdglsl or cmdline
      cmdline = glsl and (data.profid == (ID "cg.profile.gp5")) and cmdline.."-po NV_shader_atomic_float -po NV_bindless_texture " or cmdline
      cmdline = args and cmdline..args.." " or cmdline
      cmdline = cmdline..data.domaindefs[domain]
      cmdline = cmdline.."-o "..outname.." "
      cmdline = cmdline.."-entry "..entryname

      cmdline = binpath.."/cgc.exe"..cmdline

      local function nvperfcallback(str)
        local pixels = string.match(str,"([,%d]+) pixels/s")
        pixels = pixels and string.gsub(pixels,",","")
        pixels = tonumber(pixels)
        local function tostr(num)
          return string.format("%.2f",num)
        end

        -- delete .cgbin file
        local binname,ext = fullname:match("(.*)%.([a-zA-Z_0-9]+)$")
        binname = binname..".cgbin"
        wx.wxRemoveFile(binname)

        if (pixels ~= nil) then
          local str = string.match(str,("(.* pixels/s)"))
          local info = "1920x1080: "..tostr(pixels/(1920*1080)).." Hz\n"
          info = info.."1280x1024: "..tostr(pixels/(1280*1024)).." Hz\n"
          str = (str.."\n"..info)
          return str
        else
          return str.."\n"
        end
      end

      local function compilecallback(str)
        local postfunc
        -- check for errors, if none, launch nvperf
        -- and indentation
        if (string.find(str," 0 errors.")) then
          postfunc = function()
            -- beautify asm
            if (profile.asm) then
              local statlines = beautifyAsmFile(outname:sub(2,-2))
              DisplayOutput(statlines)
            end

            -- optionally run perf process
            local cgperfgpu = ide.config.cgperfgpu or "G80"
            local profiletypes = {
              ["G70"] = {},
              ["G80"] = {
                ["vp40"] = " -profile vp40",
                ["fp40"] = " -profile fp40"},
            }
            if (hasperf and (not glsl) and profile.nvperf and (domain == 1 or domain == 2)
              and profiletypes[cgperfgpu])
            then
              local domaintypes = {"cg_vp","cg_fp",}
              local cmdline = " -gpu "..cgperfgpu.." -type "..domaintypes[domain]
              cmdline = cmdline.." -function "..info.selword
              cmdline = cmdline..(profiletypes[cgperfgpu][profile[domain]] or "")
              cmdline = cmdline..' "'..fullname..'"'

              cmdline = binpath..perfexe..cmdline
              CommandLineRun(cmdline,nil,true,nil,nvperfcallback)
            end
          end
        end

        return str,postfunc
      end

      -- run compiler process
      CommandLineRun(cmdline,nil,true,nil,compilecallback)

    end

    frame:Connect(ID "cg.compile.vertex",wx.wxEVT_COMMAND_MENU_SELECTED,evCompile)
    frame:Connect(ID "cg.compile.fragment",wx.wxEVT_COMMAND_MENU_SELECTED,evCompile)
    frame:Connect(ID "cg.compile.geometry",wx.wxEVT_COMMAND_MENU_SELECTED,evCompile)
    frame:Connect(ID "cg.compile.tessctrl",wx.wxEVT_COMMAND_MENU_SELECTED,evCompile)
    frame:Connect(ID "cg.compile.tesseval",wx.wxEVT_COMMAND_MENU_SELECTED,evCompile)
    frame:Connect(ID "cg.compile.compute",wx.wxEVT_COMMAND_MENU_SELECTED,evCompile)

    -- indent asm
    frame:Connect(ID "cg.format.asm", wx.wxEVT_COMMAND_MENU_SELECTED,
      function(event)
        local curedit = GetEditor()
        local newtx = beautifyAsm( curedit:GetText() )

        curedit:SetText(newtx)
      end)

    -- master file
    frame:Connect(ID "cg.format.master", wx.wxEVT_COMMAND_MENU_SELECTED,
      function(even)
        local filename,info = GetEditorFileAndCurInfo()
        buildFromMaster(filename)
      end)
  end,
}
