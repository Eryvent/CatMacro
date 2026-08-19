# Cat Macro — Short Overview

Cat Macro is a Windows macro recorder built with **C#, WPF, and .NET 8**.

### Main Features

* Records keyboard and mouse actions.
* Replays recorded actions automatically.
* Playback speeds: **0.5x, 1x, 2x, 5x, 10x**.
* Repeat modes: **Once, Repeat X Times, Infinite**.
* Global hotkeys:

  * **F9:** Start/Stop Recording
  * **F8:** Start/Stop Playback
  * **F10:** Emergency Stop
* Saves macros as `.macro` JSON files.
* Includes a 5-second countdown.
* Automatically stops playback if the mouse is moved.

### Storage

Macros are saved locally in:
`Documents\CatMacro\`

The application is designed to work offline and does not require cloud services.

### Limitations

* Some games may block simulated input with anti-cheat.
* Very fast actions may not replay perfectly.
* Unicode characters may not work correctly in some applications.

**In short:** Cat Macro records your keyboard and mouse actions and lets you replay them automatically at different speeds and repetition settings.
