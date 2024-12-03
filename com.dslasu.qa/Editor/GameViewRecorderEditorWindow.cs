using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameViewRecorderEditorWindow : EditorWindow {
    private GameViewRecorderData recorderData;

    // Temporary fields for initial values when creating the asset
    private string tempOutputDirectory = "Assets/Recordings";
    private string tempFilenamePrefix = "game_view_recording";

    [MenuItem("Tools/ASU DSL/Game View Recorder")]
    public static void ShowWindow() {
        GetWindow<GameViewRecorderEditorWindow>("Game View Recorder");
    }

    private void OnEnable() {
        // Attempt to find the ScriptableObject
        recorderData = FindGameViewRecorderData();

        if (recorderData == null) {
            Debug.LogWarning("GameViewRecorderData asset not found. You can create it using the button in this window.");
        }
    }

    private void OnGUI() {
        GUILayout.Label("Recording Settings", EditorStyles.boldLabel);

        if (recorderData != null) {
            // Display fields for editing existing asset properties
            recorderData.outputDirectory = EditorGUILayout.TextField("Output Directory", recorderData.outputDirectory);
            recorderData.filenamePrefix = EditorGUILayout.TextField("Filename Prefix", recorderData.filenamePrefix);

            // Ensure the output directory exists when the user modifies it
            if (GUILayout.Button("Validate Output Directory")) {
                EnsureDirectoryExists(recorderData.outputDirectory);
                Debug.Log($"Validated and/or created output directory: {recorderData.outputDirectory}");
            }

            // Save changes to ScriptableObject immediately
            EditorUtility.SetDirty(recorderData);

            if (GUILayout.Button("Add Recorder Prefab to Scene")) {
                AddRecorderPrefabToScene();
            }

            GUILayout.Space(10);

            // Display the latest recording path
            GUILayout.Label("Latest Recording Path:", EditorStyles.label);
            GUILayout.TextField(recorderData.latestRecordingPath);

            if (GUILayout.Button("Open Latest Recording")) {
                if (!string.IsNullOrEmpty(recorderData.latestRecordingPath) && File.Exists(recorderData.latestRecordingPath)) {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{recorderData.latestRecordingPath}\"");
                }
                else {
                    Debug.LogWarning("No recent recording found or file does not exist.");
                }
            }
        }
        else {
            GUILayout.Label("GameViewRecorderData asset not assigned.", EditorStyles.boldLabel);

            // Fields for setting initial values when creating the asset
            tempOutputDirectory = EditorGUILayout.TextField("Output Directory", tempOutputDirectory);
            tempFilenamePrefix = EditorGUILayout.TextField("Filename Prefix", tempFilenamePrefix);

            // Button to create the ScriptableObject if it doesn't exist
            if (GUILayout.Button("Create GameViewRecorderData Asset")) {
                CreateGameViewRecorderData();
            }
        }
    }

    private void AddRecorderPrefabToScene() {
        // Check if a GameObject with this name already exists to avoid duplicates
        var existingRecorder = GameObject.Find("GameViewRecorderPrefab");
        if (existingRecorder != null) {
            Debug.LogWarning("GameViewRecorderPrefab already exists in the scene.");
            return;
        }

        // Create a new GameObject for the recorder if it doesn't already exist
        var recorderGameObject = new GameObject("GameViewRecorderPrefab");
        var recorderComponent = recorderGameObject.AddComponent<GameViewRecorder>();

        // Set the ScriptableObject reference
        recorderComponent.recorderData = recorderData;

        // Mark the scene as dirty for saving changes
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log("Game View Recorder prefab added to the scene and marked as DontDestroyOnLoad.");
    }

    private void CreateGameViewRecorderData() {
        // Create the ScriptableObject asset
        recorderData = ScriptableObject.CreateInstance<GameViewRecorderData>();
        recorderData.outputDirectory = tempOutputDirectory; // Set initial output directory
        recorderData.filenamePrefix = tempFilenamePrefix;   // Set initial filename prefix

        AssetDatabase.CreateAsset(recorderData, "Assets/GameViewRecorderData.asset");
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = recorderData;

        Debug.Log("GameViewRecorderData asset created at 'Assets/GameViewRecorderData.asset' with initial properties.");
    }

    private GameViewRecorderData FindGameViewRecorderData() {
        // Define search paths
        string[] searchFolders = { "Assets", "Assets/Settings", "Assets/Resources" };

        // Iterate through each search path and look for GameViewRecorderData instances
        foreach (var folder in searchFolders) {
            string[] guids = AssetDatabase.FindAssets("t:GameViewRecorderData", new[] { folder });
            foreach (var guid in guids) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<GameViewRecorderData>(path);
                if (data != null) {
                    Debug.Log($"GameViewRecorderData found at path: {path}");
                    return data;
                }
            }
        }

        return null;
    }


    private void EnsureDirectoryExists(string directory) {
        if (!string.IsNullOrEmpty(directory)) {
            string fullPath = Path.GetFullPath(directory);
            if (!Directory.Exists(fullPath)) {
                Directory.CreateDirectory(fullPath);
                Debug.Log($"Directory created: {fullPath}");
            }
        }
    }

}
