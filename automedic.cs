using System;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using dnlib;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using de4dot.cui;
using de4dot.code;
using de4dot.code.AssemblyClient;
using de4dot.code.deobfuscators;

/// <summary>
/// Inherit FilesDeobfuscator and modify it's functionality a bit.
/// </summary>
class AutoMedic : FilesDeobfuscator
{
    public ModuleDefMD module;
    public static int correctChecksum = -1;

    public static bool bPrintedVersion = false;

    public delegate int closure(ModuleDef module, MethodDef method);

    public static List<closure> modifiers = new List<closure>();

    string filename;
    string filenameBackup;
    string[] arguments;
    string versionLowRange;
    string versionHighRange;
    AutoMedic(FilesDeobfuscator.Options options, string filename, string filenameBackup, string[] arguments, string versionLowRange, string versionHighRange) : base(options)
    {
        this.filename = filename;
        this.filenameBackup = filenameBackup;
        this.arguments = arguments;
        this.versionLowRange = versionLowRange;
        this.versionHighRange = versionHighRange;
    }

    static bool Try(Action action) { try { action(); return true; } catch { return false; } }
    static T Try<T>(Func<T> func) { try { return func(); } catch { return default(T); } }
    static bool Always(Action action) { action(); return true; }

    /// <summary>
    /// Override the base method to add our own functionality.
    /// </summary>
    static void WriteLine(dynamic prefix, dynamic suffix)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write(prefix + ": ");
        Console.ResetColor();
        Console.WriteLine(suffix);
    }

    /// <summary>
    /// Override the base method to add our own functionality.
    /// </summary>
    void WriteLine(dynamic suffix)
    {
        WriteLine(this.filename, suffix);
    }

    /// <summary>
    /// Print Program Version
    /// </summary>
    static void version()
    {
        if(bPrintedVersion == false)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("======");

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("=====================");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("======");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("====");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("====");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(" Auto");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("-");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Medic");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(" v4.6 ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("====");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("====");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("======");

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("=====================");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("======");

            Console.WriteLine();
            Console.ResetColor();
        }

        bPrintedVersion = true;
    }

    /// <summary>
    /// Deobfuscate files ands return an instance to the module definition of the first one.
    /// Adapted from de4dot.cui.FileDeofuscator.deobfuscateAll()
    ///
    /// </summary>
    /// <returns>The module definition of the first file.</returns>
    void deobfuscate()
    {
        var stdOut = Console.Out;
        try
        {
            Console.SetOut(new StringWriter());                                              //redirect stdout to nothing.
            List<IObfuscatedFile> allFiles = new List<IObfuscatedFile>(this.LoadAllFiles()); //Load the files.
            this.DeobfuscateAllFiles(allFiles);                                              //Deobfuscate the files.
            this.Rename(allFiles);                                                           //Rename methods/classes/etc.
            this.module = allFiles[0].ModuleDefMD;                                           //Return the module definition of the first file.
        }
        finally { Console.SetOut(stdOut); }                                                  // restore stdout.
    }

    /// <summary>
    /// Checks if a file exists.
    /// </summary>
    /// <param name="filename">The file to check existence for.</param>
    /// <returns>Return true if the file is found or false otherwise.</returns>
    static bool CheckFileExists(string filename)
    {
        //check if the file exists.
        return File.Exists(filename);
    }

    /// <summary>
    /// Backs up the binary provided.
    /// </summary>
    /// <param name="from">The binary to backup. Must exist.</param>
    /// <param name="to">The name to use for the backup.</param>
    /// <returns>Nonzero on failure and zero on success.</returns>
    static int BackupBinary(string from, string to)
    {
        WriteLine(from, "creating backup binary...");

        //check the versions of the assembly to see if the one we are going to patch is newer.
        Version newVersion = AssemblyName.GetAssemblyName(from).Version;
        Version oldVersion = Try(()=>AssemblyName.GetAssemblyName(to).Version); // Implicitly null if doesn't exist.
        string ret = newVersion.CompareTo(oldVersion) switch
        {
            0 => "backup binary exists already, aborting file write.",
            _ when !Try(() => File.Delete(to)) => "failed to remove stale backup binary, aborting execution.",
            _ when !Try(() => File.Copy(from, to)) => "failed to create backup binary, aborting execution.",
            _ => null
        };

        return ret switch
        {
            null => 0,
            _ when Always(() => WriteLine(from, ret))  => -1
        };
    }

    public static void DoPatch(string filename, string[] arguments, string versionLowRange = "0.0.0.0", string versionHighRange = "2147483647.2147483647.2147483647.2147483647")
    {
        //print version.
        AutoMedic.version();

        string filenameBackup = filename + ".bak";
        //create a backup.
        if (BackupBinary(filename, filenameBackup) != 0)
            return;

        //file argument to run de4dot (the deobfuscator) with.
        List<string> argumentList = new List<string>(arguments);
        argumentList.Add("-f");
        argumentList.Add(filenameBackup);

        //create new default options for de4dot.
        FilesDeobfuscator.Options options = new FilesDeobfuscator.Options();

        //populate the options using the commandline arguments passed to de4dot (code based actual on de4dot code).
        de4dot.cui.Program.ParseCommandLine(argumentList.ToArray(), options);
        AutoMedic autoMedic = new AutoMedic(options, filename, filenameBackup, arguments, versionLowRange, versionHighRange);
        if(autoMedic.DoPatch() != 0)
            File.Delete(filenameBackup);

        Console.WriteLine("Press Enter to exit...");
        Console.ReadLine();
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="filename"></param>
    int DoPatch()
    {
        int checksum = 0;
        Version binaryVersion = Try(() => AssemblyName.GetAssemblyName(filename).Version);
        Version lowRange = Version.Parse(versionLowRange);
        Version highRange = Version.Parse(versionHighRange);

        // Check for binary.
        if (binaryVersion == null) {
            WriteLine("No binaries with matching names found...\n");
            return -1;
        }

        // Check binary version.
        if (binaryVersion < lowRange || binaryVersion > highRange) {
            WriteLine("Binary version does not match, aborting patch.\n");
            return -1;
        }

        // Deobfuscate the files.
        WriteLine("deobfuscating binary...");
        if (!Try(() => deobfuscate())) {
            WriteLine("error deobfuscating, aborting file write.");
            return -1;
        }

        //iterate through all classes with user code delegates.
        WriteLine("patching binary...");
        foreach (TypeDef type in module.GetTypes())
            foreach (MethodDef method in type.Methods)
                foreach (closure modifier in AutoMedic.modifiers)
                    checksum += modifier(module, method);

        //checksum check.
        if (checksum != AutoMedic.correctChecksum) {
            WriteLine("checksum incorrect, aborting file write.");
            return -1;
        }

        // Save file.
        var options = new ModuleWriterOptions(module);
        options.MetadataOptions.Flags |= MetadataFlags.PreserveAll & MetadataFlags.KeepOldMaxStack;
        options.Logger = DummyLogger.NoThrowInstance;
        module.Write(filename, options); //write the file (hopefully).
        return 0;
    }
}
