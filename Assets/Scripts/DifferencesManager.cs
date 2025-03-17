using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DifferencesManager : MonoBehaviour
{
    public class DifferencesStat
    {
        public int questionNum;
        public int prevNum;
        public int curNum;
        public int correctAnswer;
        public int inputAnswer;
        public float answerSpeed;

        public DifferencesStat(int questionNum, int prevNum, int curNum, int correctAnswer, int inputAnswer, float answerSpeed)
        {
            this.questionNum = questionNum;
            this.prevNum = prevNum;
            this.curNum = curNum;
            this.correctAnswer = correctAnswer;
            this.inputAnswer = inputAnswer;
            this.answerSpeed = answerSpeed;
        }
    }

    [Header("UI")]
    public TMP_Text displayedNumber;
    public TMP_Text timeLimitText;
    public Image correctnessIndicator;

    public Dropdown timeLimitDropdown;
    public Toggle showTimeLimit;
    public Toggle showCorrectnessIndicator;

    public GameObject SettingsMenu;

    [Header("Game Loop Related")]
    private float timer = 0f, totalTime = 30f, speedTimer = 0f;
    private bool gameRunning = false;
    private Coroutine timerCoroutine;
    private Coroutine nextNumberCoroutine;

    [Header("Game Logic Related")]
    private int score = 0, total = 0;
    private int num1 = 0, num2 = 0;

    [Header("Stats")]
    public TMP_Text statsText;
    public TMP_Text advancedStatsText;
    public Transform statsGroup;
    public GameObject rowStatPrefab;

    private float fastestCorrectAnswer, fastestQuestionNum;
    private float slowestCorrectAnswer, slowestQuestionNum;
    private float cumulativeSpeed;


    public void StartDifferences()
    {
        SettingsMenu.SetActive(false);

        score = 0;
        total = 0;
        totalTime = TranslateDropdownTimeLimit(timeLimitDropdown.value);
        timerCoroutine = StartCoroutine(StartTimer(totalTime));
        EnableAppropriateUI();
        ResetAdvancedStats();
        SetNextNumber();
        nextNumberCoroutine = StartCoroutine(CalculateSecondNumber());
    }

    private IEnumerator StartTimer(float totalTime)
    {
        timer = 0f;
        while(timer < totalTime)
        {
            timer += Time.deltaTime;
            timeLimitText.text = GlobalFormatter.FormatTimeMinSec(totalTime - timer);
            yield return null;
        }
        StopDifferences();
    }

    private IEnumerator CalculateSecondNumber()
    {
        yield return new WaitForSeconds(1.5f);
        SetNextNumber();
        gameRunning = true;
    }

    public void StopDifferences()
    {
        //reset
        gameRunning = false;
        displayedNumber.text = "-";
        timeLimitText.gameObject.SetActive(true);
        timeLimitText.text = "0:00";
        correctnessIndicator.gameObject.SetActive(true);
        correctnessIndicator.color = Color.white;

        StopCoroutine(timerCoroutine);
        StopCoroutine(nextNumberCoroutine);

        statsText.text = $"You got {score} / {total} correct with a {((float)score / total) * 100:F2}% accuracy in {totalTime:F0} seconds.";
        advancedStatsText.text = $"You got {score} / {total} correct with a {((float)score / total) * 100:F2}% accuracy in {totalTime:F0} seconds. " +
            $"Fastest correct: {fastestCorrectAnswer:F3}s on Q{fastestQuestionNum}. Slowest correct: {slowestCorrectAnswer:F3}s on Q{slowestQuestionNum}. " +
            $"Average speed per answer: {cumulativeSpeed/total:F3}s.";

        SettingsMenu.SetActive(true);
    }


    private void Update()
    {
        if (!gameRunning)
            return;

        if(Input.GetKeyDown(KeyCode.Escape))
        {
            StopDifferences();
        }

        speedTimer += Time.deltaTime;

        KeyCode? key = KeyCheckHelpers.GetCurrentKeypadPressed();
        if(key != null)
        {
            int calculatedDiff = key.Value - KeyCode.Keypad0;
            int correctAns = Mathf.Abs(num1 - num2);
            if (correctAns == calculatedDiff)
            {
                score++;
                correctnessIndicator.color = Color.green;
            }
            else
            {
                correctnessIndicator.color = Color.red;
            }
            total++;

            DifferencesStat roundStats = new DifferencesStat(total, num2, num1, correctAns, calculatedDiff, speedTimer);
            UpdateAdvancedStats(roundStats);

            SetNextNumber();
        }
    }


    #region Helpers

    private void SetNextNumber()
    {
        num2 = num1;
        while (Mathf.Abs(num1 - num2) > 4 || Mathf.Abs(num1 - num2) < 1)
        {
            num1 = Random.Range(1, 10);
        }
        displayedNumber.text = num1.ToString();

        speedTimer = 0f;
    }

    private float TranslateDropdownTimeLimit(int val)
    {
        switch (val)
        {
            case 0:
                return 15f;
            case 1:
                return 30f;
            case 2:
                return 45f;
            case 3:
                return 60f;
            case 4:
                return 120f;
            case 5:
                return 180f;
            case 6:
                return 240f;
            case 7:
                return 300f;
            default:
                return 5940f;
        }
    }

    private void EnableAppropriateUI()
    {
        if (showCorrectnessIndicator.isOn)
            correctnessIndicator.gameObject.SetActive(true);
        else
            correctnessIndicator.gameObject.SetActive(false);

        if (showTimeLimit.isOn)
        {
            timeLimitText.gameObject.SetActive(true);
        }
        else
        {
            timeLimitText.gameObject.SetActive(false);
        }
    }

    #endregion

    #region advanced_stats

    private void ResetAdvancedStats()
    {
        fastestCorrectAnswer = 99999f;
        slowestCorrectAnswer = 0f;
        fastestQuestionNum = -1;
        slowestQuestionNum = -1;
        cumulativeSpeed = 0f;

        foreach (Transform child in statsGroup.transform)
        {
            Destroy(child.gameObject);
        }
    }


    private void UpdateAdvancedStats(DifferencesStat roundStat)
    {
        GameObject g = Instantiate(rowStatPrefab, statsGroup);
        TMP_Text[] roundStatText = g.GetComponentsInChildren<TMP_Text>();

        if (roundStatText.Length != 6)
        {
            Debug.LogWarning("AdvancedStats cannot be displayed properly. See calling method.");
            return;
        }

        roundStatText[0].text = "#" + roundStat.questionNum.ToString();
        roundStatText[1].text = roundStat.prevNum.ToString();
        roundStatText[2].text = roundStat.curNum.ToString();
        roundStatText[3].text = roundStat.correctAnswer.ToString();
        roundStatText[4].text = roundStat.inputAnswer.ToString();
        roundStatText[5].text = roundStat.answerSpeed.ToString("F3") + "s";

        bool gotCorrect = roundStat.correctAnswer == roundStat.inputAnswer;
        foreach(TMP_Text t in roundStatText)
        {
            if (gotCorrect)
                t.color = Color.blue;
            else
                t.color = Color.red;
        }

        if (gotCorrect && fastestCorrectAnswer > roundStat.answerSpeed)
        {
            fastestCorrectAnswer = roundStat.answerSpeed;
            fastestQuestionNum = roundStat.questionNum;
        }
        if (gotCorrect && slowestCorrectAnswer < roundStat.answerSpeed)
        {
            slowestCorrectAnswer = roundStat.answerSpeed;
            slowestQuestionNum = roundStat.questionNum;
        }
        cumulativeSpeed += roundStat.answerSpeed;
    }

    #endregion
}
