----------------------------------------------------------------------
-- Metalua:  $Id: mll.lua,v 1.3 2006/11/15 09:07:50 fab13n Exp $
--
-- Summary: Source file lexer. ~~Currently only works on strings.
-- Some API refactoring is needed.
--
----------------------------------------------------------------------
--
-- Copyright (c) 2006-2007, Fabien Fleutot <metalua@gmail.com>.
--
-- This software is released under the MIT Licence, see licence.txt
-- for details.
--
----------------------------------------------------------------------

module ("mlp", package.seeall)

require "lexer"

local mlp_lexer = lexer.lexer:clone()

local keywords = {
    "and", "break", "do", "else", "elseif",
    "end", "false", "for", "function", "if",
    "in", "local", "nil", "not", "or", "repeat",
    "return", "then", "true", "until", "while",
    "...", "..", "==", ">=", "<=", "~=", 
    "+{", "-{" }
 
for w in values(keywords) do mlp_lexer:add(w) end

_M.lexer = mlp_lexer
