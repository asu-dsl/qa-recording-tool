using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

[CustomEditor(typeof(JiraIssueData))]
public class JiraIssueCreator : Editor {
    private string jiraBaseUrl = "https://asudev.jira.com";
    private string email = "abahrema@asu.edu";
    private string apiToken = "ATATT3xFfGF0d5p6xXJkcaCx2KQRIbP1YEb60923Wa4jaIF9wt732RcTLkguDBPcLqvIjkd27G3yMkOyw77buDSObpjHYwbjHrfm_uEn-mlpWhOafjnMZ14evj8Y3O2-kRj5i2-tQdFmDldvHKWUW73DSyrClVwNIznZ1djYam91oQ8KI9N-yq4=2F33B441"; //Environment.GetEnvironmentVariable("JIRA_API_TOKEN");


    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        JiraIssueData issueData = (JiraIssueData)target;

        if (GUILayout.Button("Create Jira Issue")) {
            // Start the creation process when button is clicked
            CreateJiraIssue(issueData.issueTitle, issueData.issueDescription, issueData.videoFilePath);
        }
    }

    private void CreateJiraIssue(string title, string description, string videoFilePath) {
        EditorCoroutineUtility.StartCoroutineOwnerless(CreateIssueCoroutine(title, description, videoFilePath));
    }

    private IEnumerator CreateIssueCoroutine(string title, string description, string videoFilePath) {
        // Create the issue first
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
                issueKey = responseJson["key"]?.ToString();
            }
            else {
                Debug.LogError($"Error creating issue: {request.error}");
                yield break;
            }
        }

        // Attach video if file path is provided and issue was created
        if (!string.IsNullOrEmpty(videoFilePath) && !string.IsNullOrEmpty(issueKey)) {
            yield return AttachVideoToIssue(issueKey, videoFilePath);
        }
    }

    private IEnumerator AttachVideoToIssue(string issueKey, string videoFilePath) {
        string attachUrl = $"{jiraBaseUrl}/rest/api/3/issue/{issueKey}/attachments";
        byte[] videoBytes;

        try {
            videoBytes = System.IO.File.ReadAllBytes(videoFilePath);
        }
        catch (Exception e) {
            Debug.LogError($"Error reading video file: {e.Message}");
            yield break;
        }

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>
        {
            new MultipartFormFileSection("file", videoBytes, System.IO.Path.GetFileName(videoFilePath), "video/mp4")
        };

        using (UnityWebRequest request = UnityWebRequest.Post(attachUrl, formData)) {
            SetAuthHeaders(request);
            request.SetRequestHeader("X-Atlassian-Token", "no-check");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success) {
                Debug.Log("Video attached successfully!");
            }
            else {
                Debug.LogError($"Error attaching video: {request.error}");
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