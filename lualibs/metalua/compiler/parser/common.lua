--------------------------------------------------------------------------------
-- Copyright (c) 2006-2013 Fabien Fleutot and others.
--
-- All rights reserved.
--
-- This program and the accompanying materials are made available
-- under the terms of the Eclipse Public License v1.0 which
-- accompanies this distribution, and is available at
-- http://www.eclipse.org/legal/epl-v10.html
--
-- This program and the accompanying materials are also made available
-- under the terms of the MIT public license which accompanies this
-- distribution, and is available at http://www.lua.org/license.html
--
-- Contributors:
--     Fabien Fleutot - API and implementation
--
--------------------------------------------------------------------------------

-- Shared common parser table. It will be filled by parser.init(),
-- and every other module will be able to call its elements at runtime.
--
-- If the table was directly created in parser.init, a circular
-- dependency would be created: parser.init depends on other modules to fill the table,
-- so other modules can't simultaneously depend on it.

return { }