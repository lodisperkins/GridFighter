using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Quantum.Editor {

  internal static class QuantumCodeIntegration {
    private const string AssemblyPath          = "Library/ScriptAssemblies/PhotonQuantumCode.dll";
    private const string QuantumPackageName    = "Packages/com.exitgames.photonquantumcode";
    private const string CodeGenPath           = "codegen/quantum.codegen.host.exe";
    private const int CodegenTimeout           = 10000;
    private const string CodeProjectName       = "PhotonQuantumCode";
    private const string QuantumCopiedCodePath = "Assets/Photon/QuantumCode";
    private const string QuantumToolsPath      = "../tools";
    private const int MenuItemPriority         = 200;

#if QUANTUM_CODE_INTEGRATION_USE_FULL_OSX_PATHS
    private const string DefaultMonoPath   = "/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono";
    private const string DefaultDotnetPath = "/usr/local/share/dotnet/x64/dotnet";
#else
    private const string DefaultMonoPath   = "mono";
    private const string DefaultDotnetPath = "dotnet";
#endif
    
    private readonly static string[] AdditionalDllDirectories = new[] {
      "Assets/Photon/Quantum/Assemblies"
    };
    
    private static string AdditonalDllDirectoriesArg => string.Join(" ", AdditionalDllDirectories);

    private static string QuantumCodePath {
      get {
        var packagePath = Path.GetFullPath(QuantumPackageName);
        if (Directory.Exists(packagePath)) {
          return QuantumPackageName;
        }
        return QuantumCopiedCodePath;
      }
    }

    [MenuItem("Quantum/Code Integration/Run All CodeGen", priority = MenuItemPriority)]
    public static void RunAllCodeGen() {
      RunQtnCodeGen();
      AssetDatabase.Refresh();
    }

    [MenuItem("Quantum/Code Integration/Run Qtn CodeGen", priority = MenuItemPriority + 11)]
    public static void RunQtnCodeGen() {
      RunCodeGenTool(CodeGenPath, Path.GetFullPath(QuantumCodePath));
      AssetDatabase.ImportAsset($"{QuantumCodePath}/Core/CodeGen.cs", ImportAssetOptions.ForceUpdate);
    }

    [MenuItem("Quantum/Code Integration/Run Unity CodeGen", priority = MenuItemPriority + 12)]
    public static void RunUnityCodeGen() {
      string unityCodeGenPath;
      
#if UNITY_2021_2_OR_NEWER
      var target           = EditorUserBuildSettings.activeBuildTarget;
      var group            = BuildPipeline.GetBuildTargetGroup(target);
      var apiCompatibility = PlayerSettings.GetApiCompatibilityLevel(group);
      if (apiCompatibility == ApiCompatibilityLevel.NET_Standard) {
#if UNITY_EDITOR_OSX
        unityCodeGenPath = "codegen_unity/netcoreapp3.1/quantum.codegen.unity.host.dll";
#else
        unityCodeGenPath = "codegen_unity/netcoreapp3.1/quantum.codegen.unity.host.exe";
#endif
      } else
#endif
      {
        unityCodeGenPath = "codegen_unity/quantum.codegen.unity.host.exe";
      }
      
      RunCodeGenTool(unityCodeGenPath, AssemblyPath, "Assets", AdditonalDllDirectoriesArg);
      AssetDatabase.Refresh();
    }

    private static string GetConsistentSlashes(string path) {
      path = path.Replace('/', Path.DirectorySeparatorChar);
      path = path.Replace('\\', Path.DirectorySeparatorChar);
      return path;
    }

    private static string GetToolPath(string toolName) {
      var toolPath = Path.Combine(QuantumToolsPath, toolName);
      toolPath = GetConsistentSlashes(toolPath);
      toolPath = Path.GetFullPath(toolPath);
      return toolPath;
    }

    private static string Enquote(string str) {
      return $"\"{str.Trim('\'', '"')}\"";
    }

    private static void RunCodeGenTool(string toolName, params string[] args) {
      var output    = new StringBuilder();
      var hadStdErr = false;

      var    path            = GetToolPath(toolName);
      var    toolIsDll       = toolName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
      string envOverrideName = null;


      if (UnityEngine.SystemInfo.operatingSystemFamily != UnityEngine.OperatingSystemFamily.Windows || toolIsDll) {
        ArrayUtility.Insert(ref args, 0, path);
        if (toolIsDll) {
          envOverrideName = "QUANTUM_CODE_INTEGRATION_DOTNET_PATH";
          path            = Environment.GetEnvironmentVariable(envOverrideName) ?? DefaultDotnetPath;
        } else {
          envOverrideName = "QUANTUM_CODE_INTEGRATION_MONO_PATH";
          path            = Environment.GetEnvironmentVariable(envOverrideName) ?? DefaultMonoPath;
        }
      }

      var startInfo = new ProcessStartInfo() {
        FileName               = path,
        Arguments              = string.Join(" ", args.Select(Enquote)),
        CreateNoWindow         = true,
        UseShellExecute        = false,
        RedirectStandardOutput = true,
        RedirectStandardError  = true,
      };

      using (var proc = new Process()) {
        proc.StartInfo = startInfo;

        proc.OutputDataReceived += (sender, e) => {
          if (e.Data != null) {
            output.AppendLine(e.Data);
          }
        };

        proc.ErrorDataReceived += (sender, e) => {
          if (e.Data != null) {
            output.AppendLine(e.Data);
            hadStdErr = true;
          }
        };

        try {
          proc.Start();

          proc.BeginOutputReadLine();
          proc.BeginErrorReadLine();

          if (!proc.WaitForExit(CodegenTimeout)) {
            throw new InvalidOperationException("Time out");
          }

          if (proc.ExitCode != 0) {
            throw new InvalidOperationException($"Invalid exit code ({proc.ExitCode})");
          }
        } catch (Exception ex) {
          if (ex is Win32Exception && ((Win32Exception)ex).NativeErrorCode == 2) {
            Debug.LogError($"<b>{proc.StartInfo.FileName} {proc.StartInfo.Arguments}</b> failed. " +
                           $"It seems that it was due to {proc.StartInfo.FileName} not being found. Make sure it is accessible. " +
                           $"{(envOverrideName == null ? "": $"You can override the path by setting the environment variable {envOverrideName}. ")}" +
                           $"Process output: {output}");
          } else {
            Debug.LogError($"<b>{proc.StartInfo.FileName} {proc.StartInfo.Arguments}</b> failed. The exception will follow. Process output: {output}");
          }
          throw;
        }

        if (hadStdErr) {
          Debug.LogWarning($"{toolName} succeeded, but there were problems.\n{output}");
        } else {
          Debug.Log($"{toolName} succeeded.\n{output}");
        }
      }
    }

    [Conditional("QUANTUM_CODE_INTEGRATION_TRACE")]
    static void LogTrace(string message) {
      Debug.Log($"[<color=#add8e6>Quantum/CodeIntegration</color>]: {message}");
    }

    private class CodeDllWatcher {

      const string DelayedUnityCodeGenSentinel = "Temp/RunUnityCodeGen";

      static void CheckSentinel() {
        if (File.Exists(DelayedUnityCodeGenSentinel)) {
          var path = File.ReadAllText(DelayedUnityCodeGenSentinel);

          LogTrace($"Sentinel found with: {path}");
          File.Delete(DelayedUnityCodeGenSentinel);
          if (File.Exists(path)) {
            RunUnityCodeGen();
          } else {
            Debug.LogWarning($"Unable to run Unity codegen on {path} - file does not exist.");
          }
        }
      }

      [InitializeOnLoadMethod]
      private static void Initialize() {

        UnityEditor.Compilation.CompilationPipeline.assemblyCompilationFinished += (path, messages) => {
          if (!IsPathThePhotonQuantumCodeAssembly(path)) {
            LogTrace($"Recompiled other assembly: {path} {(string.Join(" ", messages.Select(x => x.message)))}");
            return;
          }

          LogTrace($"Recompiled Quantum Code assembly: {path}");
          if (messages.Any(x => x.type == UnityEditor.Compilation.CompilerMessageType.Error)) {
            LogTrace($"Quantum Code had errors, not following up with Unity codegen");
            return;
          }

#if UNITY_2020_3_OR_NEWER
          File.WriteAllText(DelayedUnityCodeGenSentinel, path);
          EditorApplication.delayCall += () => {
            LogTrace("Checking sentinel in delayCall");
            CheckSentinel();
          };
        };

        LogTrace("Checking sentinel after reinitialize");
        CheckSentinel();
#else
          RunUnityCodeGen();
        };
#endif

      }

      private static bool IsPathThePhotonQuantumCodeAssembly(string path) {
        return string.Equals(Path.GetFileNameWithoutExtension(path), CodeProjectName, StringComparison.OrdinalIgnoreCase);
      }
    }

    private class QtnPostprocessor : AssetPostprocessor {

      [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Undocummented AssetPostprocessor callback")]
      private static string OnGeneratedCSProject(string path, string content) {
        if (Path.GetFileNameWithoutExtension(path) != CodeProjectName) {
          return content;
        }

        return AddQtnFilesToCsproj(content);
      }

      [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "AssetPostprocessor callback")]
      private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
        if (importedAssets.Any(IsValidQtnPath) || deletedAssets.Any(IsValidQtnPath) || movedAssets.Any(IsValidQtnPath) || movedFromAssetPaths.Any(IsValidQtnPath)) {
          RunQtnCodeGen();
          DeferredAssetDatabaseRefresh();
        }
      }

      private static string AddQtnFilesToCsproj(string content) {
        // find all the qtns
        var root = Path.GetFullPath(QuantumCodePath);
        var qtns = Directory.GetFiles(root, "*.qtn", SearchOption.AllDirectories);
        if (qtns.Length == 0) {
          return content;
        }

        XDocument doc = XDocument.Load(new StringReader(content));
        var ns = doc.Root.Name.Namespace;

        var group = new XElement(ns + "ItemGroup");
        foreach (var qtn in qtns) {
          group.Add(new XElement(ns + "None", new XAttribute("Include", GetConsistentSlashes(qtn))));
        }

        doc.Root.Add(group);
        using (var writer = new StringWriter()) {
          doc.Save(writer);
          writer.Flush();
          return writer.GetStringBuilder().ToString();
        }
      }

      private static void DeferredAssetDatabaseRefresh() {
        EditorApplication.update -= DeferredAssetDatabaseRefreshHandler;
        EditorApplication.update += DeferredAssetDatabaseRefreshHandler;
      }

      private static void DeferredAssetDatabaseRefreshHandler() {
        EditorApplication.update -= DeferredAssetDatabaseRefreshHandler;
        AssetDatabase.Refresh();
      }

      private static bool IsValidQtnPath(string path) {
        if (!string.Equals(Path.GetExtension(path), ".qtn", StringComparison.OrdinalIgnoreCase)) {
          return false;
        }
        return true;
      }
    }
  }
}