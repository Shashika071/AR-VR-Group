using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ReefExplorer.EditorTools
{
    /// <summary>
    /// Some Unity 6 player builds omit the Managed script folder, so the exe exits before a window appears.
    /// </summary>
    public sealed class EnsurePlayerScripts : IPostprocessBuildWithReport
    {
        public int callbackOrder => 100;

        public void OnPostprocessBuild(BuildReport report)
        {
            var output = report.summary.outputPath;
            if (string.IsNullOrEmpty(output))
                return;

            var root = Path.GetDirectoryName(output);
            var data = Path.Combine(root, Path.GetFileNameWithoutExtension(output) + "_Data");
            var managed = Path.Combine(data, "Managed");
            Directory.CreateDirectory(managed);

            if (!File.Exists(Path.Combine(managed, "mscorlib.dll")))
            {
                var project = Directory.GetParent(Application.dataPath).FullName;
                var staging = Path.Combine(project, "Temp", "StagingArea", "Data", "Managed");
                if (Directory.Exists(staging))
                    CopyDlls(staging, managed);

                var jit = Path.Combine(EditorApplication.applicationContentsPath, "MonoBleedingEdge", "lib", "mono", "unityjit-win32");
                CopyDlls(jit, managed);
                CopyDlls(Path.Combine(jit, "Facades"), managed);
                CopyDlls(Path.Combine(project, "Library", "Bee", "PlayerScriptAssemblies"), managed);
            }

            var boot = Path.Combine(data, "boot.config");
            if (File.Exists(boot))
            {
                var kept = new System.Collections.Generic.List<string>();
                foreach (var line in File.ReadAllLines(boot))
                {
                    if (line.IndexOf("xr", System.StringComparison.OrdinalIgnoreCase) < 0)
                        kept.Add(line);
                }

                File.WriteAllLines(boot, kept);
            }
        }

        static void CopyDlls(string from, string to)
        {
            if (!Directory.Exists(from))
                return;
            foreach (var file in Directory.GetFiles(from, "*.dll"))
                File.Copy(file, Path.Combine(to, Path.GetFileName(file)), true);
        }
    }
}
