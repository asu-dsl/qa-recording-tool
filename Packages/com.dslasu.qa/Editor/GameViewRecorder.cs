using System.IO;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

public class GameViewRecorder : MonoBehaviour {
    [Header("Persistent Data")]
    public GameViewRecorderData recorderData; // Reference to the ScriptableObject

    private RecorderController recorderController;
    private RecorderControllerSettings recorderControllerSettings;

    private void OnEnable() {
        // Mark the GameObject as 'DontDestroyOnLoad' so it persists between scenes
        DontDestroyOnLoad(gameObject);
        // Ensure the ScriptableObject is assigned
        if (recorderData == null) {
            Debug.LogWarning("RecorderData ScriptableObject is not assigned.");
            return;
        }

        if (Application.isPlaying && recorderData.startOnPlay) {
            SetupRecorder();
            StartRecording();
        }
    }

    private void OnDisable() {
        if (recorderController != null && recorderController.IsRecording()) {
            StopRecording();
        }
    }

    private void SetupRecorder() {
        // Ensure output directory exists
        string fullOutputDirectory = Path.GetFullPath(recorderData.outputDirectory);
        if (!Directory.Exists(fullOutputDirectory)) {
            Directory.CreateDirectory(fullOutputDirectory);
            Debug.Log($"Created output directory at: {fullOutputDirectory}");
        }

        // Initialize Recorder settings
        recorderControllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        recorderController = new RecorderController(recorderControllerSettings);

        var mediaRecorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        mediaRecorder.name = "GameView Recorder";
        mediaRecorder.Enabled = true;

        // Set to record the Game View
        mediaRecorder.ImageInputSettings = new GameViewInputSettings {
            OutputWidth = 1920,
            OutputHeight = 1080,
            FlipFinalOutput = false
        };

        // Set output file path without specifying the .mp4 extension explicitly
        string filenameWithoutExtension = $"{recorderData.filenamePrefix}_{System.DateTime.Now:yyyy-MM-dd_HH-mm}";
        recorderData.latestRecordingPath = Path.Combine(fullOutputDirectory, filenameWithoutExtension);
        mediaRecorder.OutputFile = recorderData.latestRecordingPath;

        recorderControllerSettings.AddRecorderSettings(mediaRecorder);
        recorderControllerSettings.SetRecordModeToManual();

        // Save the ScriptableObject asset so that the path is retained
        EditorUtility.SetDirty(recorderData);
        AssetDatabase.SaveAssets();
    }


    public void StartRecording() {
        if (recorderController == null) {
            SetupRecorder();
        }

        recorderController.PrepareRecording();
        recorderController.StartRecording();

        // Confirm the output path
        Debug.Log("Recording started.");
        Debug.Log($"Saving video to: {recorderData.latestRecordingPath}");
    }

    public void StopRecording() {
        if (recorderController != null && recorderController.IsRecording()) {
            recorderController.StopRecording();

            // Save the ScriptableObject asset so that the path is retained
            EditorUtility.SetDirty(recorderData);
            AssetDatabase.SaveAssets();

            Debug.Log("Recording stopped.");
            Debug.Log($"Video saved at: {recorderData.latestRecordingPath}");
        }
    }

    private void OnApplicationQuit() {
        // Stop recording if exiting Play mode
        if (recorderController != null && recorderController.IsRecording()) {
            StopRecording();
        }
    }

    public void OpenLatestRecording() {
        string latestRecordingFilePath = $"{recorderData.latestRecordingPath}.mp4"; // Append .mp4 for file searching

        Debug.Log("Attempting to open file");
        Debug.Log($"Latest recording path: {latestRecordingFilePath}");

        if (!string.IsNullOrEmpty(latestRecordingFilePath) && File.Exists(latestRecordingFilePath)) {
            // Open the folder and select the latest recording
            Process.Start("explorer.exe", $"/select,\"{latestRecordingFilePath}\"");
        }
        else {
            Debug.LogWarning("No recent recording found or file does not exist.");
        }
    }

}
