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
        int questionNum;
        float[] choices = new float[4];
        float correctAnswer;
        float inputAnswer;
        float speed;
    }


    [Header("Objects")]
    public GameObject question;
    public GameObject[] answersBoxes; //assigned in inspector, size 4

    [Header("Game Loop Related")]
    public float timer = 0f, timePerQ = 5f;

    [Header("Game Logic Related")]
    public int score = 0, totalAttempted = 0, total = 0;
    public bool answerAttempted = false;
    private Coroutine mathCoroutine;
    public Color answeredQColor;

    [Header("Stats")]
    public Transform statsGroup;
    public GameObject rowStatPrefab;

    private void Awake()
    {
        this.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        score = 0;
        totalAttempted = 0;
        total = 0;
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
        ToggleUI(false);
        yield return new WaitForSeconds(1f);

        int a = Random.Range(1, 100);
        int b = Random.Range(1, 100);
        char op = GetRandomOperation();
        List<float> answerChoices = GenerateAnswers(a, b, op); //NOTE: length is 5, 4 answers and last index represents the correct answer index

        question.GetComponent<TMP_Text>().text = a + " " + op + " " + b;
        for(int i = 0; i < answersBoxes.Length; i++)
        {
            answersBoxes[i].GetComponent<Image>().color = Color.white;
            if(op == '/')
                answersBoxes[i].GetComponentInChildren<TMP_Text>().text = answerChoices[i].ToString("F2");
            else
                answersBoxes[i].GetComponentInChildren<TMP_Text>().text = answerChoices[i].ToString("F0");
        }
        total++;
        ToggleUI(true);

        //for adv stat
        int userInputAnswer = -1;
        float timeAnswered = 5f;

        timer = 0f;
        answerAttempted = false;
        while(timer < timePerQ)
        {
            if(!answerAttempted && (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D)|| Input.GetKeyDown(KeyCode.F)))
            {
                if(Input.GetKeyDown(KeyCode.A))
                {
                    answersBoxes[0].GetComponent<Image>().color = answeredQColor;
                    score += answerChoices[4] == 0 ? 1 : 0;
                    userInputAnswer = 0;
                }
                else if (Input.GetKeyDown(KeyCode.S))
                {
                    answersBoxes[1].GetComponent<Image>().color = answeredQColor;
                    score += answerChoices[4] == 1 ? 1 : 0;
                    userInputAnswer = 1;
                }
                else if (Input.GetKeyDown(KeyCode.D))
                {
                    answersBoxes[2].GetComponent<Image>().color = answeredQColor;
                    score += answerChoices[4] == 2 ? 1 : 0;
                    userInputAnswer = 2;
                }
                else if (Input.GetKeyDown(KeyCode.F))
                {
                    answersBoxes[3].GetComponent<Image>().color = answeredQColor;
                    score += answerChoices[4] == 3 ? 1 : 0;
                    userInputAnswer = 3;
                }
                totalAttempted++;
                answerAttempted = true;

                //for adv stats
                timeAnswered = timer;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        UpdateAdvancedStats(total, answerChoices, userInputAnswer, timeAnswered, op);
        mathCoroutine = null;
    }

    private void OnDisable()
    {
        if(mathCoroutine != null)
        {
            StopCoroutine(mathCoroutine);
            mathCoroutine = null;
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

    public void UpdateAdvancedStats(int questionNum, List<float> choices, int inputAnswer, float speed, char op)
    {
        GameObject g = Instantiate(rowStatPrefab, statsGroup);
        TMP_Text[] roundStatText = g.GetComponentsInChildren<TMP_Text>();

        if (roundStatText.Length != 5)
        {
            Debug.LogWarning("AdvancedStats cannot be displayed properly. See calling method.");
            return;
        }

        roundStatText[0].text = "#" + questionNum;
        string precision = op == '/' ? "F2" : "F0";
        roundStatText[1].text = "A=" + choices[0].ToString(precision) + "\n" +
                                "S=" + choices[1].ToString(precision) + "\n" +
                                "D=" + choices[2].ToString(precision) + "\n" +
                                "F=" + choices[3].ToString(precision);
        roundStatText[2].text = choices[(int)choices[4]].ToString(precision);
        if (inputAnswer == -1)
        {
            roundStatText[3].text = "N/A";
            roundStatText[3].color = Color.red;
        }
        else
        {
            roundStatText[3].text = choices[inputAnswer].ToString(precision);
            roundStatText[3].color = (int)choices[4] == inputAnswer ? Color.blue : Color.red;
        }
        roundStatText[4].text = speed.ToString("F3") + "s";
    }

    #endregion
}
