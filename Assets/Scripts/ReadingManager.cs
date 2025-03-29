using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class ReadingManager : MonoBehaviour
{
    private static int NO_ANSWER_SELECTED = -65;

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
        public int inputAnswer = NO_ANSWER_SELECTED; //65 - 65 = 0 = 'NULL' character
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

    public List<Question> allQuestions = new List<Question>();
    public int currentQuestionIndex = 0;
    public int selectedAnswerIndex = -1;
    public int score = 0;
    public float timeRemaining;
    public bool isExamActive = false;
    public ToggleGroup toggleGroup;



    // Original UI text values
    public string originalPassageText;
    public string originalQuestionText;
    public string[] originalAnswerTexts = new string[4];
    public string originalTimerText;
    public string originalQuestionNumText;


    [Header("Stats")]
    public TMP_Text statsText;
    public TMP_Text advancedStatsText;
    public GameObject questionNumButtonsList;
    private Button[] questionButtons;
    private List<Image> questionButtonBorders = new();
    private List<Image> questionButtonBodies = new();
    public TMP_Text advContentText; //include passage, question, and answers


    public void BeginExam()
    {
        totalTimeLimit = timeLimitDropdown.value == 0 ? 1200f : 59940f;
        examUI.SetActive(true);
        settingsMenu.SetActive(false);
        isExamActive = true;

        currentQuestionIndex = 0;
        selectedAnswerIndex = -1;
        score = 0;
        allQuestions.Clear();

        LoadJSONData();
        InitializeSystem();
    }

    void LoadJSONData()
    {
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
        timerText.text = UpdateTimerDisplay(timeRemaining);
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
            timerText.text = UpdateTimerDisplay(timeRemaining);

            if (timeRemaining <= 0 || Input.GetKeyDown(KeyCode.Escape))
            {
                timeRemaining = Mathf.Max(0, timeRemaining);
                EndExam();
            }
        }
    }

    string UpdateTimerDisplay(float timeRemaining)
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        return $"{minutes:00}:{seconds:00}";
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
        allQuestions[currentQuestionIndex].inputAnswer = selectedAnswerIndex;

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

        // Show final score
        statsText.text = $"Score: {score}/{allQuestions.Count}. ({((float)score * 100 / allQuestions.Count):F2}%). Time left: {UpdateTimerDisplay(timeRemaining)}.";
        advancedStatsText.text = $"Score: {score}/{allQuestions.Count}. ({((float)score * 100 / allQuestions.Count):F2}%).\nTime left: {UpdateTimerDisplay(timeRemaining)}.";
        SetContextInAdvStats();

        examUI.SetActive(false);
        settingsMenu.SetActive(true);
    }


    #region advanced_stats

    private void Awake()
    {
        questionButtons = questionNumButtonsList.GetComponentsInChildren<Button>();
        SetPreContentInAdvStats();
    }

    private void SetPreContentInAdvStats()
    {
        int i = 1;
        foreach(Button b in questionButtons)
        {
            int temp = i;
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => LoadButtonQuestion(temp));
            //b.GetComponentInChildren<TMP_Text>().text = i.ToString();

            Image questionButtonBody = b.gameObject.GetComponent<Image>();
            questionButtonBodies.Add(questionButtonBody);
            Image questionButtonBorder = b.transform.parent.GetComponent<Image>();
            questionButtonBorders.Add(questionButtonBorder);
            //questionBorder.enabled = false;

            i++;
        }
    }

    private void ClearAllBorders()
    {
        foreach(Image i in questionButtonBorders)
        {
            i.enabled = false;
        }
    }

    private void ClearAllBodies()
    {
        foreach(Image i in questionButtonBodies)
        {
            i.color = Color.white;
        }
    }

    private void SetContextInAdvStats()
    {
        ClearAllBorders();
        ClearAllBodies();
        advContentText.text = "";
        for(int i = 0; i < allQuestions.Count && i < questionButtonBodies.Count; i++)
        {
            questionButtonBodies[i].color = allQuestions[i].correctAnswerIndex == allQuestions[i].inputAnswer ? Color.green : Color.red;
            if (allQuestions[i].inputAnswer == NO_ANSWER_SELECTED)
                questionButtonBodies[i].color = Color.yellow;
        }
    }

    private void LoadButtonQuestion(int questionNum)
    {
        if (questionNum > allQuestions.Count)
            return;
        questionNum--; //get the index rather than visual value

        //clear borders and set this one
        ClearAllBorders();
        questionButtonBorders[questionNum].enabled = true;

        //content
        string contentText = "";
        contentText += allQuestions[questionNum].passageContent + "\n\n";
        contentText += allQuestions[questionNum].questionText + "\n\n";

        int temp = 0;
        foreach(string answer in allQuestions[questionNum].answers)
        {
            contentText += $"{(char)(temp + 65)}) {answer}\n";
            temp++;
        }
        contentText += "\nCorrect Answer: " + (char)(allQuestions[questionNum].correctAnswerIndex + 65) + "\n";
        contentText += "Selected Answer: " + (char)(allQuestions[questionNum].inputAnswer + 65) + "\n";
        contentText += "(Explanations work in progress)";

        advContentText.text = contentText;
    }

    #endregion
}