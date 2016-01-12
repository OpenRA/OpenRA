local editor = NewFile()
local line = "246"
local comment = "%-%-"

editor:AddText(([[
1
  %s
3]]):format(line))

local findReplace = ide.findReplace
findReplace:SetFind(line)
findReplace:Find()

ide.frame:ProcessEvent(wx.wxCommandEvent(
  wx.wxEVT_COMMAND_MENU_SELECTED, ID_COMMENT))

local text = editor:GetText()
ok(text:find(comment..line) and not text:find(comment.."3"),
  "One-line selection is commented.")

findReplace:SetFind("--"..line)
findReplace:Find()
editor:SetCurrentPos(editor:GetLength())

ide.frame:ProcessEvent(wx.wxCommandEvent(
  wx.wxEVT_COMMAND_MENU_SELECTED, ID_COMMENT))

text = editor:GetText()
ok(text:find(comment.."  "..comment..line) and text:find(comment.."3"),
  "Commented and uncommented lines are commented.")

findReplace:Find()
editor:SetCurrentPos(editor:GetLength())

ide.frame:ProcessEvent(wx.wxCommandEvent(
  wx.wxEVT_COMMAND_MENU_SELECTED, ID_COMMENT))

text = editor:GetText()
ok(not text:find(comment.."  "..comment..line) and not text:find(comment.."3"),
  "Multi-line commented text is uncommented.")

editor:SetAnchor(0)
editor:SetCurrentPos(editor:PositionFromLine(2))

ide.frame:ProcessEvent(wx.wxCommandEvent(
  wx.wxEVT_COMMAND_MENU_SELECTED, ID_COMMENT))

text = editor:GetText()
ok(text:find(comment.."  "..comment..line) and text:find(comment.."1") and not text:find(comment.."3"),
  "Selection that ends at the beginning of the line doesn't comment line.")

editor:SetText("1\n--2\n3\n")
is(#editor:MarkerGetAll(), 0, "No marker is set in a new editor.")
editor:BookmarkToggle(0)
is(#editor:MarkerGetAll(), 1, "Bookmark is set after toggling.")
is(#editor:MarkerGetAll(2^StylesGetMarker("bookmark")), 1, "Bookmark is the only marker set after toggling.")
editor:BookmarkToggle(0)
is(#editor:MarkerGetAll(), 0, "Bookmark is removed after toggling on a line with a bookmark.")

editor:BreakpointToggle(0)
is(#editor:MarkerGetAll(), 1, "Breakpoint is set after toggling.")
is(#editor:MarkerGetAll(2^StylesGetMarker("breakpoint")), 1, "Breakpoint is the only marker set after toggling.")
editor:BreakpointToggle(0)
is(#editor:MarkerGetAll(), 0, "Breakpoint is removed after toggling on a line with a breakpoint.")
editor:BreakpointToggle(1)
is(#editor:MarkerGetAll(), 0, "Breakpoint is not set on a comment line.")

-- cleanup
ide:GetDocument(editor):SetModified(false)
ClosePage()
