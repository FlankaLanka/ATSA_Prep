using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimMathManager : MonoBehaviour
{
    [Header("Objects")]
    public GameObject question;
    public GameObject[] answers; //assigned in inspector, size 4

    [Header("Game Loop Related")]
    public float timer = 0f, timePerQ = 5f;

    [Header("Game Logic Related")]
    public int score = 0, total = 0;
    public int correctAnswer = 0;
    public bool answerAttempted = false;
    private Coroutine mathCoroutine;

    private void Awake()
    {
        this.gameObject.SetActive(false);
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

        int a = Random.Range(0, 9);
        int b = Random.Range(0, 9);

        question.GetComponent<TMP_Text>().text = a + " + " + b;
        for(int i = 0; i < answers.Length; i++)
        {
            answers[i].GetComponentInChildren<TMP_Text>().text = Random.Range(0,10).ToString();
        }
        total++;
        ToggleUI(true);

        timer = 0f;
        answerAttempted = false;
        while(timer < timePerQ)
        {
            timer += Time.deltaTime;
            if(!answerAttempted && (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D)|| Input.GetKeyDown(KeyCode.F)))
            {
                Debug.Log("attempted answer");
                answerAttempted = true;
                if( Input.GetKeyDown(KeyCode.A) && correctAnswer == 0 ||
                    Input.GetKeyDown(KeyCode.S) && correctAnswer == 1 ||
                    Input.GetKeyDown(KeyCode.D) && correctAnswer == 2 ||
                    Input.GetKeyDown(KeyCode.F) && correctAnswer == 3)
                {
                    score++;
                }
            }
            yield return null;
        }

        mathCoroutine = null;
    }

    private void OnDisable()
    {
        if(mathCoroutine != null)
            StopCoroutine(mathCoroutine);
        ToggleUI(false);
    }

    private void ToggleUI(bool val)
    {
        question.SetActive(val);
        foreach(GameObject a in answers)
        {
            a.SetActive(val);
        }
    }
}
