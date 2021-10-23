[Demo on YouTube](https://www.youtube.com/watch?v=Ag106QI_yhw)

## Features
- SimHub leaderboard in a progress bar style
- See who is fighting
- Leader's bar is full
- Last driver's bar is at 10%
- The rest of the field's bar is filled depending on the gap to the leader
- Built and tested with Assetto Corsa Competizione (ACC) only
- Fixes some of the SimHub / ACC gap issues e.g. when a driver crosses the start/finish line, the gap would normally increase by a whole lap time and the driver jumps to last place for a few ticks

## How to install the released version
- Paste __User.PluginProgressBarLeaderboard.dll__ inside the root folder of SimHub (`Program Files (x86)/SimHub`) **while SimHub is closed**
- Double click `Bar Leaderboard.simhubdash` and import it to SimHub

## Build from source
- Put the content of this repository inside `Program Files (x86)/SimHub/PluginSdk/User.PluginProgressBarLeaderboard`
- Compiling with Visual Studio will put the resulting .dll file in the root directory of SimHub, which is what you want
