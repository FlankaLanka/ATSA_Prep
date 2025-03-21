using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class SimMathManager : MonoBehaviour
{
    public class SimMathStat
    {
        public int a, b;
        public char op;
        public string questionStr;
        public List<float> choices = new(); //size 5. 4 answer choices + last value is the index of correct answer
        public int inputAnswer;
        public float timeAnswered;
    }

    [Header("Objects")]
    public GameObject question;
    public GameObject[] answersBoxes; //assigned in inspector, size 4

    [Header("Game Loop Related")]
    public float timer = 0f, timePerQ = 5f;

    [Header("Game Logic Related")]
    public int questionNum;
    private Coroutine mathCoroutine;
    public Color answeredQColor;

    [Header("Stats")]
    public Transform statsGroup;
    public GameObject rowStatPrefab;
    private bool displayingMath = false;

    private List<SimMathStat> allMathQuestions = new();
    public int score, total, totalAttempted;
    public float cumulativeSpeed;

    private void Awake()
    {
        this.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        questionNum = -1;
        allMathQuestions = new();
    }

    private void Update()
    {
        if(mathCoroutine == null)
        {
            mathCoroutine = StartCoroutine(MathGameLoop());
        }
    }

    private IEnumerator MathGameLoop()
    {
        displayingMath = false;
        ToggleUI(false);
        yield return new WaitForSeconds(1f);

        questionNum++;
        allMathQuestions.Add(new SimMathStat());
        SimMathStat curQuestion = allMathQuestions[questionNum];

        curQuestion.a = Random.Range(1, 100);
        curQuestion.b = Random.Range(1, 100);
        curQuestion.op = GetRandomOperation();
        curQuestion.questionStr = curQuestion.a + " " + curQuestion.op + " " + curQuestion.b;
        curQuestion.choices = GenerateAnswers(curQuestion.a, curQuestion.b, curQuestion.op); //size 5, 4 answers and last index represents the correct answer index

        question.GetComponent<TMP_Text>().text = curQuestion.questionStr;
        for(int i = 0; i < answersBoxes.Length; i++)
        {
            answersBoxes[i].GetComponent<Image>().color = Color.white;
            if(curQuestion.op == '/')
                answersBoxes[i].GetComponentInChildren<TMP_Text>().text = curQuestion.choices[i].ToString("F2");
            else
                answersBoxes[i].GetComponentInChildren<TMP_Text>().text = curQuestion.choices[i].ToString("F0");
        }
        ToggleUI(true);
        displayingMath = true;

        //for adv stat
        curQuestion.inputAnswer = -1;
        curQuestion.timeAnswered = timePerQ;

        timer = 0f;
        while(timer < timePerQ)
        {
            //while unanswered and input press
            if(curQuestion.inputAnswer == -1 && (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D)|| Input.GetKeyDown(KeyCode.F)))
            {
                if(Input.GetKeyDown(KeyCode.A))
                {
                    answersBoxes[0].GetComponent<Image>().color = answeredQColor;
                    curQuestion.inputAnswer = 0;
                }
                else if (Input.GetKeyDown(KeyCode.S))
                {
                    answersBoxes[1].GetComponent<Image>().color = answeredQColor;
                    curQuestion.inputAnswer = 1;
                }
                else if (Input.GetKeyDown(KeyCode.D))
                {
                    answersBoxes[2].GetComponent<Image>().color = answeredQColor;
                    curQuestion.inputAnswer = 2;
                }
                else if (Input.GetKeyDown(KeyCode.F))
                {
                    answersBoxes[3].GetComponent<Image>().color = answeredQColor;
                    curQuestion.inputAnswer = 3;
                }
                curQuestion.timeAnswered = timer;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        UpdateAdvancedStats(questionNum, curQuestion);
        mathCoroutine = null;
        displayingMath = false;
    }

    private void OnDisable()
    {
        if(mathCoroutine != null)
        {
            StopCoroutine(mathCoroutine);
            mathCoroutine = null;
        }

        if(displayingMath)
        {
            UpdateAdvancedStats(questionNum, allMathQuestions[questionNum]);
            displayingMath = false;
        }
        ToggleUI(false);
    }

    private void ToggleUI(bool val)
    {
        question.SetActive(val);
        foreach(GameObject a in answersBoxes)
        {
            a.SetActive(val);
        }
    }

    private char GetRandomOperation()
    {
        int r = Random.Range(0, 4);
        switch(r)
        {
            case 0:
                return '+';
            case 1:
                return '-';
            case 2:
                return '*';
            case 3:
                return '/';
            default:
                return '+';
        }
    }

    public List<float> GenerateAnswers(float num1, float num2, char operation)
    {
        float correctAnswer = Calculate(num1, num2, operation);
        List<float> answers = new List<float>();

        // Generate incorrect answers
        float wrong1 = correctAnswer * Random.Range(0.85f, 1.15f); // Slight variation (±15%)
        float wrong2 = GenerateCommonMistake(num1, num2, operation, correctAnswer);
        float wrong3 = correctAnswer + Random.Range(-5f, 5f);

        // Ensure no duplicates
        // if divide, we want no duplicate floating point answer choices. If other operation, we want no int dupes.
        if(operation == '/')
        {
            HashSet<float> uniqueAnswers = new HashSet<float> { correctAnswer, wrong1, wrong2, wrong3 };
            while (uniqueAnswers.Count < 4)
            {
                uniqueAnswers.Add(correctAnswer + Random.Range(-5f, 5f));
            }
            answers.AddRange(uniqueAnswers);
        }
        else
        {
            HashSet<int> uniqueAnswersInt = new HashSet<int> { (int)correctAnswer, (int)wrong1, (int)wrong2, (int)wrong3 };
            while (uniqueAnswersInt.Count < 4)
            {
                uniqueAnswersInt.Add((int)(correctAnswer + Random.Range(-5f, 5f)));
            }
            answers.AddRange(uniqueAnswersInt.Select(i => (float)i));
        }

        // Shuffle answers
        for (int i = 0; i < answers.Count; i++)
        {
            float temp = answers[i];
            int randomIndex = Random.Range(i, answers.Count);
            answers[i] = answers[randomIndex];
            answers[randomIndex] = temp;
        }

        int correctAnswerIndex = answers.IndexOf(correctAnswer);
        answers.Add(correctAnswerIndex);

        return answers;
    }

    private float Calculate(float num1, float num2, char operation)
    {
        return operation switch
        {
            '+' => num1 + num2,
            '-' => num1 - num2,
            '*' => num1 * num2,
            '/' => num2 != 0 ? num1 / num2 : 0, // Avoid division by zero
            _ => 0
        };
    }

    private float GenerateCommonMistake(float num1, float num2, char operation, float correctAnswer)
    {
        return operation switch
        {
            '+' => num1 - num2, // Subtract instead of add
            '-' => num1 + num2, // Add instead of subtract
            '*' => num1 + num2, // Sum instead of multiply
            '/' => num1 * num2, // Multiply instead of divide
            _ => correctAnswer + Random.Range(-5f, 5f) // Fallback
        };
    }

    #region advanced_stats

    public void ResetAdvancedStats()
    {
        foreach (Transform child in statsGroup.transform)
        {
            Destroy(child.gameObject);
        }
    }


    public void UpdateAdvancedStats(int questionNum, SimMathStat curQuestion)
    {
        GameObject g = Instantiate(rowStatPrefab, statsGroup);
        TMP_Text[] roundStatText = g.GetComponentsInChildren<TMP_Text>();

        if (roundStatText.Length != 6)
        {
            Debug.LogWarning("AdvancedStats cannot be displayed properly. See calling method.");
            return;
        }

        //assign variables for readability
        string questionStr = curQuestion.questionStr;
        List<float> choices = curQuestion.choices;
        int inputAnswer = curQuestion.inputAnswer;
        float speed = curQuestion.timeAnswered;
        char op = curQuestion.op;


        roundStatText[0].text = "#" + questionNum;
        roundStatText[1].text = questionStr;
        string precision = op == '/' ? "F2" : "F0";
        roundStatText[2].text = "A=" + choices[0].ToString(precision) + "\n" +
                                "S=" + choices[1].ToString(precision) + "\n" +
                                "D=" + choices[2].ToString(precision) + "\n" +
                                "F=" + choices[3].ToString(precision);
        roundStatText[3].text = choices[(int)choices[4]].ToString(precision);
        if (inputAnswer == -1)
        {
            roundStatText[4].text = "N/A";
            roundStatText[4].color = Color.red;
        }
        else
        {
            roundStatText[4].text = choices[inputAnswer].ToString(precision);
            roundStatText[4].color = (int)choices[4] == inputAnswer ? Color.blue : Color.red;
        }
        roundStatText[5].text = inputAnswer == -1 ? "N/A" : speed.ToString("F3") + "s";
    }

    public void CalculateScore()
    {
        score = 0;
        total = 0;
        totalAttempted = 0;
        cumulativeSpeed = 0;

        foreach(SimMathStat curQuestion in allMathQuestions)
        {
            if (curQuestion.inputAnswer == (int)curQuestion.choices[4])
                score++;
            total++;
            if (curQuestion.inputAnswer != -1)
            {
                totalAttempted++;
                cumulativeSpeed += curQuestion.timeAnswered;
            }
        }
    }

    #endregion
}
