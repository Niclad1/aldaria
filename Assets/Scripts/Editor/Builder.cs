using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Aldaria.EditorTools
{
    /// <summary>
    /// Builds pelo menu (Aldaria → Build) ou pela linha de comando:
    ///   Unity -batchmode -quit -projectPath . -executeMethod Aldaria.EditorTools.Builder.WebGL
    /// O jogo se monta sozinho por código, então a cena só precisa existir (com uma câmera).
    /// </summary>
    public static class Builder
    {
        const string ScenePath = "Assets/Scenes/Aldaria.unity";

        [MenuItem("Aldaria/Build/WebGL (navegador)")]
        public static void WebGL() => Build(BuildTarget.WebGL, "Build/WebGL", "");

        [MenuItem("Aldaria/Build/Windows")]
        public static void Windows() => Build(BuildTarget.StandaloneWindows64, "Build/Windows", "Aldaria.exe");

        [MenuItem("Aldaria/Build/Linux")]
        public static void Linux() => Build(BuildTarget.StandaloneLinux64, "Build/Linux", "Aldaria.x86_64");

        [MenuItem("Aldaria/Criar cena principal")]
        public static void EnsureScene()
        {
            if (File.Exists(ScenePath)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.106f, 0.149f, 0.133f);
            cam.transform.position = new Vector3(0f, -4f, -10f);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("[Aldaria] Cena criada em " + ScenePath);
        }

        static void Build(BuildTarget target, string folder, string file)
        {
            EnsureScene();
            PlayerSettings.companyName = "Aldaria";
            PlayerSettings.productName = "Aldaria";
            PlayerSettings.runInBackground = true;
            if (target == BuildTarget.WebGL)
            {
                // Sem compressão: abre em qualquer servidor estático, sem configurar cabeçalhos.
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
                PlayerSettings.WebGL.dataCaching = true;
            }
            var output = string.IsNullOrEmpty(file) ? folder : Path.Combine(folder, file);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = target,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[Aldaria] Build {target}: {summary.result}, {summary.totalSize / (1024 * 1024)} MB, {summary.totalErrors} erro(s) → {output}");
            if (Application.isBatchMode && summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        /// <summary>Só importa o projeto e compila os scripts (útil para checar erros pela linha de comando).</summary>
        public static void CheckCompile()
        {
            AssetDatabase.Refresh();
            bool failed = EditorUtility.scriptCompilationFailed;
            Debug.Log(failed ? "[Aldaria] Há erros de compilação." : "[Aldaria] Scripts compilados sem erros.");
            if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
        }
    }
}
