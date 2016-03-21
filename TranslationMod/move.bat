set GAME_DIR=E:\Games\Steam\SteamApps\common\Stardew Valley
set BUILD_DIR=E:\!GitHub\TranslationMod\TranslationMod\bin\Debug
set MOD_DIR=%AppData%\StardewValley\Mods\TranslationMod

xcopy /E /I /Y "%GAME_DIR%\Content\Fonts" "%BUILD_DIR%\content\Fonts\"

rem xcopy /E /I /Y "%BUILD_DIR%\TranslationMod.dll" "%GAME_DIR%"
rem xcopy /E /I /Y "%BUILD_DIR%\manifest.json" "%GAME_DIR%"
rem del /Q "%BUILD_DIR%\TranslationMod.dll"

rem Copy all the built STORM files to the GAME directory
xcopy /E /I /Y "%BUILD_DIR%\Castle.Core.dll" "%GAME_DIR%"
xcopy /E /I /Y "%BUILD_DIR%\Castle.Core.xml" "%GAME_DIR%"
xcopy /E /I /Y "%BUILD_DIR%\Lidgren.Network.dll" "%GAME_DIR%"
xcopy /E /I /Y "%BUILD_DIR%\Microsoft.Xna.Framework.dll" "%GAME_DIR%"
xcopy /E /I /Y "%BUILD_DIR%\Microsoft.Xna.Framework.Graphics.dll" "%GAME_DIR%"
xcopy /E /I /Y "%BUILD_DIR%\Mono.Cecil.dll" "%GAME_DIR%"
xcopy /E /I /Y "%BUILD_DIR%\Newtonsoft.Json.dll" "%GAME_DIR%"
xcopy /E /I /Y "%BUILD_DIR%\Newtonsoft.Json.xml" "%GAME_DIR%"
xcopy /E /I /Y "%BUILD_DIR%\Storm-Hooked-Game.exe" "%GAME_DIR%"
xcopy /E /I /Y "%BUILD_DIR%\StormLoader.exe" "%GAME_DIR%"
xcopy /E /I /Y "%BUILD_DIR%\StormLoader.pdb" "%GAME_DIR%"
xcopy /E /I /Y "%BUILD_DIR%\xTile.dll" "%GAME_DIR%"

del /Q "%BUILD_DIR%\Castle.Core.dll"
del /Q "%BUILD_DIR%\Castle.Core.xml"
del /Q "%BUILD_DIR%\Lidgren.Network.dll"
del /Q "%BUILD_DIR%\manifest.json"
del /Q "%BUILD_DIR%\Microsoft.Xna.Framework.dll"
del /Q "%BUILD_DIR%\Microsoft.Xna.Framework.Graphics.dll"
del /Q "%BUILD_DIR%\Mono.Cecil.dll"
del /Q "%BUILD_DIR%\Newtonsoft.Json.dll"
del /Q "%BUILD_DIR%\Newtonsoft.Json.xml"
del /Q "%BUILD_DIR%\Storm-Hooked-Game.exe"
del /Q "%BUILD_DIR%\StormLoader.exe"
del /Q "%BUILD_DIR%\StormLoader.pdb"
del /Q "%BUILD_DIR%\xTile.dll"


xcopy /E /I /Y "%BUILD_DIR%\*" "%MOD_DIR%\*"
