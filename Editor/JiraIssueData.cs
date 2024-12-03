using UnityEngine;

[CreateAssetMenu(fileName = "JiraIssueData", menuName = "Jira/Jira Issue Data")]
public class JiraIssueData : ScriptableObject {
    public string issueTitle;
    [TextArea] public string issueDescription;
    public string videoFilePath; // Path to the video file you want to attach
}