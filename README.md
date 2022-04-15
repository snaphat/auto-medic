# auto-medic
De4dot based patching toolkit for binary modification.

# Depedencies
- `csc.exe` must be in the path. 
- `ILRepack.exe` must be in the path.
- `de4dotp.exe` must be in the path.

# Setup
- Create a patched version of de4dot called `de4dotp.exe` by following the instructions [here](https://github.com/snaphat/de4dot_patcher).
- Install [Build Tools for Visual Studio](https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022) and add `csc.exe` directory to your path.
- Install [ILRepack](https://github.com/gluck/il-repack) and add the `ILRepack.exe` directory to your path. I just extract the executable from the [nupkg](http://nuget.org/api/v2/package/ILRepack) directly.
