-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------
local ide = ide

ide.iofilters["GermanUtf8Ascii"] = {

  -- this function converts some utf8 character output. It's a hack.
  -- Since luxinia is not utf8 prepared, this is still necessary.
  -- if you wish to turn this off, edit user.lua and set this filefunction to nil
  output = function (fpath, content)
    local utf8escape = ("string").char(195)
    -- only simple cases are handled (umlauts)
    local chr = ("string").char
    local charconv = {
      [chr(164)] = chr(228), -- ä
      [chr(182)] = chr(246), -- ö
      [chr(188)] = chr(252), -- ü
      [chr(132)] = chr(196), -- Ä
      [chr(150)] = chr(214), -- Ö
      [chr(156)] = chr(220), -- Ü
      [chr(159)] = chr(223), -- ß
    }
    return content : gsub (utf8escape.."(.)",charconv)
  end,

  -- this function is another hack to read an ANSI encoded
  -- file and converts the umlauts to utf8 chars
  input = function (fpath, content)
    local utf8escape = ("string").char(195)
    local chr = ("string").char
    local charconv = {
      [chr(228)] = utf8escape..chr(164), -- ä
      [chr(246)] = utf8escape..chr(182), -- ö
      [chr(252)] = utf8escape..chr(188), -- ü
      [chr(196)] = utf8escape..chr(132), -- Ä
      [chr(214)] = utf8escape..chr(150), -- Ö
      [chr(220)] = utf8escape..chr(156), -- Ü
      [chr(223)] = utf8escape..chr(159), -- ß
    }
    local lst = "["
    for k in pairs(charconv) do lst = lst .. k end
    lst = lst.."]"

    return content:gsub(lst,charconv)
  end,

}

ide.iofilters["0d0d0aFix"] = {
  -- this function converts 0d0d0a line ending to 0d0a
  input = function(fpath, content)
    return content:gsub("\013\013\010","\013\010")
  end,
}

--üäß

for i,filter in pairs(ide.iofilters) do
  if filter.input and filter.output then
    assert(filter.output("",filter.input("","äöüÄÖÜß")),
      "Ã¢â‚¬Å¾Ã¢â‚¬ÂäöüÄÖÜß","UTF8-ANSI conversion failed: "..(i))
  end
end

-- which: "input" or "output"
function GetConfigIOFilter(which)
  local filtername = ide.config.editor.iofilter
  return (filtername and ide.iofilters[filtername] and ide.iofilters[filtername][which])
end
