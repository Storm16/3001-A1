using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartScreenManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject startPanel; // The panel containing name, instructions, button
    public Text studentInfoText;
    public Button playButton;

    [Header("Student Info")]
    public string studentName = "Andella Maddex";
    public string studentNumber = "#101619941";
    public string projectName = "Steering Demo 2D";

    void Start()
    {
        // Show Start Panel
        startPanel.SetActive(true);

        // Set student info text
        if (studentInfoText != null)
            studentInfoText.text = $"Name: {studentName}\nStudent Number: {studentNumber}\nProject: {projectName}\n\nInstructions:\n1 = Seek\n2 = Flee\n3 = Arrive\n4 = Avoid\n0 = Reset";

        // Add listener to Play button
        if (playButton != null)
            playButton.onClick.AddListener(StartGame);
    }

    void StartGame()
    {
        // Hide start panel
        startPanel.SetActive(false);  // This disables the panel and everything under it

        // Initialize SteeringDemo2D
        SteeringDemo2D demo = Object.FindFirstObjectByType<SteeringDemo2D>();
        if (demo != null)
        {
            demo.ResetScene();
        }
    }
}
