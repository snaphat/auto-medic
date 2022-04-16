using System;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

class Program
{
    // Called for all methods in the target assembly.
    static AutoMedic.closure fooModifier = delegate (ModuleDef module, MethodDef method)
    {
        // Check if matching method.
        if (method.Name.Contains("get_foo") && method.HasBody)
        {
            // Replace bytecode.
            method.Body.Instructions.Insert(0, (Instruction.Create(OpCodes.Ldc_I4_1)));
            method.Body.Instructions.Insert(1, (Instruction.Create(OpCodes.Ret)));
            return 1244; // Checksum return if match.
        }
        return 0; // Checksum return if no match.
    };

    static int Main(string[] args)
    {
        // Specify a `correct` checksum is. If checksum is wrong, a patched binary will not be saved to disk.
        AutoMedic.correctChecksum = 1244;
        // Empty the list of modifiers.
        AutoMedic.modifiers.Clear();
        // Add modifier to the list of modifiers to call when patching.
        AutoMedic.modifiers.Add(fooModifier);
        // Patch the target assembly. Uses the same arguments as de4dot CLI.
        AutoMedic.DoPatch("a.exe", new string[] { "--dont-rename" });

        return 0;
    }
}
