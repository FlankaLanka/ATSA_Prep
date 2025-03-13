using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DifferencesManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text displayedNumber;
    public TMP_Text timeLimitText;
    public Image correctnessIndicator;

    public Dropdown timeLimitDropdown;
    public Toggle showTimeLimit;
    public Toggle showCorrectnessIndicator;

    public GameObject SettingsMenu;


    [Header("Game Loop Related")]
    public float totalTime = 30f;
    public float timer = 0f;
    public bool gameRunning = false;
    private Coroutine timerCoroutine;
    private Coroutine nextNumberCoroutine;


    [Header("Game Logic Related")]
    public int score = 0, total = 0;
    public int num1 = 0, num2 = 0;


    //TODO: stats
    private string logOutput = "| prevNum | curNum | difference | keypressed | rxn_time | timer_val_on_answer |";


    public void StartDifferences()
    {
        Debug.Log("Started");

        SettingsMenu.SetActive(false);

        totalTime = TranslateDropdownTimeLimit(timeLimitDropdown.value);
        timerCoroutine = StartCoroutine(StartTimer(totalTime));

        EnableAppropriateUI();

        score = 0;
        total = 0;
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
        Debug.Log("Stopped");

        //reset
        gameRunning = false;
        displayedNumber.text = "-";
        timeLimitText.gameObject.SetActive(true);
        timeLimitText.text = "0:00";
        correctnessIndicator.gameObject.SetActive(true);
        correctnessIndicator.color = Color.white;

        StopCoroutine(timerCoroutine);
        StopCoroutine(nextNumberCoroutine);

        UpdateStats();
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

        KeyCode? key = KeyCheckHelpers.GetCurrentKeypadPressed();
        if(key != null)
        {
            Debug.Log("Pressed: " + key);
            int calculatedDiff = key.Value - KeyCode.Keypad0;
            if (Mathf.Abs(num1 - num2) == calculatedDiff)
            {
                score++;
                correctnessIndicator.color = Color.green;
            }
            else
            {
                correctnessIndicator.color = Color.red;
            }
            total++;
            SetNextNumber();
        }

        
        //for (KeyCode key = KeyCode.Keypad1; key <= KeyCode.Keypad9; key++)
        //{
        //    if (Input.GetKeyDown(key))
        //    {
        //        Debug.Log("Pressed: " + key);
        //        int calculatedDiff = key - KeyCode.Keypad0;
        //        if (Mathf.Abs(num1 - num2) == calculatedDiff)
        //        {
        //            score++;
        //            correctnessIndicator.color = Color.green;
        //        }
        //        else
        //        {
        //            correctnessIndicator.color = Color.red;
        //        }
        //        total++;
        //        SetNextNumber();
        //        break;
        //    }
        //}
    }


    #region Helpers

    private void SetNextNumber()
    {
        num2 = num1;
        while (num1 == num2)
        {
            num1 = Random.Range(1, 10);
        }
        displayedNumber.text = num1.ToString();
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


    private void UpdateStats()
    {

    }

    #endregion
}
