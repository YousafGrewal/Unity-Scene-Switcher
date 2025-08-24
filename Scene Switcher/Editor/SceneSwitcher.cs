
/*
 * ---------------------------------------------------------------------------
 *  Scene Switcher Tool for Unity
 *  Copyright (c) 2025 Vector Viral. All rights reserved.
 *
 *  Author: Yousaf Saleem (Vector Viral)
 *
 *  Rights:
 *  - This script is the intellectual property of Vector Viral.
 *  - Permission is granted to use and modify this tool for personal or 
 *    commercial projects as long as proper credit is given.
 *  - Redistribution or resale of this script as-is, or in a competing asset, 
 *    without prior written consent from Vector Viral is strictly prohibited.
 *  - Vector Viral retains the right to revoke or alter these permissions at 
 *    any time.
 *
 *  Contact: vectorviralgames@gmail.com
 * ---------------------------------------------------------------------------
 */

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
 namespace VectorViral.SceneSwitcher
{
    public class SceneSwitcher : EditorWindow
    {
        private int selectedTab = 0;
        private string[] tabNames = { "Build Settings", "All Scenes" };

        private string[] buildSceneNames;
        private string[] buildScenePaths;

        private string[] projectSceneNames;
        private string[] projectScenePaths;

        private Vector2 scrollPos;
        private static string lastBuildSettingsHash = "";

        // Persistent setting for toggle (default = true)
        private static readonly string SavePrefKey = "SceneSwitcher_ConfirmSave";
        private static bool confirmSave
        {
            get => EditorPrefs.GetBool(SavePrefKey, true);
            set => EditorPrefs.SetBool(SavePrefKey, value);
        }

        [MenuItem("Tools/Scene Switcher")]
        public static void ShowWindow()
        {
            GetWindow<SceneSwitcher>("Scene Switcher");
        }

        [InitializeOnLoadMethod]
        private static void InitOnLoad()
        {
            // Show welcome window only once (first install)
            if (!EditorPrefs.HasKey("SceneSwitcher_FirstRun"))
            {
                WelcomeWindow.ShowWindow();
                EditorPrefs.SetBool("SceneSwitcher_FirstRun", true);
            }
        }

        void OnEnable()
        {
            Texture2D myIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Icons/myIcon.png");

            RefreshBuildScenes();
            RefreshProjectScenes();
            EditorApplication.projectChanged += AutoRefresh;
            EditorApplication.update += WatchBuildSettings; // 👈 NEW
        }

        void OnDisable()
        {

            EditorApplication.projectChanged -= AutoRefresh;
            EditorApplication.update -= WatchBuildSettings; // 👈 NEW
        }
        void WatchBuildSettings()
        {
            string currentHash = GetBuildSettingsHash();
            if (currentHash != lastBuildSettingsHash)
            {
                lastBuildSettingsHash = currentHash;
                RefreshBuildScenes();
                Repaint();
            }
        }

        string GetBuildSettingsHash()
        {
            var scenes = EditorBuildSettings.scenes;
            string hash = "";
            foreach (var s in scenes)
                hash += s.path + (s.enabled ? "1" : "0");
            return hash;
        }

        void AutoRefresh()
        {
            RefreshBuildScenes();
            RefreshProjectScenes();
            Repaint();
        }

        void RefreshBuildScenes()
        {
            var scenes = EditorBuildSettings.scenes;
            buildSceneNames = new string[scenes.Length];
            buildScenePaths = new string[scenes.Length];

            for (int i = 0; i < scenes.Length; i++)
            {
                buildSceneNames[i] = Path.GetFileNameWithoutExtension(scenes[i].path);
                buildScenePaths[i] = scenes[i].path;
            }
            lastBuildSettingsHash = GetBuildSettingsHash(); // 👈 NEW
        }

        void RefreshProjectScenes()
        {
            var sceneGUIDs = AssetDatabase.FindAssets("t:Scene");
            projectSceneNames = new string[sceneGUIDs.Length];
            projectScenePaths = new string[sceneGUIDs.Length];

            for (int i = 0; i < sceneGUIDs.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(sceneGUIDs[i]);
                projectSceneNames[i] = Path.GetFileNameWithoutExtension(path);
                projectScenePaths[i] = path;
            }
        }

        void OnGUI()
        {
            DrawTabs();
            DrawSaveToggle(); // looks different

            EditorGUILayout.Space();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            if (selectedTab == 0)
                DrawSceneButtons(buildSceneNames, buildScenePaths, new Color(0.3f, 0.8f, 0.4f));
            else
                DrawSceneButtons(projectSceneNames, projectScenePaths, new Color(0.9f, 0.6f, 0.2f));

            EditorGUILayout.EndScrollView();
        }

        void DrawTabs()
        {
            GUIStyle tabStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                fixedHeight = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                normal = {
                textColor = Color.white
            }
            };

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            for (int i = 0; i < tabNames.Length; i++)
            {
                Color prevColor = GUI.backgroundColor;

                if (i == 0) // left button
                    GUI.backgroundColor = (i == selectedTab) ? new Color(0.0f, 0.75f, 1f) : new Color(0.4f, 0.4f, 0.4f);
                else if (i == 1) // right button
                    GUI.backgroundColor = (i == selectedTab) ? new Color(1f, 0.55f, 0f) : new Color(0.4f, 0.4f, 0.4f);

                if (GUILayout.Toggle(selectedTab == i, tabNames[i], tabStyle))
                {
                    selectedTab = i;
                }
                GUI.backgroundColor = prevColor;
            }
            GUILayout.EndHorizontal();
        }

        void DrawSaveToggle()
        {
            GUILayout.Space(10);

            GUILayout.Label("⚙️ Settings", EditorStyles.boldLabel);

            GUIStyle toggleStyle = new GUIStyle("button")
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = 28,
                fontSize = 14
            };

            Color prevColor = GUI.backgroundColor;

            GUI.backgroundColor = confirmSave ? new Color(0.1f, 0.7f, 0.2f) : new Color(0.9f, 0.2f, 0.2f);

            if (GUILayout.Button(confirmSave ? "💾 Confirm Save: ON" : "⚡ Fast Switch: OFF", toggleStyle))
            {
                confirmSave = !confirmSave;
            }

            GUI.backgroundColor = prevColor;

            GUILayout.Space(5);
            EditorGUILayout.HelpBox("Toggle ON: You’ll be asked to save changes before switching.\nToggle OFF: Switch instantly (unsaved changes will be lost).", MessageType.Info);
        }

        void DrawSceneButtons(string[] names, string[] paths, Color buttonColor)
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.helpBox);
            headerStyle.fontSize = 14;   // bigger font
            headerStyle.fontStyle = FontStyle.Bold; // bold
            headerStyle.alignment = TextAnchor.MiddleCenter; // centered (optional)
            headerStyle.padding = new RectOffset(10, 10, 5, 5); // extra spacing inside box

            GUILayout.Label("⚙️ Scenes", headerStyle, GUILayout.Height(30));

            for (int i = 0; i < names.Length; i++)
            {
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = buttonColor * (1f - (i % 2) * 0.2f);

                GUILayout.BeginHorizontal();
                // Main scene button with default Unity scene icon
                if (GUILayout.Button(
                        new GUIContent("   " + names[i], EditorGUIUtility.IconContent("SceneAsset Icon").image),
                        GUILayout.Height(26)))
                {
                    SwitchScene(paths[i]);
                }
                Color red = new Color(1f, 0f, 0f);   // Red
                Color green = new Color(0f, 1f, 0f);   // Green
                Color blue = new Color(0f, 0f, 1f);   // Blue
                Color yellow = new Color(1f, 1f, 0f);   // pure yellow
                GUI.backgroundColor = red * (1.5f - (i % 2) * 0.2f);
                Texture2D myIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Scene Switcher/Editor/Icons/editIcon.png");
                // Small locate button (scene icon)
                if (GUILayout.Button(new GUIContent(myIcon), GUILayout.Width(28), GUILayout.Height(26)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<SceneAsset>(paths[i]);
                    EditorGUIUtility.PingObject(obj);
                }

                GUILayout.EndHorizontal();

                GUI.backgroundColor = prev;
            }

        }

        private static void SwitchScene(string scenePath)
        {
            if (confirmSave)
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    EditorSceneManager.OpenScene(scenePath);
            }
            else
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }
    }

    // --------------- WELCOME WINDOW ------------------
    public class WelcomeWindow : EditorWindow
    {
        public static void ShowWindow()
        {
            var window = GetWindow<WelcomeWindow>("Welcome");
            window.minSize = new Vector2(400, 300);
        }

        void OnGUI()
        {
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.cyan }
            };

            GUILayout.Space(15);
            GUILayout.Label("🎉 Welcome to Scene Switcher!", titleStyle);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox("Easily switch between Build Settings scenes or ALL project scenes with just one click. Colorful tabs & smart workflow optimization included.", MessageType.Info);

            GUILayout.Space(15);
            GUILayout.Label("📖 Quick Guide:", EditorStyles.boldLabel);
            GUILayout.Label("1️ Open via Tools → Scene Switcher\n" +
                            "2️ Use the Tabs to switch between Build Settings and All Scenes\n" +
                            "3️ Use the Toggle to decide whether to save before switching\n" +
                            "4 Use Navigation button to locate the scene\n" +
                            "5 Click any Scene Button to load it instantly", EditorStyles.wordWrappedLabel);

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
            if (GUILayout.Button("Let’s Go!", GUILayout.Height(35)))
            {
                Close();
                SceneSwitcher.ShowWindow();
            }
            GUI.backgroundColor = Color.white;
        }
    }
}