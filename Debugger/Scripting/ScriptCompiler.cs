using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ColossalFramework.Plugins;
using ModTools.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ModTools.Scripting
{
    internal static class ScriptCompiler
    {
        private static readonly string WorkspacePath = Path.Combine(Application.temporaryCachePath, "ModTools");
        private static readonly string SourcesPath = Path.Combine(WorkspacePath, "src");
        private static readonly string DllsPath = Path.Combine(WorkspacePath, "dll");

        private static readonly string[] GameAssemblies =
        {
            "Assembly-CSharp.dll",
            "ICities.dll",
            "ColossalManaged.dll",
            "UnityEngine.dll",
        };

        static ScriptCompiler()
        {
            if (!Directory.Exists(WorkspacePath))
            {
                Directory.CreateDirectory(WorkspacePath);
            }

            ClearFolder(WorkspacePath);
            Directory.CreateDirectory(SourcesPath);
            Directory.CreateDirectory(DllsPath);
        }

        public static bool GetMod(List<ScriptEditorFile> sources, out string errorMessage, out IModEntryPoint modInstance)
        {
            try
            {
                modInstance = null;

                if (!CompileSource(sources, out var dllPath))
                {
                    errorMessage = "Failed to compile the source!";
                    return false;
                }

                {
                    var assembly = Assembly.LoadFile(dllPath);

                    if (assembly == null)
                    {
                        errorMessage = "Failed to load assembly!";
                        return false;
                    }
                    else
                    {
                        Logger.Message($"Loaded {dllPath}");
                    }

                    // in case of multiple IModEntryPoint implementations, priorotize like this:
                    var entry =
                        assembly.GetTypes()
                        .Where(_type => typeof(IModEntryPoint).IsAssignableFrom(_type))
                        .OrderBy(_type =>
                        {
                            if (_type.IsPublic)
                                return 0;
                            else if (_type.Name != ScriptEditor.ExampleScriptName)
                                return 1;
                            else
                                return 2;
                        })
                        .FirstOrDefault();

                    if (entry == null)
                    {
                        errorMessage = "Failed to find any class that implements IModEntryPoint!";
                        return false;
                    }

                    modInstance = Activator.CreateInstance(entry) as IModEntryPoint;
                    if (modInstance == null)
                    {
                        errorMessage = "Failed to create an instance of the IModEntryPoint class!";
                        return false;
                    }
                }

                errorMessage = "OK!";
                return true;
            } catch(Exception ex) {
                modInstance = null;
                errorMessage = ex.ToString();
                return false;
            }
        }

        public static bool CompileSource(List<ScriptEditorFile> sources, out string dllPath)
        {
            try
            {
                var randomName = $"tmp_{Random.Range(0, int.MaxValue)}";

                // write source files to SourcesPath\randomName\*.*
                var sourcePath = Path.Combine(SourcesPath, randomName);
                Directory.CreateDirectory(sourcePath);
                foreach (var file in sources)
                {
                    var sourceFilePath = Path.Combine(sourcePath, Path.GetFileName(file.Path));
                    File.WriteAllText(sourceFilePath, file.Source);
                }

#if DEBUG
                Logger.Message("Source files copied to " + sourcePath);
#endif

                // compile sources to DllsPath\randomName\randomName.dll
                var outputPath = Path.Combine(DllsPath, randomName);
                Directory.CreateDirectory(outputPath);
                dllPath = Path.Combine(outputPath, randomName + ".dll");

                var self = typeof(ModToolsMod).Assembly;
                var plugin = PluginManager.instance.FindPluginInfo(self);
                var modToolsAssembly = Path.Combine(plugin.modPath, self.GetName().Name + ".dll");
                var additionalAssemblies = GameAssemblies.Concat(new[] { modToolsAssembly }).ToArray();

                PluginManager.CompileSourceInFolder(sourcePath, outputPath, additionalAssemblies);
                return File.Exists(dllPath);
            } catch(Exception ex) {
                Logger.Exception(ex);
                dllPath = null;
                return false;
            }
        }

        private static void ClearFolder(string path)
        {
            var directory = new DirectoryInfo(path);

            foreach (var file in directory.GetFiles())
            {
                file.Delete();
            }

            foreach (var dir in directory.GetDirectories())
            {
                ClearFolder(dir.FullName);
                dir.Delete();
            }
        }
    }
}