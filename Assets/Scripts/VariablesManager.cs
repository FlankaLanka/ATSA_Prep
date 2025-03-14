using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class VariablesManager : MonoBehaviour
{
    public enum VariableStateMachine{
        NullState,
        WhitespaceBeforeStart,
        ShowingABC,
        AnsweringA,
        AnsweringB,
        AnsweringC
    }


    [Header("UI")]
    public TMP_Text timerText;
    public TMP_Text expression;
    public Image correctnessIndicator;

    public Dropdown timePerVariableDropdown;
    public Dropdown numQuestionsDropdown;
    public Toggle showTimerToggle;
    public Toggle showIndicatorToggle;

    public GameObject settingsMenu;

    [Header("Game Loop Related")]
    public int totalQuestions = 20;
    public int curQuestion = 0;
    public float timePerVariable = 2f;
    public float timer;
    public bool gameRunning = false;
    public Coroutine gameLoopCoroutine;

    public VariableStateMachine state = VariableStateMachine.NullState;
    public VariableStateMachine prevState = VariableStateMachine.NullState;

    [Header("Game Logic Related")]
    public int score, total;
    public int[] abc = new int[3];
    public string[] abcExp = new string[3];

    public void StartVariables()
    {
        settingsMenu.SetActive(false);

        int.TryParse(numQuestionsDropdown.options[numQuestionsDropdown.value].text, out totalQuestions);
        timePerVariable = TranslateDropdownTimeVariable(timePerVariableDropdown.value);
        curQuestion = 0;
        score = 0;
        total = 0;
        EnableAppropriateUI();
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
        curQuestion++;
        CalculateABC();

        //whitespace step
        timerText.text = "--:---";
        expression.text = "";
        yield return new WaitForSeconds(1f);

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
        total += 3;

        //waiting for answer
        List<int> randOrder = new List<int> { 0, 1, 2 }; //find a random order to ask for ABC
        Shuffle(randOrder);
        for (int i = 0; i < 3; i++)
        {
            expression.text = (char)('A' + randOrder[i]) + " = ?";
            timer = 0f;
            while (timer < timePerVariable)
            {
                KeyCode? key = KeyCheckHelpers.GetCurrentKeypadPressed();
                if (key != null)
                {
                    if (key - KeyCode.Keypad0 == abc[randOrder[i]])
                    {
                        score++;
                        correctnessIndicator.color = Color.green;
                    }
                    else
                    {
                        correctnessIndicator.color = Color.red;
                    }

                    timer = timePerVariable; //if recieved an answer, use timer to immediately go to next
                }
                else
                {
                    timer += Time.deltaTime;
                }
                timerText.text = GlobalFormatter.FormatTimeSecMilli(timePerVariable - timer);
                yield return null;
            }
        }

        //helps move on to next question in Update
        gameLoopCoroutine = null;
    }

    private void WhitespaceStep()
    {
        timerText.text = "-:--";
    }

    private void CalculateABC()
    {
        int qType = Random.Range(0, 3);
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

            ////loop until we find a possible fit for the equation
            //int op;
            //int val;
            //(bool, int) result;
            //do
            //{
            //    op = Random.Range(0, 4);
            //    val = Random.Range(1, 3);
            //    result = IsOperationValid(abc[r[0]], op, val);
            //} while (!result.Item1) ;

            //abc[r[2]] = result.Item2;
            //abcExp[r[2]] = ((char)(65 + r[0])).ToString() + GetOperationCharacter(op).ToString() + val.ToString();
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

}
