using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class VariablesManager : MonoBehaviour
{
    public class VariablesStat
    {
        public int questionNum;
        public string[] expressions = new string[3];
        public string[] correctAnswers = new string[3];
        public string[] inputAnswers = new string[3];
        public float[] speed = new float[3];
    }

    [Header("UI")]
    public TMP_Text timerText;
    public TMP_Text expression;
    public Image correctnessIndicator;
    public Dropdown timePerVariableDropdown;
    public Dropdown numQuestionsDropdown;
    public Toggle showTimerToggle;
    public Toggle showIndicatorToggle;
    public Dropdown algebraDropdown;

    public GameObject settingsMenu;

    [Header("Game Loop Related")]
    public int totalQuestions = 10;
    public int curQuestion = 0;
    public float timePerVariable = 2f;
    public float timer;
    public bool gameRunning = false;
    public Coroutine gameLoopCoroutine;

    [Header("Game Logic Related")]
    public int score, total; //total is calculated upon end of a question, meaning a question only counts towards stats if fully completed (all ABC for that question attempted)
    public int[] abc = new int[3];
    public string[] abcExp = new string[3];

    [Header("Stats")]
    public TMP_Text statsText;
    public TMP_Text advancedStatsText;
    public Transform statsGroup;
    public GameObject rowStatPrefab;

    private float cumulativeSpeed;


    public void StartVariables()
    {
        settingsMenu.SetActive(false);

        curQuestion = 0;
        score = 0;
        total = 0;
        int.TryParse(numQuestionsDropdown.options[numQuestionsDropdown.value].text, out totalQuestions);
        timePerVariable = TranslateDropdownTimeVariable(timePerVariableDropdown.value);
        EnableAppropriateUI();
        ResetAdvancedStats();
        gameRunning = true;
    }

    public void StopVariables()
    {
        if(gameLoopCoroutine != null)
        {
            StopCoroutine(gameLoopCoroutine);
            gameLoopCoroutine = null;
        }

        gameRunning = false;

        timerText.gameObject.SetActive(true);
        timerText.text = "--:---";
        expression.text = "-";
        correctnessIndicator.gameObject.SetActive(true);
        correctnessIndicator.color = Color.white;

        statsText.text = $"Questions challenged: {total}. Score: {score} / {total * 3}.";
        advancedStatsText.text = $"Questions challenged: {total}. Score: {score} / {total * 3}. The average speed of your responses is {(cumulativeSpeed / total / 3):F3}.";
        settingsMenu.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (!gameRunning)
            return;

        if(curQuestion > totalQuestions || Input.GetKeyDown(KeyCode.Escape))
        {
            StopVariables();
            return;
        }

        if (gameLoopCoroutine == null)
        {
            gameLoopCoroutine = StartCoroutine(RunGameCoroutine());
        }
    }

    private IEnumerator RunGameCoroutine()
    {
        CalculateABC();

        //whitespace step
        timerText.text = "--:---";
        expression.text = "";
        yield return new WaitForSeconds(1f);

        //the moment you see next variable, you start challenging the next question
        curQuestion++;

        //showing abc
        correctnessIndicator.color = Color.white;
        for(int i = 0; i < 3; i++)
        {
            expression.text = (char)('A' + i) + " = " + abcExp[i];
            timer = 0;
            while (timer < timePerVariable)
            {
                timer += Time.deltaTime;
                timerText.text = GlobalFormatter.FormatTimeSecMilli(timePerVariable - timer);
                yield return null;
            }
        }

        //create advanced stats
        VariablesStat roundStat = new();
        roundStat.questionNum = curQuestion;
        for(int i = 0; i < roundStat.expressions.Length; i++)
            roundStat.expressions[i] = (char)('A' + i) + " = " + abcExp[i];

        //waiting for answer
        List<int> randOrder = new List<int> { 0, 1, 2 }; //find a random order to ask for ABC
        Shuffle(randOrder);
        for (int i = 0; i < 3; i++)
        {
            //adv stats
            bool answerAttempted = false;

            expression.text = (char)('A' + randOrder[i]) + " = ?";
            timer = 0f;
            while (timer < timePerVariable)
            {
                KeyCode? key = KeyCheckHelpers.GetCurrentKeypadPressed();
                if (key != null)
                {
                    answerAttempted = true;

                    if (key - KeyCode.Keypad0 == abc[randOrder[i]])
                    {
                        score++;
                        correctnessIndicator.color = Color.green;
                    }
                    else
                    {
                        correctnessIndicator.color = Color.red;
                    }

                    //adv stats
                    roundStat.correctAnswers[i] = (char)('A' + randOrder[i]) + " = " + abc[randOrder[i]].ToString();
                    roundStat.inputAnswers[i] = (char)('A' + randOrder[i]) + " = " + (key - KeyCode.Keypad0).Value.ToString();
                    roundStat.speed[i] = timer;

                    timer = timePerVariable; //if recieved an answer, use timer to immediately go to next
                }
                else
                {
                    timer += Time.deltaTime;
                }
                timerText.text = GlobalFormatter.FormatTimeSecMilli(timePerVariable - timer);
                yield return null;
            }

            //if no answer, fill. adv stats
            if(!answerAttempted)
            {
                roundStat.correctAnswers[i] = (char)('A' + randOrder[i]) + " = " + abc[randOrder[i]].ToString();
                roundStat.inputAnswers[i] = (char)('A' + randOrder[i]) + " = N/A";
                roundStat.speed[i] = timePerVariable;
            }
        }

        UpdateAdvancedStats(roundStat);

        total++;
        //helps move on to next question in Update
        gameLoopCoroutine = null;
    }

    private void WhitespaceStep()
    {
        timerText.text = "-:--";
    }

    private void CalculateABC()
    {
        int qType = algebraDropdown.value;
        if(qType == 3)
            qType = Random.Range(0, 3);

        if(qType == 0) //no algebra
        {
            abc[0] = Random.Range(1, 5);
            abc[1] = Random.Range(1, 5);
            abc[2] = Random.Range(1, 5);
            abcExp[0] = abc[0].ToString();
            abcExp[1] = abc[1].ToString();
            abcExp[2] = abc[2].ToString();
        }
        else if (qType == 1) //1 algebra
        {
            List<int> r = new List<int> { 0, 1, 2 }; //find a random order to ask for ABC
            Shuffle(r);

            abc[r[0]] = Random.Range(1, 5);
            abc[r[1]] = Random.Range(1, 5);
            abcExp[r[0]] = abc[r[0]].ToString();
            abcExp[r[1]] = abc[r[1]].ToString();

            GenerateWorkingEquation(r, 0, 2);
        }
        else //2 algebra
        {
            List<int> r = new List<int> { 0, 1, 2 }; //find a random order to ask for ABC
            Shuffle(r);

            abc[r[0]] = Random.Range(1, 5);
            abcExp[r[0]] = abc[r[0]].ToString();

            GenerateWorkingEquation(r, 0, 1);
            GenerateWorkingEquation(r, 1, 2);
        }
    }

    #region Helpers

    private void GenerateWorkingEquation(List<int> r, int idxToCheck, int idxToStore)
    {
        int op;
        int val;
        (bool, int) result;
        do
        {
            op = Random.Range(0, 4);
            val = Random.Range(1, 3);
            result = IsOperationValid(abc[r[idxToCheck]], op, val);
        } while (!result.Item1);

        abc[r[idxToStore]] = result.Item2;
        abcExp[r[idxToStore]] = ((char)(65 + r[idxToCheck])).ToString() + GetOperationCharacter(op).ToString() + val.ToString();
    }


    private float TranslateDropdownTimeVariable(int val)
    {
        switch(val)
        {
            case 0:
                return 1f;
            case 1:
                return 1.5f;
            case 2:
                return 2f;
            case 3:
                return 2.5f;
            case 4:
                return 3f;
            case 5:
                return 5f;
            case 6:
                return 99f;
            default:
                return -1f;
        }
    }

    private void EnableAppropriateUI()
    {
        if (showTimerToggle.isOn)
            timerText.gameObject.SetActive(true);
        else
            timerText.gameObject.SetActive(false);

        if (showIndicatorToggle.isOn)
            correctnessIndicator.gameObject.SetActive(true);
        else
            correctnessIndicator.gameObject.SetActive(false);
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }


    private (bool,int) IsOperationValid(int start, int op, int val)
    {
        switch(op)
        {
            case 0: //+
                return (start + val <= 9, start + val);
            case 1: //-
                return (start - val >= 1, start - val);
            case 2: //*
                return (start * val <= 9, start * val);
            case 3: ///
                return (start % val == 0, start / val);
            default:
                return (false, 0);
        }
    }

    private char GetOperationCharacter(int op)
    {
        switch (op)
        {
            case 0:
                return '+';
            case 1:
                return '-';
            case 2:
                return 'x';
            case 3:
                return '/';
            default:
                return '?';
        }
    }

    #endregion


    #region advanced_stats

    private void ResetAdvancedStats()
    {
        cumulativeSpeed = 0f;

        foreach (Transform child in statsGroup.transform)
        {
            Destroy(child.gameObject);
        }
    }



    private void UpdateAdvancedStats(VariablesStat roundStat)
    {
        GameObject g = Instantiate(rowStatPrefab, statsGroup);
        TMP_Text[] roundStatText = g.GetComponentsInChildren<TMP_Text>();

        if (roundStatText.Length != 5)
        {
            Debug.LogWarning("AdvancedStats cannot be displayed properly. See calling method.");
            return;
        }

        roundStatText[0].text = "#" + roundStat.questionNum;
        roundStatText[1].text = roundStat.expressions[0] + "\n" + roundStat.expressions[1] + "\n" + roundStat.expressions[2];
        roundStatText[2].text = roundStat.correctAnswers[0] + "\n" + roundStat.correctAnswers[1] + "\n" + roundStat.correctAnswers[2];
        roundStatText[3].text = $"<color={(roundStat.inputAnswers[0] == roundStat.correctAnswers[0] ? "blue" : "red")}>{roundStat.inputAnswers[0]}</color>\n" +
                                $"<color={(roundStat.inputAnswers[1] == roundStat.correctAnswers[1] ? "blue" : "red")}>{roundStat.inputAnswers[1]}</color>\n" +
                                $"<color={(roundStat.inputAnswers[2] == roundStat.correctAnswers[2] ? "blue" : "red")}>{roundStat.inputAnswers[2]}</color>\n";
        roundStatText[4].text = roundStat.speed[0].ToString("F3") + "s\n" + roundStat.speed[1].ToString("F3") + "s\n" + roundStat.speed[2].ToString("F3") + "s";

        for (int i = 0; i < roundStat.speed.Length; i++)
            cumulativeSpeed += roundStat.speed[i];
    }


    #endregion

}
