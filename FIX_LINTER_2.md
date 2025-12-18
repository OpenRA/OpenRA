Linux Errors:

Run make check
  make check
  make tests
  shell: /usr/bin/bash -e {0}
  env:
    DOTNET_ROOT: /usr/share/dotnet

Compiling in Debug mode...
  Determining projects to restore...
  Restored /home/runner/work/OpenRA/OpenRA/OpenRA.Game/OpenRA.Game.csproj (in 2.64 sec).
  Restored /home/runner/work/OpenRA/OpenRA/OpenRA.Platforms.Default/OpenRA.Platforms.Default.csproj (in 2.65 sec).
  Restored /home/runner/work/OpenRA/OpenRA/OpenRA.Launcher/OpenRA.Launcher.csproj (in 4 ms).
  Restored /home/runner/work/OpenRA/OpenRA/OpenRA.Utility/OpenRA.Utility.csproj (in 3 ms).
  Restored /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj (in 423 ms).
  Restored /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Cnc/OpenRA.Mods.Cnc.csproj (in 3.09 sec).
  Restored /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.D2k/OpenRA.Mods.D2k.csproj (in 3.09 sec).
  Restored /home/runner/work/OpenRA/OpenRA/OpenRA.Server/OpenRA.Server.csproj (in 5 ms).
  Restored /home/runner/work/OpenRA/OpenRA/OpenRA.Test/OpenRA.Test.csproj (in 2.48 sec).
  OpenRA.Game -> /home/runner/work/OpenRA/OpenRA/bin/OpenRA.Game.dll
  OpenRA.Utility -> /home/runner/work/OpenRA/OpenRA/bin/OpenRA.Utility.dll
  OpenRA.Platforms.Default -> /home/runner/work/OpenRA/OpenRA/bin/OpenRA.Platforms.Default.dll
  OpenRA.Launcher -> /home/runner/work/OpenRA/OpenRA/bin/OpenRA.dll
  OpenRA.Server -> /home/runner/work/OpenRA/OpenRA/bin/OpenRA.Server.dll
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(400,2): error SA1508: A closing brace should not be preceded by a blank line (https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1508.md) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(38,16): error SA1203: Constant fields should appear before non-constant fields (https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1203.md) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(92,7): error SA1009: Closing parenthesis should be followed by a space (https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1009.md) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(92,8): error SA1013: Closing brace should be preceded by a space (https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1013.md) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/Ingame/IngameMenuLogic.cs(341,4): error SA1515: Single-line comment should be preceded by blank line (https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1515.md) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/Ingame/LoadIngamePlayerOrObserverUILogic.cs(179,2): error SA1518: File is required to end with a single newline character (https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1518.md) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/Ingame/LoadIngamePlayerOrObserverUILogic.cs(173,25): error IDE0200: Lambda expression can be removed (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0200) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/Ingame/AIBattleObserverLogic.cs(40,65): error IDE0028: Collection initialization can be simplified (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0028) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/Ingame/AIBattleObserverLogic.cs(81,8): error CA1822: Member 'SetupSpeedButton' does not access instance data and can be marked as static (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1822) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/Ingame/IngameMenuLogic.cs(354,25): error IDE0200: Lambda expression can be removed (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0200) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(88,6): error SA1500: Braces for multi-line statements should not share line (https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1500.md) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(92,8): error SA1500: Braces for multi-line statements should not share line (https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1500.md) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(92,8): error SA1137: Elements should have the same indentation (https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1137.md) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleResultsLogic.cs(58,15): error IDE0004: Cast is redundant. (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0004) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleResultsLogic.cs(68,26): error IDE0200: Lambda expression can be removed (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0200) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleResultsLogic.cs(72,27): error IDE0200: Lambda expression can be removed (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0200) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleResultsLogic.cs(68,32): error CA1305: The behavior of 'int.ToString()' could vary based on the current user's locale settings. Replace this call in 'AIBattleResultsLogic.AIBattleResultsLogic(Widget, ModData)' with a call to 'int.ToString(IFormatProvider)'. (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1305) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleResultsLogic.cs(72,33): error CA1305: The behavior of 'int.ToString()' could vary based on the current user's locale settings. Replace this call in 'AIBattleResultsLogic.AIBattleResultsLogic(Widget, ModData)' with a call to 'int.ToString(IFormatProvider)'. (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1305) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(132,13): error IDE0047: Parentheses can be removed (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0047) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(238,24): error IDE0200: Lambda expression can be removed (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0200) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(238,30): error CA1305: The behavior of 'int.ToString()' could vary based on the current user's locale settings. Replace this call in 'AIBattleLogic.CreateAISlotWidget(int, AIBattleLogic.AISlotConfig, List<AIBattleLogic.BotTypeInfo>, List<FactionInfo>, Widget, int)' with a call to 'int.ToString(IFormatProvider)'. (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1305) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(284,56): error CA1305: The behavior of 'int.ToString()' could vary based on the current user's locale settings. Replace this call in 'AIBattleLogic.CreateAISlotWidget(int, AIBattleLogic.AISlotConfig, List<AIBattleLogic.BotTypeInfo>, List<FactionInfo>, Widget, int)' with a call to 'int.ToString(IFormatProvider)'. (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1305) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(292,37): error CA1305: The behavior of 'int.ToString()' could vary based on the current user's locale settings. Replace this call in 'AIBattleLogic.CreateAISlotWidget(int, AIBattleLogic.AISlotConfig, List<AIBattleLogic.BotTypeInfo>, List<FactionInfo>, Widget, int)' with a call to 'int.ToString(IFormatProvider)'. (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1305) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(365,17): error IDE0028: Collection initialization can be simplified (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0028) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(368,4): error IDE0028: Collection initialization can be simplified (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0028) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(371,4): error IDE0028: Collection initialization can be simplified (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0028) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(372,4): error IDE0028: Collection initialization can be simplified (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0028) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(373,4): error IDE0028: Collection initialization can be simplified (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0028) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(374,4): error IDE0028: Collection initialization can be simplified (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0028) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(377,4): error IDE0028: Collection initialization can be simplified (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0028) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(221,13): error CA1304: The behavior of 'char.ToUpper(char)' could vary based on the current user's locale settings. Replace this call in 'AIBattleLogic.BotTypeInfo.GetDisplayName()' with a call to 'char.ToUpper(char, CultureInfo)'. (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1304) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(38,16): error CA1823: Unused field 'AIBattleTitle' (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1823) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(38,16): error IDE0051: Private member 'AIBattleLogic.AIBattleTitle' is unused (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0051) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
  OpenRA.Mods.Common -> /home/runner/work/OpenRA/OpenRA/bin/OpenRA.Mods.Common.dll
  OpenRA.Mods.D2k -> /home/runner/work/OpenRA/OpenRA/bin/OpenRA.Mods.D2k.dll
  OpenRA.Mods.Cnc -> /home/runner/work/OpenRA/OpenRA/bin/OpenRA.Mods.Cnc.dll
  OpenRA.Test -> /home/runner/work/OpenRA/OpenRA/bin/OpenRA.Test.dll

Build FAILED.

Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(400,2): error SA1508: A closing brace should not be preceded by a blank line (https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1508.md) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(38,16): error SA1203: Constant fields should appear before non-constant fields (https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1203.md) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(92,7): error SA1009: Closing parenthesis should be followed by a space (https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1009.md) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(92,8): error SA1013: Closing brace should be preceded by a space (https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1013.md) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/Ingame/IngameMenuLogic.cs(341,4): error SA1515: Single-line comment should be preceded by blank line (https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1515.md) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/Ingame/LoadIngamePlayerOrObserverUILogic.cs(179,2): error SA1518: File is required to end with a single newline character (https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1518.md) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/Ingame/LoadIngamePlayerOrObserverUILogic.cs(173,25): error IDE0200: Lambda expression can be removed (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0200) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/Ingame/AIBattleObserverLogic.cs(40,65): error IDE0028: Collection initialization can be simplified (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0028) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/Ingame/AIBattleObserverLogic.cs(81,8): error CA1822: Member 'SetupSpeedButton' does not access instance data and can be marked as static (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1822) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/Ingame/IngameMenuLogic.cs(354,25): error IDE0200: Lambda expression can be removed (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0200) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(88,6): error SA1500: Braces for multi-line statements should not share line (https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1500.md) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(92,8): error SA1500: Braces for multi-line statements should not share line (https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1500.md) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(92,8): error SA1137: Elements should have the same indentation (https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1137.md) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleResultsLogic.cs(58,15): error IDE0004: Cast is redundant. (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0004) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleResultsLogic.cs(68,26): error IDE0200: Lambda expression can be removed (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0200) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleResultsLogic.cs(72,27): error IDE0200: Lambda expression can be removed (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0200) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleResultsLogic.cs(68,32): error CA1305: The behavior of 'int.ToString()' could vary based on the current user's locale settings. Replace this call in 'AIBattleResultsLogic.AIBattleResultsLogic(Widget, ModData)' with a call to 'int.ToString(IFormatProvider)'. (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1305) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleResultsLogic.cs(72,33): error CA1305: The behavior of 'int.ToString()' could vary based on the current user's locale settings. Replace this call in 'AIBattleResultsLogic.AIBattleResultsLogic(Widget, ModData)' with a call to 'int.ToString(IFormatProvider)'. (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1305) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(132,13): error IDE0047: Parentheses can be removed (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0047) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(238,24): error IDE0200: Lambda expression can be removed (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0200) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(238,30): error CA1305: The behavior of 'int.ToString()' could vary based on the current user's locale settings. Replace this call in 'AIBattleLogic.CreateAISlotWidget(int, AIBattleLogic.AISlotConfig, List<AIBattleLogic.BotTypeInfo>, List<FactionInfo>, Widget, int)' with a call to 'int.ToString(IFormatProvider)'. (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1305) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(284,56): error CA1305: The behavior of 'int.ToString()' could vary based on the current user's locale settings. Replace this call in 'AIBattleLogic.CreateAISlotWidget(int, AIBattleLogic.AISlotConfig, List<AIBattleLogic.BotTypeInfo>, List<FactionInfo>, Widget, int)' with a call to 'int.ToString(IFormatProvider)'. (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1305) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(292,37): error CA1305: The behavior of 'int.ToString()' could vary based on the current user's locale settings. Replace this call in 'AIBattleLogic.CreateAISlotWidget(int, AIBattleLogic.AISlotConfig, List<AIBattleLogic.BotTypeInfo>, List<FactionInfo>, Widget, int)' with a call to 'int.ToString(IFormatProvider)'. (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1305) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(365,17): error IDE0028: Collection initialization can be simplified (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0028) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(368,4): error IDE0028: Collection initialization can be simplified (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0028) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(371,4): error IDE0028: Collection initialization can be simplified (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0028) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(372,4): error IDE0028: Collection initialization can be simplified (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0028) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(373,4): error IDE0028: Collection initialization can be simplified (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0028) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(374,4): error IDE0028: Collection initialization can be simplified (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0028) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(377,4): error IDE0028: Collection initialization can be simplified (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0028) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(221,13): error CA1304: The behavior of 'char.ToUpper(char)' could vary based on the current user's locale settings. Replace this call in 'AIBattleLogic.BotTypeInfo.GetDisplayName()' with a call to 'char.ToUpper(char, CultureInfo)'. (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1304) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(38,16): error CA1823: Unused field 'AIBattleTitle' (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1823) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
Error: /home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/Widgets/Logic/AIBattleLogic.cs(38,16): error IDE0051: Private member 'AIBattleLogic.AIBattleTitle' is unused (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0051) [/home/runner/work/OpenRA/OpenRA/OpenRA.Mods.Common/OpenRA.Mods.Common.csproj]
    0 Warning(s)
    33 Error(s)

Time Elapsed 00:01:06.36
make: *** [Makefile:103: check] Error 1
Error: Process completed with exit code 2.

Windows Errors:
Run choco install lua --version 5.1.5.52 --no-progress
  choco install lua --version 5.1.5.52 --no-progress
  $ENV:Path = $ENV:Path + ";C:\Program Files (x86)\Lua\5.1\"
  $ENV:TREAT_WARNINGS_AS_ERRORS = "true"
  .\make.ps1 check-scripts
  .\make.ps1 test
  shell: C:\Program Files\PowerShell\7\pwsh.EXE -command ". '{0}'"
  env:
    DOTNET_ROOT: C:\Program Files\dotnet
Chocolatey v2.5.1
Installing the following packages:
lua
By installing, you accept licenses for the packages.
Downloading package from source 'https://community.chocolatey.org/api/v2/'

vcredist2005 v8.0.50727.619501 [Approved]
vcredist2005 package files install completed. Performing other installation steps.
Downloading vcredist2005 64 bit
  from 'https://download.microsoft.com/download/8/B/4/8B42259F-5D70-43F4-AC2E-4B208FD8D66A/vcredist_x64.EXE'

Download of vcredist_x64.EXE (3.03 MB) completed.
Hashes match.
Installing vcredist2005...
vcredist2005 has been installed.
Downloading vcredist2005 32 bit
  from 'https://download.microsoft.com/download/8/B/4/8B42259F-5D70-43F4-AC2E-4B208FD8D66A/vcredist_x86.EXE'

Download of vcredist_x86.EXE (2.58 MB) completed.
Hashes match.
Installing vcredist2005...
vcredist2005 has been installed.
  vcredist2005 may be able to be automatically uninstalled.
 The install of vcredist2005 was successful.
  Software installed as 'exe', install location is likely default.
Downloading package from source 'https://community.chocolatey.org/api/v2/'

lua v5.1.5.52 [Approved]
lua package files install completed. Performing other installation steps.
Downloading lua 
  from 'https://github.com/rjpcomputing/luaforwindows/releases/download/v5.1.5-52/LuaForWindows_v5.1.5-52.exe'

Download of LuaForWindows_v5.1.5-52.exe (27.8 MB) completed.
Hashes match.
Installing lua...
lua has been installed.
  lua can be automatically uninstalled.
Environment Vars (like PATH) have changed. Close/reopen your shell to
 see the changes (or in powershell/cmd.exe just type `refreshenv`).
 The install of lua was successful.
  Deployed to 'C:\Program Files (x86)\Lua\5.1\'

Chocolatey installed 2/2 packages. 
 See the log for details (C:\ProgramData\chocolatey\logs\chocolatey.log).
Testing Lua scripts...
Check completed!
Testing mods...

Testing Tiberian Sun mod MiniYAML...
Testing mod: Mod Content Manager
Testing Fluent references
Testing mod: Tiberian Sun
Testing default sequences for TEMPERATE
Testing default sequences for SNOW
Testing Fluent references
Testing map: No where to run
Testing map: Hidden Valley
Testing map: A River Runs Near It
Testing map: Cityscape
Testing map: Cliffs of Insanity
Testing map: Drawbridges
Testing map: Fields of Green
Testing map: They All Float
Testing map: Forest Fires
Testing map: Town of Karasjok
Testing map: Oasis Trouble
Testing map: River Raid
Testing map: Hot Springs
Testing map: Sunstroke
Testing map: Tiberium Garden Redux
Testing map: Tactical
Testing map: Terraces
Testing map: The Pit
Testing map: Tiers of Sorrow
Testing map: Tread Lightly
Testing map: Tournament Rift
Testing map: The Way to Uganda

Testing Dune 2000 mod MiniYAML...
Testing mod: Mod Content Manager
Testing Fluent references
Testing mod: Dune 2000
Testing default sequences for ARRAKIS
Testing Fluent references
Error: Empty key in mod translation files required by Widget `Label@HEADER_SLOT` field `Text` in d2k|chrome/aibattle.yaml:111
Error: Unused key `tooltip-ai-battle-game-speed` in d2k|fluent/chrome.ftl
Error: Unused attribute `tooltip` of key `button-ai-battle-1x` in d2k|fluent/chrome.ftl
Error: Unused attribute `tooltip` of key `button-ai-battle-2x` in d2k|fluent/chrome.ftl
Error: Unused attribute `tooltip` of key `button-ai-battle-8x` in d2k|fluent/chrome.ftl
Error: Unused attribute `tooltip` of key `button-ai-battle-16x` in d2k|fluent/chrome.ftl
Error: Unused attribute `tooltip` of key `button-ai-battle-32x` in d2k|fluent/chrome.ftl
Error: Unused attribute `tooltip` of key `button-ai-battle-64x` in d2k|fluent/chrome.ftl
Error: Unused attribute `tooltip` of key `button-ai-battle-128x` in d2k|fluent/chrome.ftl
Error: Unused key `label-ai-battle-rewinding` in d2k|fluent/chrome.ftl
Error: Unused key `label-ai-battle-seeking` in d2k|fluent/chrome.ftl
Error: Unused key `label-no-fog` in d2k|fluent/chrome.ftl
Error: Unused key `label-combined-vision` in d2k|fluent/chrome.ftl
Testing map: Arakeen
Testing map: Atreides 01a
Testing map: Atreides 01b
Testing map: Atreides 02a
Testing map: Atreides 02b
Testing map: Atreides 03a
Testing map: Atreides 03b
Testing map: Atreides 04
Testing map: Atreides 05
Testing map: Battle for Dune
Testing map: Carthag
Testing map: Cave of Birds
Testing map: Cave of Riches
Testing map: Chin Rock
Testing map: Death Depths
Testing map: Desert Twister
Testing map: Eyes of The Desert
Testing map: Gara Kulon
Testing map: Habbanya Erg
Testing map: Habbanya Ridge
Testing map: Halleck's Ridge
Testing map: Harkonnen 01a
Testing map: Harkonnen 01b
Testing map: Harkonnen 02a
Testing map: Harkonnen 02b
Testing map: Harkonnen 03a
Testing map: Harkonnen 03b
Testing map: Harkonnen 04
Testing map: Harkonnen 05
Testing map: Harkonnen 06a
Testing map: Harkonnen 06b
Testing map: Harkonnen 07
Testing map: Harkonnen 08
Testing map: Harkonnen 09a
Testing map: Harkonnen 09b
Testing map: Imperial Basin
Testing map: Jacarutu
Testing map: Kanly
Testing map: Leto's Erg
Testing map: Lynch Pass
Testing map: Mauddib's Cave
Testing map: Mount Idaho
Testing map: Mount Irulan
Testing map: Oasis Conquest
Testing map: OH Gap
Testing map: Ordos 01a
Testing map: Ordos 01b
Testing map: Ordos 02a
Testing map: Ordos 02b
Testing map: Ordos 03a
Testing map: Ordos 03b
Testing map: Ordos 04
Testing map: Ordos 05
Testing map: Ordos 06a
Testing map: Pasty Mesa
Testing map: Red Chasm
Testing map: D2k Shellmap
Testing map: Side Step Dune 03
Testing map: Sietch Tabr
Testing map: Source
Testing map: Spice Mesa
Testing map: Stone Plateaus
Testing map: Sunstroke
Testing map: Tenaya
Testing map: The Duell
Testing map: The Funeral Plain
Testing map: The Great Flat
Testing map: The Minor Erg
Testing map: The Shield Wall
Testing map: Thufir's Point
Testing map: Tsimpo
Testing map: Tuck's Sietch
Testing map: Venac ditch
Testing map: Vladimir's Folly
Testing map: Worm Graveyard
Errors: 13
Error: Process completed with exit code 1.