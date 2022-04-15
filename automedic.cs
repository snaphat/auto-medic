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

    /// <summary>
    /// Override the base method to add our own functionality.
    /// </summary>
    static void WriteLine(dynamic prefix, dynamic suffix)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write(prefix + ": ");
        Console.ResetColor();
        Console.WriteLine(suffix + "\n");
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
        //Load the files.
        List<IObfuscatedFile> allFiles = new List<IObfuscatedFile>(this.LoadAllFiles());

        //Deobfuscate the files.
        this.DeobfuscateAllFiles(allFiles);

        //Rename methods/classes/etc.
        this.Rename(allFiles);

        //Return the module definition of the first file.
        this.module = allFiles[0].ModuleDefMD;
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
    /// <param name="from">The binary to backup.</param>
    /// <param name="to">The name to use for the backup.</param>
    /// <returns>Nonzero on failure and zero on success.</returns>
    static int BackupBinary(string from, string to)
    {
        //check if the file we are trying to backup to already exists.
        if (CheckFileExists(to) == true)
        {
            //check the versions of the assembly to see if the one we are going to patch is newer.
            Version newVersion = AssemblyName.GetAssemblyName(from).Version;
            Version oldVersion = AssemblyName.GetAssemblyName(to).Version;
            if(newVersion.CompareTo(oldVersion) == 0) //if the versions match don't write over backup.
            {
                WriteLine(from, "backup binary exists already, aborting file write.");
                return -1;
            }
            else
            {
                //Try to remove stale backup.
                try
                {
                    File.Delete(to);
                }
                catch (Exception e)
                {
                    WriteLine(from, "backup binary exists already, aborting file write.");
                    return -1;
                }
            }
        }

        WriteLine(from, "creating backup binary...");

        //Try to backup the file.
        try
        {
            File.Copy(from, to);
        }
        catch (Exception e)
        {
            WriteLine(from, "failed to backup binary (" + e.Message + "), aborting file write.");
            return -1;
        }

        return 0;
    }

    public static void DoPatch(string filename, string[] arguments, String versionLowRange = null, string versionHighRange = null)
    {

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
        autoMedic._DoPatch();

        //clean up the backup file.

        Console.WriteLine("Press Enter to exit...");
        Console.ReadLine();
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="filename"></param>
    void _DoPatch()
    {
        //print version.
        AutoMedic.version();

        if (!CheckFileExists(filename))
        {
            Console.WriteLine("No binaries with matching names found...\n");
            return;
        }

        if (versionLowRange != null)
        {
            Version binaryVersion = AssemblyName.GetAssemblyName(filename).Version;
            if (binaryVersion.CompareTo(new Version(versionLowRange)) < 0)
            {
                Console.WriteLine("No binaries with matching version found...\n");
                return;
            }
        }

        if (versionHighRange != null)
        {
            Version binaryVersion = AssemblyName.GetAssemblyName(filename).Version;
            if (binaryVersion.CompareTo(new Version(versionHighRange)) >= 0)
            {
                Console.WriteLine("No binaries with matching version found...\n");
                return;
            }
        }



        // Print information.
        this.WriteLine("deobfuscating binary...");

        //redirect standard out to nothing so that we display nothing when running de4dot.
        var stdOut = Console.Out;
        Console.SetOut(new StringWriter());

        try
        {
            //deobfuscate using de4dot
            this.deobfuscate();
        }
        catch
        {
            //Some sort of error occurred when running de4dot.

            //redirect standard out back to stdout.
            Console.SetOut(stdOut);

            //delete backup.
            File.Delete(filenameBackup);
            this.WriteLine("error deobfuscating, aborting file write.");
            return;
        }

        try
        {
            //redirect standard out back to stdout.
            Console.SetOut(stdOut);
            this.WriteLine("patching binary...");

            int checksum = 0;

            //iterate through all classes.
            foreach (TypeDef type in this.module.GetTypes())
            {
                foreach (MethodDef method in type.Methods)
                {
                    foreach (closure modifier in AutoMedic.modifiers)
                    {
                        checksum += modifier(this.module, method);
                    }
                }
            }

            //check that the checksum is correct.
            if (checksum == AutoMedic.correctChecksum)
            {
                //write the file (hopefully).
                var options = new ModuleWriterOptions(this.module);
                options.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
                options.MetadataOptions.Flags |= MetadataFlags.KeepOldMaxStack;
                options.Logger = DummyLogger.NoThrowInstance;
                this.module.Write(filename, options);
            }
            else
            {
                //incorrect checksum. WTF.
                this.WriteLine("checksum incorrect, aborting file write.");

                //delete backup.
                this.module.Dispose();
                File.Delete(filenameBackup);
                return;
            }
        }
        catch(Exception exception)
        {
            //Some sort of error occurred during patching.

            //delete backup.
            this.module.Dispose();
            File.Delete(filenameBackup);
            this.WriteLine("error patching binary, aborting file write.");
            this.WriteLine(exception.Message);
            return;
        }
    }
}
