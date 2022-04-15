# auto-medic
De4dot based patching toolkit for .net binary modification.

# Depedencies
- `csc.exe` must be in the path. 
- `ILRepack.exe` must be in the path.
- `de4dotp.exe` must be in the path.

# Setup
- Create a patched version of de4dot called `de4dotp.exe` by following the instructions [here](https://github.com/snaphat/de4dot_patcher).
- Install [Build Tools for Visual Studio](https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022) and add `csc.exe` directory to your path.
- Install [ILRepack](https://github.com/gluck/il-repack) and add the `ILRepack.exe` directory to your path. I just extract the executable from the [nupkg](http://nuget.org/api/v2/package/ILRepack) directly.

# Example
- `automedic.cs`
  - Base toolkit.
  - This is utilized to streamline user-code that needs to be written to patch an executable.
- `example/auto-medic.cs`
  - Example user code utilizing the base toolkit.
  - Compiled into `Auto-Medic.exe`.
- `example/sample.cs`
  - An example target application to patch.
  - Compiled into `a.exe`.
  - Returns `A equal 0!` pre-patch.
  - Should return `A equal 1!` post-patch.
- `example/make.bat`
  - Build and run script for example.
  - Compiles `a.exe` and `Auto-Medic.exe`.
  - Runs `a.exe` pre-patch.
  - Patches `a.exe` using `Auto-Doc.exe`.
  - Runs `a.exe` post-patch.

# Usage
- Include `automedic.cs` into a project.
- Adapt `example/auto-medic.cs` for real-world usage.
  - It should be pretty simple in practice.
  - Each modifier is called for all methods in the assembly.
  - Multiple code patches can be applied via additional calls to `AutoMedic.modifiers.Add()` with different modifiers.
  - The checksum is computed by adding all modifier return values.
  - Any .net assembly can be targeted.
- In a real world use case, one might use [dnSpy](https://github.com/dnSpy/dnSpy) or [.net Reflector](https://www.red-gate.com/products/dotnet-development/reflector/) to reverse-engineer the target .net assembly, and then utilize this toolkit to create and apply patches to the bytecode.