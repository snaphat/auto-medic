using System;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using dnlib;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using de4dot.cui;
using de4dot.code;

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
    string verLow;
    string verHigh;

    static bool Try(Action action) { try { action(); return true; } catch { return false; } }
    static T Try<T>(Func<T> func) { try { return func(); } catch { return default(T); } }
    static bool Always(Action action) { action(); return true; }
    static bool Never(Action action) { action(); return false; }

    AutoMedic(FilesDeobfuscator.Options options, string filename, string filenameBackup, string[] arguments, string verLow, string verHigh) : base(options)
    {
        this.filename       = filename;
        this.filenameBackup = filenameBackup;
        this.arguments      = arguments;
        this.verLow         = verLow;
        this.verHigh        = verHigh;
    }

    static void Write(ConsoleColor color, String txt)
    {
        Console.ForegroundColor = color;
        Console.Write(txt);
        Console.ResetColor();
    }

    static void WriteLine(dynamic prefix, dynamic suffix)
    {
        Write(ConsoleColor.Cyan, prefix + ": ");
        Console.WriteLine(suffix);
    }

    void WriteLine(dynamic suffix)
    {
        WriteLine(filename, suffix);
    }

    /// <summary>
    /// Print Program Version
    /// </summary>
    static void version()
    {
        if(bPrintedVersion == false) {
            Write(ConsoleColor.Red, "======");
            Write(ConsoleColor.White, "=====================");
            Write(ConsoleColor.Red, "======\n");

            Write(ConsoleColor.Red, "====");
            Write(ConsoleColor.White, "====");
            Write(ConsoleColor.Cyan, " Auto");
            Write(ConsoleColor.Red, "-");
            Write(ConsoleColor.Cyan, "Medic");
            Write(ConsoleColor.Yellow, " v4.7 ");
            Write(ConsoleColor.White, "====");
            Write(ConsoleColor.Red, "====\n");

            Write(ConsoleColor.Red, "======");
            Write(ConsoleColor.White, "=====================");
            Write(ConsoleColor.Red, "======\n");
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
        try {
            Console.SetOut(new StringWriter());                                         //redirect stdout to nothing.
            List<IObfuscatedFile> allFiles = new List<IObfuscatedFile>(LoadAllFiles()); //Load the files.
            DeobfuscateAllFiles(allFiles);                                              //Deobfuscate the files.
            Rename(allFiles);                                                           //Rename methods/classes/etc.
            module = allFiles[0].ModuleDefMD;                                           //Return the module definition of the first file.
        }
        finally { Console.SetOut(stdOut); }                                             // restore stdout.
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
        string ret = (newVersion == oldVersion) switch {
            true                                   => "backup binary exists already, aborting file write.",
            _ when !Try(() => File.Delete(to))     => "failed to remove stale backup binary, aborting execution.",
            _ when !Try(() => File.Copy(from, to)) => "failed to create backup binary, aborting execution.",
            _ => null
        };

        return ret switch {
            null => 0,
            _ when Always(() => WriteLine(from, ret))  => -1
        };
    }

    public static void DoPatch(string filename, string[] arguments, string verLow = "0.0.0.0", string verHigh = "2147483647.2147483647.2147483647.2147483647")
    {
        //print version.
        AutoMedic.version();

        //create a backup.
        string filenameBackup = filename + ".bak";
        if (BackupBinary(filename, filenameBackup) != 0)
            return;

        //file argument to run de4dot (the deobfuscator) with.
        List<string> argumentList = new List<string>(arguments);
        argumentList.Add("-f");
        argumentList.Add(filenameBackup);

        //populate the options using the commandline arguments passed to de4dot (code based actual on de4dot code).
        FilesDeobfuscator.Options options = new FilesDeobfuscator.Options();
        de4dot.cui.Program.ParseCommandLine(argumentList.ToArray(), options);
        if(!(new AutoMedic(options, filename, filenameBackup, arguments, verLow, verHigh)).DoPatch())
            File.Delete(filenameBackup);

        Console.WriteLine("Press Enter to exit...");
        Console.ReadLine();
    }

    bool DoPatch()
    {
        int checksum = 0;
        Version binaryVersion = Try(() => AssemblyName.GetAssemblyName(filename).Version);
        Version low = Version.Parse(verLow);
        Version high = Version.Parse(verHigh);

        string ret = null;
        if ((binaryVersion == null) switch {
            true => "No binaries with matching names found.",
            _ when binaryVersion < low || binaryVersion > high       => "Binary version does not match, aborting patch.",
            _ when Never(() => WriteLine("deobfuscating binary...")) => "",
            _ when !Try(() => deobfuscate())                         => "error deobfuscating, aborting file write.",
            _ => null
        } != null) {
            WriteLine(ret);
            return false;
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
            return false;
        }

        // Save file.
        var options = new ModuleWriterOptions(module);
        options.MetadataOptions.Flags |= MetadataFlags.PreserveAll & MetadataFlags.KeepOldMaxStack;
        options.Logger = DummyLogger.NoThrowInstance;
        module.Write(filename, options); //write the file (hopefully).
        return true;
    }
}
