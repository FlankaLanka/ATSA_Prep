using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

    public void StartVariables()
    {
        settingsMenu.SetActive(false);

        int.TryParse(numQuestionsDropdown.options[numQuestionsDropdown.value].text, out totalQuestions);
        timePerVariable = TranslateDropdownTimeVariable(timePerVariableDropdown.value);
        score = 0;
        total = 0;

        gameRunning = true;

        //gameLoopCoroutine = StartCoroutine(RunGameCoroutine());
    }

    public void StopVariables()
    {
        StopCoroutine(gameLoopCoroutine);

        gameRunning = false;
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
            expression.text = (char)('A' + i) + " = " + abc[i];
            timer = 0;
            while (timer < timePerVariable)
            {
                timer += Time.deltaTime;
                timerText.text = GlobalFormatter.FormatTimeSecMilli(timePerVariable - timer);
                yield return null;
            }
        }

        //waiting for answer
        for(int i = 0; i < 3; i++)
        {

        }



        gameLoopCoroutine = null;
    }

    private void WhitespaceStep()
    {
        timerText.text = "-:--";
    }

    private void CalculateABC()
    {
        abc[0] = Random.Range(1, 5);
        abc[1] = Random.Range(1, 5);
        abc[2] = Random.Range(1, 5);
    }

    #region Helpers


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

    #endregion

}
