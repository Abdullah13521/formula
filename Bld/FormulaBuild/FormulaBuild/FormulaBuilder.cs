﻿namespace FormulaBuild
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Net.Http;
    using System.IO;
    using System.IO.Compression;
    using System.Reflection;
    using System.Diagnostics;

    internal static class FormulaBuilder
    {
        private const string MsBuildCommand = "\"{0}\" /p:Configuration={1} /p:Platform={2}";
        private const string ConfigDebug = "Debug";
        private const string ConfigRelease = "Release";
        private const string PlatformX86 = "x86";
        private const string PlatformX64 = "x64";
        private const string PlatformAny = "AnyCPU";
        private const string CodeGeneratorDebug = "..\\..\\..\\..\\..\\Src\\Extensions\\FormulaCodeGenerator\\bin\\x86\\Debug\\FormulaCodeGenerator.vsix";
        private const string CodeGeneratorRelease = "..\\..\\..\\..\\..\\Src\\Extensions\\FormulaCodeGenerator\\bin\\x86\\Release\\FormulaCodeGenerator.vsix";

        /// <summary>
        /// Project is described by:
        /// (1) true if can only be built with 32-bit version of MsBuild (e.g. VS extensions)
        /// (2) the relative location of the project file
        /// (3) the platform on which it should be built
        /// (4) If true then must be built; otherwise warning if build fails.
        /// </summary>
        private static readonly Tuple<bool, string, string, bool>[] Projects = new Tuple<bool, string, string, bool>[]
        {
            new Tuple<bool, string, string, bool>(false, "..\\..\\..\\..\\..\\Src\\CommandLine\\CommandLine.csproj", PlatformX86, true),
            new Tuple<bool, string, string, bool>(false, "..\\..\\..\\..\\..\\Src\\CommandLine\\CommandLinex64.csproj", PlatformX64, true),
            new Tuple<bool, string, string, bool>(true, "..\\..\\..\\..\\..\\Src\\Extensions\\FormulaCodeGenerator\\FormulaCodeGenerator.csproj", PlatformX86, false),
            new Tuple<bool, string, string, bool>(false, "..\\..\\..\\..\\..\\Src\\Extensions\\FormulaCodeGeneratorTask\\FormulaCodeGeneratorTask.csproj", PlatformX86, true),    
            new Tuple<bool, string, string, bool>(false, "..\\..\\..\\..\\..\\Src\\Extensions\\FormulaCodeGeneratorTask\\FormulaCodeGeneratorTaskx64.csproj", PlatformX64, true),    
            new Tuple<bool, string, string, bool>(false, "..\\..\\..\\..\\..\\Src\\Utilities\\FormulaToTex\\FormulaToTex.csproj", PlatformAny, true)
        };

        private static readonly Tuple<string, string>[] DebugMoveMap = new Tuple<string, string>[]
        {
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x86\\Debug\\CommandLine.exe", 
                "..\\..\\..\\..\\Drops\\Formula_Debug_x86\\Formula.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x86\\Debug\\CommandLine.exe.config", 
                "..\\..\\..\\..\\Drops\\Formula_Debug_x86\\Formula.exe.config"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x86\\Debug\\CommandLine.pdb", 
                "..\\..\\..\\..\\Drops\\Formula_Debug_x86\\Formula.pdb"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x86\\Debug\\Core.dll", 
                "..\\..\\..\\..\\Drops\\Formula_Debug_x86\\Core.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x86\\Debug\\Core.pdb", 
                "..\\..\\..\\..\\Drops\\Formula_Debug_x86\\Core.pdb"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x86\\Debug\\Microsoft.Z3.dll", 
                "..\\..\\..\\..\\Drops\\Formula_Debug_x86\\Microsoft.Z3.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x86\\Debug\\Microsoft.Z3.pdb", 
                "..\\..\\..\\..\\Drops\\Formula_Debug_x86\\Microsoft.Z3.pdb"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x86\\Debug\\libz3.dll", 
                "..\\..\\..\\..\\Drops\\Formula_Debug_x86\\libz3.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Utilities\\FormulaToTex\\bin\\Debug\\FormulaToTex.exe", 
                "..\\..\\..\\..\\Drops\\Formula_Debug_x86\\FormulaToTex.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x64\\Debug\\CommandLine.exe", 
                "..\\..\\..\\..\\Drops\\Formula_Debug_x64\\Formula.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x64\\Debug\\CommandLine.exe.config", 
                "..\\..\\..\\..\\Drops\\Formula_Debug_x64\\Formula.exe.config"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x64\\Debug\\CommandLine.pdb", 
                "..\\..\\..\\..\\Drops\\Formula_Debug_x64\\Formula.pdb"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x64\\Debug\\Core.dll", 
                "..\\..\\..\\..\\Drops\\Formula_Debug_x64\\Core.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x64\\Debug\\Core.pdb", 
                "..\\..\\..\\..\\Drops\\Formula_Debug_x64\\Core.pdb"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x64\\Debug\\Microsoft.Z3.dll", 
                "..\\..\\..\\..\\Drops\\Formula_Debug_x64\\Microsoft.Z3.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x64\\Debug\\Microsoft.Z3.pdb", 
                "..\\..\\..\\..\\Drops\\Formula_Debug_x64\\Microsoft.Z3.pdb"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x64\\Debug\\libz3.dll", 
                "..\\..\\..\\..\\Drops\\Formula_Debug_x64\\libz3.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Utilities\\FormulaToTex\\bin\\Debug\\FormulaToTex.exe", 
                "..\\..\\..\\..\\Drops\\Formula_Debug_x64\\FormulaToTex.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Extensions\\FormulaCodeGeneratorTask\\bin\\x86\\FormulaCodeGeneratorTask.dll", 
                "..\\..\\..\\..\\Drops\\Formula_Debug_x86\\FormulaCodeGeneratorTask.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Extensions\\FormulaCodeGeneratorTask\\bin\\x64\\FormulaCodeGeneratorTask.dll", 
                "..\\..\\..\\..\\Drops\\Formula_Debug_x64\\FormulaCodeGeneratorTask.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Extensions\\FormulaCodeGeneratorTask\\bin\\x86\\FormulaCodeGeneratorTask.pdb", 
                "..\\..\\..\\..\\Drops\\Formula_Debug_x86\\FormulaCodeGeneratorTask.pdb"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Extensions\\FormulaCodeGeneratorTask\\bin\\x64\\FormulaCodeGeneratorTask.pdb", 
                "..\\..\\..\\..\\Drops\\Formula_Debug_x64\\FormulaCodeGeneratorTask.pdb\\FormulaCodeGeneratorTask.pdb")
        };

        private static readonly Tuple<string, string>[] ReleaseMoveMap = new Tuple<string, string>[]
        {
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x86\\Release\\CommandLine.exe", 
                "..\\..\\..\\..\\Drops\\Formula_Release_x86\\Formula.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x86\\Release\\CommandLine.exe.config", 
                "..\\..\\..\\..\\Drops\\Formula_Release_x86\\Formula.exe.config"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x86\\Release\\CommandLine.pdb", 
                "..\\..\\..\\..\\Drops\\Formula_Release_x86\\Formula.pdb"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x86\\Release\\Core.dll", 
                "..\\..\\..\\..\\Drops\\Formula_Release_x86\\Core.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x86\\Release\\Core.pdb", 
                "..\\..\\..\\..\\Drops\\Formula_Release_x86\\Core.pdb"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x86\\Release\\Microsoft.Z3.dll", 
                "..\\..\\..\\..\\Drops\\Formula_Release_x86\\Microsoft.Z3.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x86\\Release\\Microsoft.Z3.pdb", 
                "..\\..\\..\\..\\Drops\\Formula_Release_x86\\Microsoft.Z3.pdb"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x86\\Release\\libz3.dll", 
                "..\\..\\..\\..\\Drops\\Formula_Release_x86\\libz3.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Utilities\\FormulaToTex\\bin\\Release\\FormulaToTex.exe", 
                "..\\..\\..\\..\\Drops\\Formula_Release_x86\\FormulaToTex.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x64\\Release\\CommandLine.exe", 
                "..\\..\\..\\..\\Drops\\Formula_Release_x64\\Formula.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x64\\Release\\CommandLine.exe.config", 
                "..\\..\\..\\..\\Drops\\Formula_Release_x64\\Formula.exe.config"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x64\\Release\\CommandLine.pdb", 
                "..\\..\\..\\..\\Drops\\Formula_Release_x64\\Formula.pdb"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x64\\Release\\Core.dll", 
                "..\\..\\..\\..\\Drops\\Formula_Release_x64\\Core.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x64\\Release\\Core.pdb", 
                "..\\..\\..\\..\\Drops\\Formula_Release_x64\\Core.pdb"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x64\\Release\\Microsoft.Z3.dll", 
                "..\\..\\..\\..\\Drops\\Formula_Release_x64\\Microsoft.Z3.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x64\\Release\\Microsoft.Z3.pdb", 
                "..\\..\\..\\..\\Drops\\Formula_Release_x64\\Microsoft.Z3.pdb"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\CommandLine\\bin\\x64\\Release\\libz3.dll", 
                "..\\..\\..\\..\\Drops\\Formula_Release_x64\\libz3.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Utilities\\FormulaToTex\\bin\\Release\\FormulaToTex.exe", 
                "..\\..\\..\\..\\Drops\\Formula_Release_x64\\FormulaToTex.exe"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Extensions\\FormulaCodeGeneratorTask\\bin\\x86\\FormulaCodeGeneratorTask.dll", 
                "..\\..\\..\\..\\Drops\\Formula_Release_x86\\FormulaCodeGeneratorTask.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Extensions\\FormulaCodeGeneratorTask\\bin\\x64\\FormulaCodeGeneratorTask.dll", 
                "..\\..\\..\\..\\Drops\\Formula_Release_x64\\FormulaCodeGeneratorTask.dll"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Extensions\\FormulaCodeGeneratorTask\\bin\\x86\\FormulaCodeGeneratorTask.pdb", 
                "..\\..\\..\\..\\Drops\\Formula_Release_x86\\FormulaCodeGeneratorTask.pdb"),
            new Tuple<string, string>(
                "..\\..\\..\\..\\..\\Src\\Extensions\\FormulaCodeGeneratorTask\\bin\\x64\\FormulaCodeGeneratorTask.pdb", 
                "..\\..\\..\\..\\Drops\\Formula_Release_x64\\FormulaCodeGeneratorTask.pdb")
        };

        public static bool Build(bool isBldDebug)
        {
            var result = true;
            FileInfo msbuild, msbuild32 = null;
            result = SourceDownloader.GetMsbuild(out msbuild) && 
                     SourceDownloader.GetMsbuild(out msbuild32, true) &&
                     result;
            if (!result)
            {
                Program.WriteError("Could not build Formula, unable to find msbuild");
                return false;
            }

            var config = isBldDebug ? ConfigDebug : ConfigRelease;
            foreach (var proj in Projects)
            {
                Program.WriteInfo("Building {0}: Config = {1}, Platform = {2}", proj.Item2, config, proj.Item3);
                result = BuildCSProj(proj.Item1 ? msbuild32 : msbuild, proj.Item2, config, proj.Item3, proj.Item4) && result;
            }

            if (!result)
            {
                return false;
            }

            result = DoMove(isBldDebug ? DebugMoveMap : ReleaseMoveMap) && result;
            InstallVsix(isBldDebug ? CodeGeneratorDebug : CodeGeneratorRelease);
            return result;
        }

        private static bool DoMove(Tuple<string, string>[] moveMap)
        {
            bool result = true;
            try
            {
                var runningLoc = new FileInfo(Assembly.GetExecutingAssembly().Location);
                foreach (var t in moveMap)
                {
                    var inFile = new FileInfo(Path.Combine(runningLoc.Directory.FullName, t.Item1));
                    if (!inFile.Exists)
                    {
                        result = false;
                        Program.WriteError("Could not find output file {0}", inFile.FullName);
                        continue;
                    }

                    var outFile = new FileInfo(Path.Combine(runningLoc.Directory.FullName, t.Item2));
                    if (!outFile.Directory.Exists)
                    {
                        outFile.Directory.Create();
                    }

                    inFile.CopyTo(outFile.FullName, true);
                    Program.WriteInfo("Moved output {0} --> {1}", inFile.FullName, outFile.FullName);
                }

                return result;
            }
            catch (Exception e)
            {
                Program.WriteError("Unable to move output files - {0}", e.Message);
                return false;
            }
        }

        private static bool InstallVsix(string vsixName)
        {
            Program.WriteInfo("Installing vsix {0}", vsixName);
            try
            {
                var runningLoc = new FileInfo(Assembly.GetExecutingAssembly().Location);
                var vsix = new FileInfo(Path.Combine(runningLoc.Directory.FullName, vsixName));
                if (!vsix.Exists)
                {
                    Program.WriteWarning("Could not install vsix; file {0} does not exist.", vsix.FullName);
                    return false;
                }

                FileInfo installer;
                if (!SourceDownloader.GetVsixInstaller(out installer))
                {
                    Program.WriteWarning("Could not install vsix; unable to find vsix installer.");
                    return false;
                }

                if (!RunInstaller(installer, "/q /u:4f5a7e3b-98cc-4601-a247-51cdbb9eb444"))
                {
                    Program.WriteWarning("Attempt to uninstall previous vsix failed.");
                }

                if (!RunInstaller(installer, string.Format("/q \"{0}\"", vsix.FullName)))
                {
                    Program.WriteWarning("Could not install vsix extension {0}", vsix.FullName);
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Program.WriteWarning("Could not install vsix {0} - {1}", vsixName, e.Message);
                return false;
            }
        }

        private static bool RunInstaller(FileInfo vsixInstaller, string args)
        {
            try
            {
                var psi = new ProcessStartInfo();
                psi.UseShellExecute = false;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                psi.FileName = vsixInstaller.FullName;
                psi.Arguments = args;
                psi.CreateNoWindow = true;

                var process = new Process();
                process.StartInfo = psi;
                process.OutputDataReceived += OutputReceived;
                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.WaitForExit();

                return process.ExitCode == 0;
            }
            catch (Exception e)
            {
                Program.WriteError("Failed to run vsix installer - {0}", e.Message);
                return false;
            }
        }

        private static bool BuildCSProj(FileInfo msbuild, string projFileName, string config, string platform, bool isRequired)
        {
            try
            {
                FileInfo projFile;
                if (!SourceDownloader.GetBuildRelFile(projFileName, out projFile) || !projFile.Exists)
                {
                    if (isRequired)
                    {
                        Program.WriteError("Could not find project file {0}", projFileName);
                        return false;
                    }
                    else
                    {
                        Program.WriteWarning("Could not find project file {0}", projFileName);
                        return true;
                    }
                }

                var psi = new ProcessStartInfo();
                psi.UseShellExecute = false;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                psi.WorkingDirectory = projFile.Directory.FullName;
                psi.FileName = msbuild.FullName;
                psi.Arguments = string.Format(MsBuildCommand, projFile.Name, config, platform);
                psi.CreateNoWindow = true;

                var process = new Process();
                process.StartInfo = psi;
                process.OutputDataReceived += OutputReceived;
                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.WaitForExit();

                Program.WriteInfo("EXIT: {0}", process.ExitCode);
                return process.ExitCode == 0 || !isRequired;
            }
            catch (Exception e)
            {
                if (isRequired)
                {
                    Program.WriteError("Failed to build project {0} - {1}", projFileName, e.Message);
                    return false;
                }
                else
                {
                    Program.WriteWarning("Failed to build project {0} - {1}", projFileName, e.Message);
                    return true;
                }
            }
        }

        private static void OutputReceived(
            object sender,
            DataReceivedEventArgs e)
        {
            Console.WriteLine("OUT: {0}", e.Data);
        }
    }
}
