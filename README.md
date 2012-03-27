# ZeroBrane Studio

A simple and extensible Lua IDE and debugger. It supports multiple file
formats, "api" for autocompletion and tooltips, and custom command-line
tools. Its main focus is extensibility for target applications using Lua.

## Features

* Written in Lua, so easily customizable
* Automatically loads several 'plugin' like classes
  - applications: overall control of applications settings
  - specs (spec/): file syntax, lexer, keywords
  - apis (api/): for code-completion and tool-tips
  - interpreters (interpreters/): how a project is run
  - config (cfg/): contains style and basic editor settings
  - tools (tools/): additional tools, e.g. cg compiler, dx fxc compiler
* Auto-completion for functions, keywords...
* Function tips
* Function list in file (quick jump to)
* Function call highlighting
* Bracket matching/highlighting
* Project file browser
* Experimental type/class guessing for auto-completion
* Support for different editor styles
* Console to directly test code snippets with local and remote execution
* Integrated debugger (with support for local and remote debugging)

## Frontends

There is currently two front-ends using the same editor engine. The original
is `estrela`, which has a focus on 3d graphics related usage of Lua, especially
in combination with the luxinia engine or luxinia2 framework.
The second is `zbstudio` which has a focus on remote use of Lua in robotics.

Both are part of the standard distribution.

## Installation

```bash
$ git clone git://github.com/pkulchenko/ZeroBraneStudio.git zbstudio
  or
$ git clone git://estrelaeditor.git.sourceforge.net/gitroot/estrelaeditor/estrelaeditor estrelaeditor
```

## Usage

```
Open File(s):
  <exe> <filename> [<filename>...]
  any non-option will be treated as filename

Overriding Config:
  <exe> [...] -cfg "<luacode overriding config>" [...]
  e.g.: zbstudio.exe -cfg "singleinstance=false;" somefile.lua
```

## Author

#### Estrela Editor

  **Luxinia Dev:** Christoph Kubisch (crazybutcher@luxinia.de)

#### ZeroBrane Studio and MobDebug

  **ZeroBrane LLC:** Paul Kulchenko (paul@kulchenko.com)

## License

See LICENSE file.
