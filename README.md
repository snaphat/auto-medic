# auto-medic
De4dot based patching toolkit for .net binary modification.

# Rationale
- Why not just use [dnlib](https://github.com/0xd4d/dnlib) for patching directly?
  - Most assemblies are obfuscated and packed in practice.
  - Using [dnlib](https://github.com/0xd4d/dnlib) or [Mono.Cecil](https://www.mono-project.com/docs/tools+libraries/libraries/Mono.Cecil/) directly on these will, in most cases, result in a broken Assembly that will not run.
- What does this do differently from using [de4dot](https://github.com/mobile46/de4dot) or [dnlib](https://github.com/0xd4d/dnlib)?
  - [dnlib](https://github.com/0xd4d/dnlib) is a library that allows for programmatically patching an assembly.
  - [de4dot](https://github.com/mobile46/de4dot) is CLI program to deobfuscate and unpack assemblies.
  - [auto-medic](https://github.com/snaphat/auto-medic) provides scaffolding to simplify and combines both processes so that an assembly can be deobfucated and programmatically patched in a single-step.
- What are the tangible benefits to utilizing [auto-medic](https://github.com/snaphat/auto-medic)?
  - A streamlined patch process.   
  - No need to run [de4dot](https://github.com/mobile46/de4dot) independently before patching.
  - Less boiler-plate code to utilize [dnlib](https://github.com/0xd4d/dnlib) for patching.
  - A resultant self-contained single-executable patch-program for a target assembly.

# Dependencies
- `csc.exe` must be in your path. 
- `ILRepack.exe` must be in your path.
- `de4dotp.exe` must be in your path.

# Setup
- Create a patched version of [de4dot](https://github.com/mobile46/de4dot) called `de4dotp.exe` by following the instructions [here](https://github.com/snaphat/de4dot_patcher) and add the directory it is in to your path.
- Install [Build Tools for Visual Studio](https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022) and add the directory `csc.exe` is in to your path.
- Install [ILRepack](https://github.com/gluck/il-repack) and add the directory `ILRepack.exe` is in to your path. I just extract the executable from the [nupkg](http://nuget.org/api/v2/package/ILRepack) directly.

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
  - Example build and run script.
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
- Make sure to use `ILRepack.exe` to pack `de4dotp.exe` with your executable after compilation. Otherwise, it won't work. See `example/make.bat` for details.
- In a real world use-cases, one might use [dnSpy](https://github.com/dnSpy/dnSpy) or [.net Reflector](https://www.red-gate.com/products/dotnet-development/reflector/) to reverse-engineer the target .net assembly, and then utilize this toolkit to create and apply patches to an assembly's bytecode.
