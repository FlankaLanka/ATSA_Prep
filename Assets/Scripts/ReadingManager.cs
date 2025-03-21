using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class ReadingManager : MonoBehaviour
{
    [System.Serializable]
    public class PassageData
    {
        public string title;
        public string content;
        public List<Question> questions;
    }

    [System.Serializable]
    public class ReadingPackage
    {
        public List<PassageData> passages;
    }

    [System.Serializable]
    public class Question
    {
        public string passageContent; // Added to store associated passage text
        public string questionText;
        public string[] answers = new string[4];
        public int correctAnswerIndex;
    }

    [Header("UI Elements")]
    public Dropdown timeLimitDropdown;
    public Dropdown questionSetDropdown;

    [SerializeField] private TMP_Text passageText;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text[] answerTexts;
    [SerializeField] private Toggle[] answerToggles;
    [SerializeField] private Button submitButton;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text questionNumText;

    public GameObject examUI;
    public GameObject settingsMenu;

    [Header("Exam Settings")]
    [SerializeField] private TextAsset[] jsonFile;
    [SerializeField] private float totalTimeLimit = 1200f;

    private List<Question> allQuestions = new List<Question>();
    private int currentQuestionIndex = 0;
    private int selectedAnswerIndex = -1;
    private int score = 0;
    private float timeRemaining;
    private bool isExamActive = false;
    private ToggleGroup toggleGroup;

    // Original UI text values
    private string originalPassageText;
    private string originalQuestionText;
    private string[] originalAnswerTexts = new string[4];
    private string originalTimerText;
    private string originalQuestionNumText;


    [Header("Stats")]
    public TMP_Text statsText;


    public void BeginExam()
    {
        //score = 0;

        totalTimeLimit = timeLimitDropdown.value == 0 ? 1200f : 59940f;
        examUI.SetActive(true);
        settingsMenu.SetActive(false);

        isExamActive = true;
        LoadJSONData();
        InitializeSystem();
    }

    void LoadJSONData()
    {
        //currentQuestionIndex = 0;
        //allQuestions.Clear();
        if (jsonFile != null)
        {
            ReadingPackage data = JsonUtility.FromJson<ReadingPackage>(jsonFile[questionSetDropdown.value].text);
            foreach (PassageData passage in data.passages)
            {
                foreach (Question question in passage.questions)
                {
                    // Add passage content to each question
                    Question newQuestion = new Question
                    {
                        passageContent = passage.content,
                        questionText = question.questionText,
                        answers = question.answers,
                        correctAnswerIndex = question.correctAnswerIndex
                    };
                    allQuestions.Add(newQuestion);
                }
            }
        }
        else
        {
            Debug.LogError("No JSON file assigned!");
        }
    }

    void InitializeSystem()
    {
        toggleGroup = GetComponent<ToggleGroup>();
        InitializeToggles();
        StoreOriginalTextValues();
        submitButton.onClick.AddListener(SubmitAnswer);
        timeRemaining = totalTimeLimit;
        UpdateTimerDisplay();
        LoadQuestion(currentQuestionIndex);
    }

    void StoreOriginalTextValues()
    {
        originalPassageText = passageText.text;
        originalQuestionText = questionText.text;
        originalTimerText = timerText.text;
        originalQuestionNumText = questionNumText.text;
        for (int i = 0; i < answerTexts.Length; i++)
        {
            originalAnswerTexts[i] = answerTexts[i].text;
        }
    }

    void Update()
    {
        if (isExamActive)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerDisplay();

            if (timeRemaining <= 0)
            {
                timeRemaining = 0;
                EndExam();
            }
        }
    }

    void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    void InitializeToggles()
    {
        toggleGroup.allowSwitchOff = true;
        foreach (Toggle toggle in answerToggles)
        {
            toggle.group = toggleGroup;
            toggle.onValueChanged.AddListener((isOn) => OnAnswerSelected(toggle, isOn));
        }
    }

    void LoadQuestion(int index)
    {
        if (!isExamActive || index >= allQuestions.Count) return;

        // Set passage and question text
        passageText.text = allQuestions[index].passageContent;
        questionNumText.text = $"Q{index + 1}/{allQuestions.Count}";
        questionText.text = allQuestions[index].questionText;

        // Set answers
        for (int i = 0; i < answerTexts.Length; i++)
        {
            answerTexts[i].text = allQuestions[index].answers[i];
        }

        // Reset UI state
        toggleGroup.SetAllTogglesOff();
        submitButton.interactable = false;
        selectedAnswerIndex = -1;
    }

    void OnAnswerSelected(Toggle selectedToggle, bool isOn)
    {
        if (!isExamActive) return;

        selectedAnswerIndex = -1;
        for (int i = 0; i < answerToggles.Length; i++)
        {
            if (answerToggles[i].isOn)
            {
                selectedAnswerIndex = i;
                break;
            }
        }
        submitButton.interactable = selectedAnswerIndex != -1;
    }

    void SubmitAnswer()
    {
        if (!isExamActive || selectedAnswerIndex == -1) return;

        // Check answer
        if (selectedAnswerIndex == allQuestions[currentQuestionIndex].correctAnswerIndex)
        {
            score++;
            Debug.Log("Correct!");
        }
        else
        {
            Debug.Log("Incorrect!");
        }

        // Move to next question
        currentQuestionIndex++;

        if (currentQuestionIndex < allQuestions.Count)
        {
            LoadQuestion(currentQuestionIndex);
        }
        else
        {
            EndExam();
        }
    }

    void EndExam()
    {
        isExamActive = false;

        // Restore original text
        passageText.text = originalPassageText;
        questionText.text = originalQuestionText;
        timerText.text = originalTimerText;
        questionNumText.text = originalQuestionNumText;

        for (int i = 0; i < answerTexts.Length; i++)
        {
            answerTexts[i].text = originalAnswerTexts[i];
        }

        // Disable interaction
        foreach (Toggle toggle in answerToggles)
        {
            toggle.isOn = false;
            toggle.interactable = false;
        }
        submitButton.interactable = false;

        // Show final score
        //Debug.Log($"Final Score: {score}/{allQuestions.Count}");
        statsText.text = $"Final Score: {score}/{allQuestions.Count}.";

        examUI.SetActive(false);
        settingsMenu.SetActive(true);
    }
}