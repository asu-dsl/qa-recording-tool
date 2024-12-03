#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameViewRecorder))]
public class GameViewRecorderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GameViewRecorder recorder = (GameViewRecorder)target;

        if (GUILayout.Button("Start Recording"))
        {
            recorder.StartRecording();
        }
        
        if (GUILayout.Button("Stop Recording"))
        {
            recorder.StopRecording();
        }
        
        if (GUILayout.Button("Open Latest Recording"))
        {
            recorder.OpenLatestRecording();
        }
    }
}
#endif
