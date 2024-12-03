using System;
using System.Collections;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using System.IO;

public class JiraIssueEditorWindow : EditorWindow {
    private string jiraBaseUrl = "https://asudev.jira.com";
    private string email = "abahrema@asu.edu";
    private string apiToken = "ATATT3xFfGF0d5p6xXJkcaCx2KQRIbP1YEb60923Wa4jaIF9wt732RcTLkguDBPcLqvIjkd27G3yMkOyw77buDSObpjHYwbjHrfm_uEn-mlpWhOafjnMZ14evj8Y3O2-kRj5i2-tQdFmDldvHKWUW73DSyrClVwNIznZ1djYam91oQ8KI9N-yq4=2F33B441"; //Environment.GetEnvironmentVariable("JIRA_API_TOKEN");

    private string issueTitle = "";
    private string issueDescription = "";
    private string videoFilePath = "";
    private UnityEngine.Object videoFile;

    private string createdTicketId = "";
    private string createdTicketUrl = "";
    private JiraConfig config;
    private GameViewRecorderData recorderData;


    [MenuItem("Tools/ASU DSL/Jira Issue Creator")]
    public static void ShowWindow() {
        // Show existing window instance. If one doesn’t exist, make a new one.
        GetWindow<JiraIssueEditorWindow>("Jira Issue Creator");
    }

    private void LoadConfig() {
        if (File.Exists("Assets/JiraConfig.json")) {
            string json = File.ReadAllText("Assets/JiraConfig.json");
            config = JsonUtility.FromJson<JiraConfig>(json);
        }
    }

    private void OnEnable() {
        LoadConfig();
        jiraBaseUrl = config.jiraBaseUrl;
        email = config.email;
        apiToken = config.apiToken;

        recorderData = FindGameViewRecorderData();
        if (recorderData != null && !string.IsNullOrEmpty(recorderData.latestRecordingPath)) {
            videoFilePath = recorderData.latestRecordingPath + ".mp4"; // Set initial videoFilePath
        }
    }


    private GameViewRecorderData FindGameViewRecorderData() {
        string[] guids = AssetDatabase.FindAssets("t:GameViewRecorderData");
        foreach (string guid in guids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<GameViewRecorderData>(path);
            if (data != null)
                return data;
        }
        return null;
    }

    private void OnGUI() {
        GUILayout.Label("Create a Jira Issue", EditorStyles.boldLabel);

        issueTitle = EditorGUILayout.TextField("Title", issueTitle);
        issueDescription = EditorGUILayout.TextArea(issueDescription, GUILayout.Height(60));

        // Display video file path and allow for drag-and-drop to override it
        videoFile = EditorGUILayout.ObjectField("Video File", videoFile, typeof(UnityEngine.Object), false);

        // Update videoFilePath based on the selected or dropped file
        if (videoFile != null) {
            string draggedFilePath = AssetDatabase.GetAssetPath(videoFile);
            if (!string.IsNullOrEmpty(draggedFilePath)) {
                videoFilePath = System.IO.Path.GetFullPath(draggedFilePath);
            }
        }

        EditorGUILayout.LabelField("Video File Path", videoFilePath); // Display current videoFilePath

        if (GUILayout.Button("Create Jira Issue")) {
            if (string.IsNullOrEmpty(issueTitle)) {
                Debug.LogError("Issue title cannot be empty.");
                return;
            }
            StartCoroutine(CreateIssueCoroutine(issueTitle, issueDescription, videoFilePath));
        }

        if (!string.IsNullOrEmpty(createdTicketId)) {
            EditorGUILayout.LabelField("Ticket ID", createdTicketId);
            if (GUILayout.Button(createdTicketUrl, EditorStyles.linkLabel)) {
                Application.OpenURL(createdTicketUrl);
            }
        }
    }


    private void StartCoroutine(IEnumerator coroutine) {
        EditorCoroutineRunner.StartEditorCoroutine(coroutine);
    }

    private IEnumerator CreateIssueCoroutine(string title, string description, string videoFilePath) {
        // Create the issue
        string issueKey = null;
        string createIssueUrl = $"{jiraBaseUrl}/rest/api/3/issue";

        JObject issueData = new JObject {
            ["fields"] = new JObject {
                ["project"] = new JObject { ["key"] = "DSLI" }, // Replace with your project key
                ["summary"] = title,
                ["description"] = new JObject {
                    ["type"] = "doc",
                    ["version"] = 1,
                    ["content"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "paragraph",
                            ["content"] = new JArray
                            {
                                new JObject
                                {
                                    ["type"] = "text",
                                    ["text"] = description
                                }
                            }
                        }
                    }
                },
                ["issuetype"] = new JObject { ["name"] = "Task" }
            }
        };

        string requestBody = issueData.ToString();
        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);

        using (UnityWebRequest request = new UnityWebRequest(createIssueUrl, "POST")) {
            SetAuthHeaders(request);

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success) {
                Debug.Log("Issue created successfully!");
                var responseJson = JObject.Parse(request.downloadHandler.text);
                createdTicketId = responseJson["key"]?.ToString();
                createdTicketUrl = $"{jiraBaseUrl}/browse/{createdTicketId}";

                Debug.Log($"Ticket ID: {createdTicketId}");
                Debug.Log($"Ticket URL: {createdTicketUrl}");
            }
            else {
                Debug.LogError($"Error creating issue: {request.error}");
            }


        }

        // Attach video if file path is provided and issue was created
        if (!string.IsNullOrEmpty(videoFilePath) && !string.IsNullOrEmpty(issueKey)) {
            yield return AttachVideoToIssue(issueKey);
        }

        Repaint();

    }

    private IEnumerator AttachVideoToIssue(string issueKey) {
        string url = $"{jiraBaseUrl}/rest/api/3/issue/{issueKey}/attachments";
        string videoFilePath = @"C:\Users\abahrema\Documents\Projects\editor-recording-tool\Assets\Recordings\game_view_recording_2024-11-11_16-45.mp4";

        // Read the video file as bytes
        byte[] videoBytes;
        try {
            videoBytes = System.IO.File.ReadAllBytes(videoFilePath);
        }
        catch (Exception e) {
            Debug.LogError($"Error reading video file: {e.Message}");
            yield break; // Exit if the file cannot be read
        }

        using (UnityWebRequest request = new UnityWebRequest(url, "POST")) {
            // Set up authentication and headers
            string auth = $"{email}:{apiToken}";
            string base64Auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(auth));
            request.SetRequestHeader("Authorization", $"Basic {base64Auth}");
            request.SetRequestHeader("X-Atlassian-Token", "no-check");

            // Prepare multipart form data
            WWWForm form = new WWWForm();
            form.AddBinaryData("file", videoBytes, System.IO.Path.GetFileName(videoFilePath), "video/mp4");

            request.uploadHandler = new UploadHandlerRaw(form.data);
            request.SetRequestHeader("Content-Type", form.headers["Content-Type"]);
            request.downloadHandler = new DownloadHandlerBuffer();

            // Send the request
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success) {
                Debug.Log("Video attached successfully!");
            }
            else {
                Debug.LogError($"Error attaching video: {request.error}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
            }
        }
    }

    private void SetAuthHeaders(UnityWebRequest request) {
        string auth = $"{email}:{apiToken}";
        string base64Auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(auth));
        request.SetRequestHeader("Authorization", $"Basic {base64Auth}");
        request.SetRequestHeader("Content-Type", "application/json");
    }
}

public static class EditorCoroutineRunner {
    private class CoroutineHolder : MonoBehaviour { }

    private static CoroutineHolder coroutineHolder;

    public static void StartEditorCoroutine(IEnumerator coroutine) {
        if (coroutineHolder == null) {
            coroutineHolder = new GameObject("EditorCoroutineRunner").AddComponent<CoroutineHolder>();
            coroutineHolder.hideFlags = HideFlags.HideAndDontSave;
        }

        coroutineHolder.StartCoroutine(CleanupCoroutine(coroutine));
    }

    private static IEnumerator CleanupCoroutine(IEnumerator coroutine) {
        yield return coroutine;

        // Destroy the CoroutineHolder GameObject after the coroutine completes
        if (coroutineHolder != null) {
            GameObject.DestroyImmediate(coroutineHolder.gameObject);
            coroutineHolder = null;
        }
    }
}