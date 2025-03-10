using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DifferencesManager : MonoBehaviour
{
    public TMP_Text displayedNumber;
    public TMP_Text timeLimitText;
    public Image correctnessIndicator;
    public Dropdown timeLimitDropdown;
    public Toggle showTimeLimit;
    public Toggle showCorrectnessIndicator;

    public GameObject SettingsMenu;

    [HideInInspector]
    public float totalTime = 30f;
    public float timer = 0f;

    public int score = 0, total = 0;
    public int num1 = 0, num2 = 0;
    public bool gameRunning = false;

    private Coroutine timerCoroutine;
    private Coroutine nextNumberCoroutine;



    
    private string logOutput = "| prevNum | curNum | difference | keypressed | rxn_time | timer_val_on_answer |";

    public void StartDifferences()
    {
        Debug.Log("Started");

        SettingsMenu.SetActive(false);

        TranslateDropdownTimeLimit();
        if(totalTime > 0f)
        {
            timerCoroutine = StartCoroutine(StartTimer(totalTime));
        }
        else
        {
            timeLimitText.text = "Untimed Session";
        }
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
            timeLimitText.text = FormatTime(totalTime - timer);
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

        for (KeyCode key = KeyCode.Keypad1; key <= KeyCode.Keypad9; key++)
        {
            if (Input.GetKeyDown(key))
            {
                Debug.Log("Pressed: " + key);
                int calculatedDiff = key - KeyCode.Keypad0;
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
                break;
            }
        }
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

    private void TranslateDropdownTimeLimit()
    {
        int limit = timeLimitDropdown.value;
        switch (limit)
        {
            case 0:
                totalTime = 15f;
                break;
            case 1:
                totalTime = 30f;
                break;
            case 2:
                totalTime = 450f;
                break;
            case 3:
                totalTime = 60f;
                break;
            case 4:
                totalTime = 120f;
                break;
            case 5:
                totalTime = 180f;
                break;
            case 6:
                totalTime = 240f;
                break;
            case 7:
                totalTime = 300f;
                break;
            default:
                totalTime = -1f;
                break;
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

    public string FormatTime(float timeInSeconds)
    {
        int minutes = (int)(timeInSeconds / 60);
        int seconds = (int)(timeInSeconds % 60);
        //int milliseconds = (int)((timeInSeconds - (int)timeInSeconds) * 1000);

        //return $"{minutes}:{seconds:00}:{milliseconds:000}";
        return $"{minutes}:{seconds:00}";
    }

    #endregion
}
