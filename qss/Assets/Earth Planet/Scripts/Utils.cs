#if UNITY_EDITOR
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEditor;
using System.IO;

public static class Utils
{
    [MenuItem("Tools/Set WEBGL build settings")]
    public static void SetWebGLBuildSettings()
    {
#if !UNITY_WEBGL
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        PlayerSettings.WebGL.template = "PROJECT:Default";
        PlayerSettings.WebGL.threadsSupport = true;
        PlayerSettings.WebGL.emscriptenArgs = "-s USE_SDL=2";
#endif
    }

    [MenuItem("Tools/Build WEBGL")]
    public static void BuildWebGL()
    {
        ChangeColorSpaceToLinear();
        TurnOffAutoGraphicsAPI();
        SetGraphicsAPIToWebGL2();
        TurnOnDecompressionFallback();
        SetLightmapEncodingToNormalQuality();

        var activeScene = SceneManager.GetActiveScene();
        var buildPlayerOptions = new BuildPlayerOptions();
        var buildPath = Path.Combine(Application.persistentDataPath, activeScene.name, "WebGL build");

        if (!Directory.Exists(buildPath))
            Directory.CreateDirectory(buildPath);

        buildPlayerOptions.scenes = new[] { activeScene.path };
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.WebGL;
        buildPlayerOptions.options = BuildOptions.None;

        BuildPipeline.BuildPlayer(buildPlayerOptions);


        static void ChangeColorSpaceToLinear()
        {
            string projectSettingsPath = Application.dataPath.Replace("Assets", "") + "ProjectSettings/ProjectSettings.asset";
            string[] lines = File.ReadAllLines(projectSettingsPath);

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("m_ColorSpace:"))
                {
                    lines[i] = "  m_ColorSpace: 1";
                    break;
                }
            }

            File.WriteAllLines(projectSettingsPath, lines);
        }
        static void SetLightmapEncodingToNormalQuality()
        {
            LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional;
            LightmapSettings.lightmapsMode = LightmapsMode.CombinedDirectional;
        }
        static void TurnOffAutoGraphicsAPI()
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
        }
        static void TurnOnDecompressionFallback()
        {
            PlayerSettings.WebGL.decompressionFallback = true;
        }
        static void SetGraphicsAPIToWebGL2()
        {
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new GraphicsDeviceType[] { GraphicsDeviceType.OpenGLES3 });
        }
    }
}
#endif