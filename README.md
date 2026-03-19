# RE9_SensitivityScalingFix

## Description
Fixes the sensitivity scaling for all FOVs, ADS, and scopes, etc. in Resident Evil Requiem (RE9). Also allows for configuring monitor distance horizontal (MDH) and custom sensitivity.

## Dependencies
- REFrameworkNETPluginConfig https://github.com/TonWonton/REFrameworkNETPluginConfig

## Prerequisites
### Lua
- REFramework
  - NexusMods: https://www.nexusmods.com/residentevilrequiem/mods/13
  - GitHub: https://github.com/praydog/REFramework-nightly/releases

### C#
- REFramework and the REFramework C# API (both `RE9.zip` AND `csharp-api.zip`) https://github.com/praydog/REFramework-nightly/releases
- .NET 10.0 Desktop Runtime x64 (if you don't already have it installed) https://dotnet.microsoft.com/en-us/download/dotnet/10.0

## Installation
### Lua
1. Install REFramework
2. Download the lua script and extract to game folder
  - `RE9_SensitivityScalingFix.lua` should be in `\GAME_FOLDER\reframework\autorun\RE9_SensitivityScalingFix.lua`

### C#
1. Install prerequisites
2. Download the plugin and extract to game folder
  - `RE9_SensitivityScalingFix.dll` should be in `\GAME_FOLDER\reframework\plugins\managed\RE9_SensitivityScalingFix.dll`
3. The first startup after installing the `csharp-api` might take a while. Wait until it is complete. When the game isn't frozen anymore and it says "setting up script watcher" it is done
4. Open the REFramework UI -> `REFramework.NET script generated UI` -> change settings

## Note
- Due to how the scope works I would recommend installing one of these two mods or the scoped sensitivity will still be off
  - Only one of them since both of them include the same fix and are therefore not compatible with eachother
  - Scope Resolution Fix
    - NexusMods: https://www.nexusmods.com/residentevilrequiem/mods/588
    - GitHub: https://github.com/TonWonton/RE9_ScopeResolutionFix
  - Custom Camera FOV
    - NexusMods: https://www.nexusmods.com/residentevilrequiem/mods/57
    - GitHub: https://github.com/TonWonton/RE9_CustomCameraFOV

## Recommendation
- Sensitivity
  - Set `When Aiming` sensitivity to the same as `Normal Gameplay` sensitivity
    - Actually the ADS sensitivity still feels slightly higher even though it should scale (maybe the game has different multiplier values for normal and ADS). Maybe set `When Aiming` to one or two lower than `Normal Gameplay` sensitivity
  - If you have `Use custom sensitivity` enabled: There is no "1x" multiplier. It is probably best to set the in-game sensitivity to 10/20 (default 2.025x multiplier) or 0/20 (0.1x multiplier).
- MDH
  - MDH 1 = MDH 100% on all aspect ratios
  - MDH 100% is the same as Valorant 1x scope sensitivity
  - MDH 75% is the same as CS2 1x scope sensitivity
  - If you are not on 16:9 aspect ratio: Calculate the MDH you want since 100% MDH with ultrawide aspect ratios might feel off
    - Example: You are on 21:9 aspect ratio and want to match the MDH to 100% @ 16:9
      1. Divide the aspect ratio you want to match:							16 / 9 = 1.77777777777777777778
      2. Divide the aspect ratio you are using:								21 / 9 = 2.33333333333333333333
      3. Divide the result of both divisions:								1.77777777777777777778 / 2.33333333333333333333 = 0.76190476190476190476
      4. Multiply with MDH from the aspect ratio you wanted to match:		100% * 0.76190476190476190476 = 76.190476190476190476%
      5. Divide by 100 to get the value you need to set in the mod:			76.190476190476190476 / 100 = 0.76190476190476190476
      6. Setting MDH to 0.76190476190476190476 on a 21:9 monitor gives the same sensitivity as MDH 100% on a 16:9 monitor

## Features
- Fixes the sensitivity scaling for all FOVs, ADS, and scopes, etc.
- Can configure different sensitivity options
  - Monitor distance horizontal (MDH)
  - Custom sensitivity

## Changelog
### v1.0.0
- Initial release

### v1.0.1
- Fix MDH 0% scaling