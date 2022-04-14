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
    public static bool bBinaryFound = false;

    public delegate int closure(ModuleDef module, MethodDef method);

    public static List<closure> modifiers = new List<closure>();

    /// <summary>
    /// Call the base constructor
    /// </summary>
    /// <param name="options">file deobfuscator options</param>
    public AutoMedic(Options options) : base(options) { }

    /// Print Program Version
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
            Console.Write(" V4.5 ");
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
    public void deobfuscate()
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
    public static bool CheckFileExists(string filename)
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
    public static int BackupBinary(string from, string to)
    {
        //check if the file we are trying to backup to already exists.
        if (CheckFileExists(to) == true)
        {
            //check the versions of the assembly to see if the one we are going to patch is newer.
            Version newVersion = AssemblyName.GetAssemblyName(from).Version;
            Version oldVersion = AssemblyName.GetAssemblyName(to).Version;
            if(newVersion.CompareTo(oldVersion) == 0) //if the versions match don't write over backup.
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(from);
                Console.ResetColor();
                Console.WriteLine(": backup binary exists already, aborting file write.");
                Console.WriteLine();
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
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(from);
                    Console.ResetColor();
                    Console.WriteLine(": failed to remove old backup binary (" + e.Message + "), aborting file write.");
                    Console.WriteLine();
                    return -1;
                }
            }
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write(from);
        Console.ResetColor();
        Console.WriteLine(": creating backup binary...");

        //Try to backup the file.
        try
        {
            File.Copy(from, to);
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(from);
            Console.ResetColor();
            Console.WriteLine(": failed to backup binary (" + e.Message + "), aborting file write.");
            Console.WriteLine();
            return -1;
        }

        return 0;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="filename"></param>
    public static void DoPatch(string filename, string[] arguments, String versionLowRange = null, string versionHighRange = null)
    {
        //print version.
        AutoMedic.version();

        if (!CheckFileExists(filename))
            return;
        else
            bBinaryFound = true;

        if (versionLowRange != null)
        {
            Version binaryVersion = AssemblyName.GetAssemblyName(filename).Version;
            ///Console.WriteLine("Binary version:");
            ///Console.WriteLine(binaryVersion);
            ///Console.WriteLine("atleast version:");
            ///Console.WriteLine(versionLowRange);
            ///Console.WriteLine("result:");
            ///Console.WriteLine(binaryVersion.CompareTo(new Version(versionLowRange)));
            if (binaryVersion.CompareTo(new Version(versionLowRange)) < 0)
                return;
        }

        if (versionHighRange != null)
        {
            Version binaryVersion = AssemblyName.GetAssemblyName(filename).Version;
            ///Console.WriteLine("Binary version:");
            ///Console.WriteLine(binaryVersion);
            ///Console.WriteLine("atmost version:");
            ///Console.WriteLine(versionHighRange);
            ///Console.WriteLine("result:");
            ///Console.WriteLine(binaryVersion.CompareTo(new Version(versionHighRange)));
            if (binaryVersion.CompareTo(new Version(versionHighRange)) >= 0)
                return;
        }

        //create a backup.
        string filenameBackup = filename + ".bak";
        if (BackupBinary(filename, filenameBackup) == 0)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(filename);
            Console.ResetColor();
            Console.WriteLine(": deobfuscating binary...");

            //redirect standard out to nothing so that we display nothing when running de4dot.
            var stdOut = Console.Out;
            Console.SetOut(new StringWriter());

            AutoMedic automedic = null;

            try
            {
                List<string> argumentList = new List<string>(arguments);

                //file argument to run de4dot (the deobfuscator) with.
                argumentList.Add("-f");
                argumentList.Add(filenameBackup);

                //create new default options for de4dot.
                FilesDeobfuscator.Options options = new FilesDeobfuscator.Options();

                //populate the options using the commandline arguments passed to de4dot (code based actual on de4dot code).
                de4dot.cui.Program.ParseCommandLine(argumentList.ToArray(), options);

                //deobfuscate using de4dot
                automedic = new AutoMedic(options);
                automedic.deobfuscate();
            }
            catch
            {
                //Some sort of error occurred when running de4dot.

                //redirect standard out back to stdout.
                Console.SetOut(stdOut);

                //delete backup.
                File.Delete(filenameBackup);

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(filename);
                Console.ResetColor();
                Console.WriteLine(": error deobfuscating, aborting file write.");
                Console.WriteLine();
                return;
            }

            try
            {
                //redirect standard out back to stdout.
                Console.SetOut(stdOut);

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(filename);
                Console.ResetColor();
                Console.WriteLine(": patching binary...");

                int checksum = 0;

                //iterate through all classes.
                foreach (TypeDef type in automedic.module.GetTypes())
                {
                    foreach (MethodDef method in type.Methods)
                    {
                        foreach (closure modifier in AutoMedic.modifiers)
                        {
                            checksum += modifier(automedic.module, method);
                        }
                    }
                }

                //check that the checksum is correct.
                if (checksum == AutoMedic.correctChecksum)
                {
                    //write the file (hopefully).
                    var options = new ModuleWriterOptions(automedic.module);
                    options.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
                    options.MetadataOptions.Flags |= MetadataFlags.KeepOldMaxStack;
                    options.Logger = DummyLogger.NoThrowInstance;

                    automedic.module.Write(filename, options);
                    Console.WriteLine();
                }
                else
                {
                    //incorrect checksum. WTF.
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(filename);
                    Console.ResetColor();
                    Console.WriteLine(": checksum incorrect, aborting file write.");
                    Console.WriteLine();

                    //delete backup.
                    automedic.module.Dispose();
                    File.Delete(filenameBackup);
                }
            }
            catch(Exception exception)
            {
                //Some sort of error occurred during patching.

                //delete backup.
                automedic.module.Dispose();
                File.Delete(filenameBackup);

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(filename);
                Console.ResetColor();
                Console.WriteLine(": error patching binary, aborting file write.");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(filename);
                Console.ResetColor();
                Console.WriteLine(": " + exception.Message);
                Console.WriteLine();
                return;
            }
        }
    }
}
