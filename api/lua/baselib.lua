-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

-- function helpers

local function fn (description) 
	local description2,returns,args = description:match("(.+)%-%s*(%b())%s*(%b())")
	if not description2 then
		return {type="function",description=description,
			returns="(?)"} 
	end
	returns = returns:gsub("^%s+",""):gsub("%s+$","")
	local ret = returns:sub(2,-2)
	local vt = ret:match("^%[?string") and "string" 
	vt = vt or ret:match("^%[?table") and "table"
	vt = vt or ret:match("^%[?file") and "io"
	return {type="function",description=description2,
		returns=returns, args = args, valuetype = vt} 
end

local function val (description)
	return {type="value",description = description}
end
-- docs

local api = {
table = {
	description = "Table functions",
	type = "lib",
	childs = {
		concat = fn "concatenates an array of elements - (string)(table,[sep])",
		insert = fn "inserts an element into an array - ()(table,idx,element)",
		remove = fn "removes an element from an array - (element)(table,idx)",
		maxn = fn "Returns the largest positive numerical index of the given table, or zero if the table has no positive numerical indices. - (number)(table)",
		sort = fn "Sorts table elements in a given order, in-place, from table[1] to table[n], where n is the length of the table. - ()(table,[comp])"
	}
},

math = {
	type = "lib",
	description = "Math functions",
	childs = {
		abs = fn "Returns the absolute value of x. - (number)(number)",
		acos = fn "Returns the arc cosine of x (in radians). - (number)(number)",
		asin = fn "Returns the arc sine of x (in radians). - (number)(number)",
		atan = fn "Returns the arc tangent of x (in radians). - (number)(number)",
		atan2 = fn "Returns the arc tangent of x/y (in radians), but uses the signs of both parameters to find the quadrant of the result. (It also handles correctly the case of y being zero.) - (number)(number,number)",
		ceil = fn "Returns the smallest integer larger than or equal to x. - (number)(number)",
		cos = fn "Returns the cosine of x (assumed to be in radians).",
		cosh = fn "Returns the hyperbolic cosine of x.",
		deg = fn "Returns the angle x (given in radians) in degrees.",
		exp = fn "Returns the the value ex.",
		floor = fn "Returns the largest integer smaller than or equal to x.",
		fmod = fn "Returns the remainder of the division of x by y.",
		frexp = fn "Returns m and e such that x = m2e, e is an integer and the absolute value of m is in the range [0.5, 1) (or zero when x is zero).",
		huge = val "The value HUGE_VAL, a value larger than or equal to any other numerical value.",
		ldexp = fn "Returns m2e (e should be an integer).",
		log = fn "Returns the natural logarithm of x.",
		log10 = fn "Returns the base-10 logarithm of x.",
		max = fn "Returns the maximum value among its arguments.",
		min = fn "Returns the minimum value among its arguments.",
		modf = fn "Returns two numbers, the integral part of x and the fractional part of x.",
		pi = val "The value PI.",
		pow = fn "Returns xy. (You can also use the expression x^y to compute this value.)",
		rad = fn "Returns the angle x (given in degrees) in radians.",
		random = fn "This function is an interface to the simple pseudo-random generator function rand provided by ANSI C. (No guarantees can be given for its statistical properties.) When called without arguments, returns a pseudo-random real number in the range [0,1). When called with a number m, math.random returns a pseudo-random integer in the range [1, m]. When called with two numbers m and n, math.random returns a pseudo-random integer in the range [m, n].",
		randomseed = fn "Sets x as the \"seed\" for the pseudo-random generator: equal seeds produce equal sequences of numbers.",
		sin = fn"Returns the sine of x (assumed to be in radians).",
		sinh = fn"Returns the hyperbolic sine of x.",
		sqrt = fn "Returns the square root of x. (You can also use the expression x^0.5 to compute this value.)",
		tan = fn "Returns the tangent of x (assumed to be in radians).",
		tanh = fn "Returns the hyperbolic tangent of x. "
	}
},

pairs = fn "returns an iterator function for the given table - (function)(table)",
ipairs = fn "returns an iterator function for the given table - (function)(table)",
xpcall = fn "calls a function in protected mode - (boolean success, [error string / result])(called,errorfunc)",
pcall = fn "calls a function in protected mode - (boolean success, [error string / result])(called, args ...)",
print = fn "prints out the arguments - ()(...)",
assert = fn "error checking, if first arg is false, error message is thrown - (result)(compute,errormsg)",
collectgarbage = fn "garbage collector manipulation - (...)(...)",
dofile = fn "compile and execute a file - (...)(...)",
error = fn "raise an error - (...)(...)",
getfenv = fn "get the function environment for a function - (...)(...)",
getmetatable = fn "not yet - (...)(...)",
load  = fn "not yet - (...)(...)",
loadfile  = fn "not yet - (...)(...)",
loadstring  = fn "not yet - (...)(...)",
next  = fn "not yet - (...)(...)",
rawequal  = fn "not yet - (...)(...)",
rawget  = fn "not yet - (...)(...)",
rawset  = fn "not yet - (...)(...)",
select  = fn "not yet - (...)(...)",
setfenv  = fn "not yet - (...)(...)",
setmetatable  = fn "not yet - (...)(...)",
tonumber  = fn "not yet - (number)(...)",
tostring  = fn "not yet - (string)(...)",
type  = fn "not yet - (string)(...)",
unpack  = fn "not yet - (...)(...)",

module = fn "Creates a module. - (?)(name,...)",
require = fn "Loads the given module. - (?)(name)",

package = {
	type = "table",
	description = "package info",
	childs = {
		cpath = val "The path used by require to search for a C loader. ",
		loaded = val "A table used by require to control which modules are already loaded.",
		loadlib  = fn "Dynamically links the host program with the C library libname. - (?)(libname, funcname)",
		path = val "The path used by require to search for a Lua loader. ",
		preload = val "A table to store loaders for specific modules (see require). ",
		seeall = fn "Sets a metatable for module with its __index field referring to the global environment, so that this module inherits values from the global environment. To be used as an option to function module. - (?)(module)"
	}
},

string = {
	type = "lib",
	description = "string lib",
	childs = {
		byte = fn "Returns the internal numerical codes of the characters s[i], s[i+1], ···, s[j]. The default value for i is 1; the default value for j is i. - (number)(string [, i [, j]])",
		char = fn "Receives zero or more integers. Returns a string with length equal to the number of arguments, in which each character has the internal numerical code equal to its corresponding argument. - (string)(...)",
		dump = fn "Returns a string containing a binary representation of the given function, so that a later loadstring on this string returns a copy of the function. function must be a Lua function without upvalues. - (string)(func)",
		find = fn "Looks for the first match of pattern in the string s. If it finds a match, then find returns the indices of s where this occurrence starts and ends; otherwise, it returns nil. A third, optional numerical argument init specifies where to start the search; its default value is 1 and may be negative. A value of true as a fourth, optional argument plain turns off the pattern matching facilities, so the function does a plain \"find substring\" operation, with no characters in pattern being considered \"magic\". Note that if plain is given, then init must be given as well. - (number,number)(string, pattern [, init [, plain]])",
		format = fn "Returns a formatted version of its variable number of arguments following the description given in its first argument (which must be a string). The format string follows the same rules as the printf family of standard C functions. The only differences are that the options/modifiers *, l, L, n, p, and h are not supported and that there is an extra option, q. The q option formats a string in a form suitable to be safely read back by the Lua interpreter: the string is written between double quotes, and all double quotes, newlines, embedded zeros, and backslashes in the string are correctly escaped when written. - (string)(formatstring, ···)",
		gmatch = fn "Returns an iterator function that, each time it is called, returns the next captures from pattern over string s. - (func)(string, pattern)",
		gsub = fn "Returns a copy of s in which all occurrences of the pattern have been replaced by a replacement string specified by repl, which may be a string, a table, or a function. gsub also returns, as its second value, the total number of substitutions made. - (string,number)(string, pattern, repl [, n])",
		len = fn "Receives a string and returns its length. The empty string '' has length 0. Embedded zeros are counted, so 'a\\000bc\\000' has length 5. - (number)(string)",
		lower = fn "Receives a string and returns a copy of this string with all uppercase letters changed to lowercase. All other characters are left unchanged. The definition of what an uppercase letter is depends on the current locale. - (string)(string)",
		match = fn "Looks for the first match of pattern in the string s. If it finds one, then match returns the captures from the pattern; otherwise it returns nil. If pattern specifies no captures, then the whole match is returned. A third, optional numerical argument init specifies where to start the search; its default value is 1 and may be negative. - (string,...)(string, pattern [, init])",
		rep = fn "Returns a string that is the concatenation of n copies of the string s. - (string)(string s, n)",
		reverse = fn "Returns a string that is the string s reversed. - (string)(string)",
		sub = fn "Returns the substring of s that starts at i and continues until j; i and j may be negative. If j is absent, then it is assumed to be equal to -1 (which is the same as the string length). In particular, the call string.sub(s,1,j) returns a prefix of s with length j, and string.sub(s, -i) returns a suffix of s with length i. - (string)(string, i [, j])",
		upper = fn "Receives a string and returns a copy of this string with all lowercase letters changed to uppercase. All other characters are left unchanged. The definition of what a lowercase letter is depends on the current locale.  - (string)(string)",
	}
},

coroutine = {
	type = "lib",
	description = "Lua supports coroutines, also called collaborative multithreading. A coroutine in Lua represents an independent thread of execution. Unlike threads in multithread systems, however, a coroutine only suspends its execution by explicitly calling a yield function.",
	childs = {
		create = fn 'Creates a new coroutine, with body f. f must be a Lua function. Returns this new coroutine, an object with type "thread". - (thread)(function)',
		resume = fn 'Starts or continues the execution of coroutine co. The first time you resume a coroutine, it starts running its body. The values val1, ··· are passed as the arguments to the body function. If the coroutine has yielded, resume restarts it; the values val1, ··· are passed as the results from the yield. - (boolean success, ...)(coroutine,...)',
		running = fn 'Returns the running coroutine, or nil when called by the main thread. - ([thread])()',
		status = fn 'Returns the status of coroutine co, as a string: "running", if the coroutine is running (that is, it called status); "suspended", if the coroutine is suspended in a call to yield, or if it has not started running yet; "normal" if the coroutine is active but not running (that is, it has resumed another coroutine); and "dead" if the coroutine has finished its body function, or if it has stopped with an error. - (status)(coroutine)',
		wrap = fn 'Creates a new coroutine, with body f. f must be a Lua function. Returns a function that resumes the coroutine each time it is called. Any arguments passed to the function behave as the extra arguments to resume. Returns the same values returned by resume, except the first boolean. In case of error, propagates the error. - (function)(function)',
		yield = fn 'Suspends the execution of the calling coroutine. The coroutine cannot be running a C function, a metamethod, or an iterator. Any arguments to yield are passed as extra results to resume. - (...)(...)'
	}
},

io = {
	type = "lib",
	description = "The I/O library provides two different styles for file manipulation. The first one uses implicit file descriptors; that is, there are operations to set a default input file and a default output file, and all input/output operations are over these default files. The second style uses explicit file descriptors. ",
	childs = {
		close = fn'Equivalent to file:close(). Without a file, closes the default output file. - ()([file])',
		flush = fn'Equivalent to file:flush over the default output file. - ()([file])',
		input = fn'When called with a file name, it opens the named file (in text mode), and sets its handle as the default input file. When called with a file handle, it simply sets this file handle as the default input file. When called without parameters, it returns the current default input file. - ([in])([file])',
		lines = fn'Opens the given file name in read mode and returns an iterator function that, each time it is called, returns a new line from the file. - (function)([file])',
		open = fn'This function opens a file, in the mode specified in the string mode. It returns a new file handle, or, in case of errors, nil plus an error message. - (file,[errormsg])(filename,[mode])',
		output = fn'Similar to io.input, but operates over the default output file. - ([file])([file])',
		popen = fn'Starts program prog in a separated process and returns a file handle that you can use to read data from this program (if mode is "r", the default) or to write data to this program (if mode is "w"). - (file)([prog, [mode]])',
		read = fn'Reads the file file, according to the given formats, which specify what to read. For each format, the function returns a string (or a number) with the characters read, or nil if it cannot read data with the specified format. When called without formats, it uses a default format that reads the entire next line (see below).  - (string)(...)',
		tmpfile = fn'Returns a handle for a temporary file. This file is opened in update mode and it is automatically removed when the program ends. - (file)()',
		type = fn'Checks whether obj is a valid file handle. Returns the string "file" if obj is an open file handle, "closed file" if obj is a closed file handle, or nil if obj is not a file handle. - (string)(file)',
		write = fn'Writes the value of each of its arguments to the file. The arguments must be strings or numbers. To write other values, use tostring or string.format before write. - (?)(...)',
		seek = fn'Sets and gets the file position, measured from the beginning of the file, to the position given by offset plus a base specified by the string whence - (?)([whence] [, offset])',
		setvbuf = fn'Sets the buffering mode for an output file.  - (?)(mode [, size])',
		
	}
},

os = {
	type = "lib",
	description = ' Operating System Facilities',
	childs = {
		clock = fn'Returns an approximation of the amount in seconds of CPU time used by the program. - (seconds)()',
		date = fn'Returns a string or a table containing date and time, formatted according to the given string format.  - (string)([format [, time]])',
		difftime = fn'Returns the number of seconds from time t1 to time t2. In POSIX, Windows, and some other systems, this value is exactly t2-t1. - (time)(t2,t1)',
		execute = fn'This function is equivalent to the C function system. It passes command to be executed by an operating system shell. It returns a status code, which is system-dependent. If command is absent, then it returns nonzero if a shell is available and zero otherwise. - (return)([cmd])',
		exit = fn'Calls the C function exit, with an optional code, to terminate the host program. The default value for code is the success code. - ()([code])',
		getenv = fn'Returns the value of the process environment variable varname, or nil if the variable is not defined. - ([string])(varname)',
		remove = fn'Deletes the file or directory with the given name. Directories must be empty to be removed. If this function fails, it returns nil, plus a string describing the error. - (success,[error])(filename)',
		rename = fn'Renames file or directory named oldname to newname. If this function fails, it returns nil, plus a string describing the error. - (success,[error])(oldname, newname)',
		setlocale = fn'Sets the current locale of the program. locale is a string specifying a locale; category is an optional string describing which category to change: "all", "collate", "ctype", "monetary", "numeric", or "time"; the default category is "all". The function returns the name of the new locale, or nil if the request cannot be honored.  - ([string])(locale [, category])',
		time = fn'Returns the current time when called without arguments, or a time representing the date and time specified by the given table. This table must have fields year, month, and day, and may have fields hour, min, sec, and isdst (for a description of these fields, see the os.date function). - (time)([table])',
		tmpname = fn'Returns a string with a file name that can be used for a temporary file. The file must be explicitly opened before its use and explicitly removed when no longer needed.  - (string)()',
	}
},
}

local function key (str)
	api[str] = {type="keyword"}
	return key
end
-- keywords - shouldn't be left out
key "local" "not" "if" "elseif" "else" "end" "do" "while" "repeat" "function" "until" "or"
	"or" "and" "then" "true" "false" "return" "break" "in"

return api