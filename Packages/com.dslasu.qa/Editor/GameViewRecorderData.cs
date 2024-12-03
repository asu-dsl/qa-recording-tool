using UnityEngine;

[CreateAssetMenu(fileName = "GameViewRecorderData", menuName = "Tools/Game View Recorder Data")]
public class GameViewRecorderData : ScriptableObject {
    public string outputDirectory = "Assets/Recordings";
    public string filenamePrefix = "game_view_recording";
    public string latestRecordingPath; // Stores the path of the most recent recording
    public bool startOnPlay;
}
