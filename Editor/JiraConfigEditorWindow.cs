using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class JiraConfigEditorWindow : EditorWindow {
    private const string ConfigFilePath = "Assets/JiraConfig.json";
    private JiraConfig config = new JiraConfig();

    [MenuItem("Tools/ASU DSL/Configure Jira Instance")]
    public static void ShowWindow() {
        GetWindow<JiraConfigEditorWindow>("Configure Jira Instance");
    }

    private void OnEnable() {
        LoadConfig();
    }

    private void OnGUI() {
        GUILayout.Label("Jira Configuration", EditorStyles.boldLabel);

        config.jiraBaseUrl = EditorGUILayout.TextField("Jira Base URL", config.jiraBaseUrl);
        config.email = EditorGUILayout.TextField("Email", config.email);
        config.apiToken = EditorGUILayout.PasswordField("API Token", config.apiToken);

        if (GUILayout.Button("Save Configuration")) {
            SaveConfig();
        }
    }

    private void SaveConfig() {
        string json = JsonUtility.ToJson(config, true);
        File.WriteAllText(ConfigFilePath, json);
        AssetDatabase.Refresh(); // Refresh Unity to show the new file in the project
        Debug.Log("Jira configuration saved.");
    }

    private void LoadConfig() {
        if (File.Exists(ConfigFilePath)) {
            string json = File.ReadAllText(ConfigFilePath);
            config = JsonUtility.FromJson<JiraConfig>(json);
            Debug.Log("Jira configuration loaded.");
        }
    }
}


[Serializable]
public class JiraConfig {
    public string jiraBaseUrl;
    public string email;
    public string apiToken;
}
