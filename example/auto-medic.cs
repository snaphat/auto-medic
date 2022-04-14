using System;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

class Program
{
    static AutoMedic.closure fooModifier = delegate (ModuleDef module, MethodDef method)
    {
        if (method.Name.Contains("get_foo") && method.HasBody)
        {
            method.Body.Instructions.Insert(0, (Instruction.Create(OpCodes.Ldc_I4_1)));
            method.Body.Instructions.Insert(1, (Instruction.Create(OpCodes.Ret)));
            return 1;
        }
        return 0;
    };

    static int Main(string[] args)
    {
        AutoMedic.correctChecksum = 1;
        AutoMedic.modifiers.Clear();
        AutoMedic.modifiers.Add(fooModifier);
        AutoMedic.DoPatch("a.exe", new string[] { "--dont-rename" }, null, null);

        if (AutoMedic.bBinaryFound == false)
        {
            Console.WriteLine("No binaries with matching names found...");
            Console.WriteLine();
        }

        Console.WriteLine("Press Enter to exit...");
        Console.ReadLine();

        return 0;
    }
}
