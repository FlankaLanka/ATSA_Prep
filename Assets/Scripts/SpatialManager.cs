using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SpatialManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text questionNumText;
    public TMP_Text timePerPicText;
    public Image correctnessIndicator;

    public Dropdown totalTimeDropdown;
    public Dropdown timePerPicDropdown;
    public Toggle totalTimeToggle;
    public Toggle timePerPicToggle;
    public Toggle indicatiorToggle;

    public GameObject settingsMenu;

    [Header("Sprites")]
    public GameObject background;
    public GameObject redPlane;
    public GameObject blackPlane;
    public GameObject eye;
    public TMP_Text leftRightText;

    [Header("Game Loop Related")]
    public int totalQuestions = 60;
    public int curQuestion = 0;
    public float timePerPicture = 2f;
    public float curPicTimer = 0f;
    public bool gameRunning = false;

    [Header("Game Logic Related")]
    public int score, total;
    public bool correctAnswer = false;
    public SpriteRenderer spawnbox;


    [Header("Stats")]
    public TMP_Text statsText;
    public TMP_Text advancedStatsText;
    public Transform statsGroup;
    public GameObject rowStatPrefab;

    private float cumulativeSpeed = 0f;

    public Dictionary<GameObject, Sprite> questionSnapshots = new();
    //public List<Texture2D> questionSnapshots = new();



    public void StartSpatial()
    {
        settingsMenu.SetActive(false);

        score = 0;
        total = 0;
        curQuestion = 0;
        int.TryParse(totalTimeDropdown.options[totalTimeDropdown.value].text, out totalQuestions);
        timePerPicture = TranslateDropdownTimePerPic(timePerPicDropdown.value);
        EnableAppropriateUI();
        ResetAdvancedStats();

        //basically LoadNewOrientations();
        curPicTimer = timePerPicture + 1;
        gameRunning = true;
    }

    public void StopSpatial()
    {
        gameRunning = false;

        leftRightText.text = "";
        questionNumText.text = "QuestionNum";
        timePerPicText.text = "Time Per Image";
        correctnessIndicator.color = Color.white;

        redPlane.transform.position = new Vector2(1000, 1000);
        blackPlane.transform.position = new Vector2(2000, 1000);
        eye.transform.position = new Vector2(3000, 1000);

        statsText.text = $"You scored {score} / {Mathf.Min(curQuestion, totalQuestions)}.";
        advancedStatsText.text = $"You scored {score} / {Mathf.Min(curQuestion, totalQuestions)}. Average speed of correct answers: {(cumulativeSpeed / score):F3}s. " +
            $"Hover over a question to see the image.";

        settingsMenu.SetActive(true);
    }

    public void LoadNewOrientations()
    {
        //cheaper than SetActive()
        redPlane.transform.position = new Vector2(1000,1000);
        blackPlane.transform.position = new Vector2(2000, 1000);
        eye.transform.position = new Vector2(3000, 1000);

        //text
        bool textLR = Random.Range(0, 2) == 0 ? false : true;
        leftRightText.text = textLR == false ? "Left" : "Right";

        //planes
        SetPositionAndOrientation(redPlane.transform, spawnbox.bounds);
        SetPositionAndOrientation(blackPlane.transform, spawnbox.bounds);
        bool actualLR = Vector2.Dot(blackPlane.transform.right, redPlane.transform.position - blackPlane.transform.position) >= 0; //false = L, true = R

        //eye
        if (Random.Range(0, 9) <= 3) //40% chance for eye to appear
        {
            SetPositionAndOrientation(eye.transform, spawnbox.bounds);
            //the eye changes the correct answer
            actualLR = Vector2.Dot(eye.transform.right, redPlane.transform.position - eye.transform.position) >= 0; //false = L, true = R
        }

        correctAnswer = textLR == actualLR;
        curQuestion++;
        questionNumText.text = $"{curQuestion} / {totalQuestions}";
        curPicTimer = 0f;
    }


    private void Update()
    {
        if (!gameRunning)
            return;

        if(curPicTimer >= timePerPicture)
        {
            if (curQuestion >= 1 && curQuestion <= totalQuestions)
                UpdateAdvancedStats(curQuestion, correctAnswer ? "T" : "F", "N/A", timePerPicture);
            LoadNewOrientations();
        }
        else
        {
            curPicTimer += Time.deltaTime;
        }
        timePerPicText.text = GlobalFormatter.FormatTimeSecMilli(timePerPicture - curPicTimer);


        if (curQuestion > totalQuestions || Input.GetKeyDown(KeyCode.Escape))
        {
            if (Input.GetKeyDown(KeyCode.Escape) && !(curQuestion > totalQuestions))
                UpdateAdvancedStats(curQuestion, correctAnswer ? "T" : "F", "N/A", timePerPicture);
            StopSpatial();
            return;
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            if (correctAnswer == true)
            {
                score++;
                correctnessIndicator.color = Color.green;
            }
            else
            {
                correctnessIndicator.color = Color.red;
            }
            UpdateAdvancedStats(curQuestion, correctAnswer ? "T" : "F", "T", curPicTimer - Time.deltaTime);
            LoadNewOrientations();
        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            if (correctAnswer == false)
            {
                score++;
                correctnessIndicator.color = Color.green;
            }
            else
            {
                correctnessIndicator.color = Color.red;
            }
            UpdateAdvancedStats(curQuestion, correctAnswer ? "T" : "F", "F", curPicTimer - Time.deltaTime);
            LoadNewOrientations();
        }
    }

    #region Helpers

    public void SetPositionAndOrientation(Transform target, Bounds spawnbox)
    {
        CircleCollider2D circleCollider = target.GetComponent<CircleCollider2D>();
        Debug.Assert(circleCollider != null);

        //keep testing random positions until valid
        Vector2 randomPos;
        do
        {
            randomPos = new Vector2(Random.Range(spawnbox.min.x, spawnbox.max.x), Random.Range(spawnbox.min.y, spawnbox.max.y));
        } while (Physics2D.OverlapCircle(randomPos, circleCollider.radius * target.lossyScale.x));

        target.position = randomPos;
        target.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));

        //remove cases where answer is hard to visually tell
        if (target == blackPlane.transform)
        {
            while (WithinAngleRange(target, redPlane.transform))
            {
                target.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
            }
        }
        else if (target == eye.transform)
        {
            LookAtTarget2D(target, blackPlane.transform);
            //TODO: perhaps find a better algorithm, instead of randomly checking points
            while (WithinAngleRange(target, redPlane.transform))
            {
                SetPositionAndOrientation(redPlane.transform, spawnbox);
            }
        }

        Physics2D.SyncTransforms();
    }

    public bool WithinAngleRange(Transform obj1, Transform obj2)
    {
        Vector2 directionToTarget = obj2.position - obj1.position;
        float angle = Vector2.SignedAngle(obj1.up, directionToTarget);
        return (angle >= -20f && angle <= 20f) || angle >= 160f || angle <= -160f;
    }

    public void LookAtTarget2D(Transform obj, Transform target)
    {
        Vector2 direction = (target.position - obj.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        obj.rotation = Quaternion.Euler(0f, 0f, angle - 90f); // -90f to make up vector point to airplane
    }

    public float TranslateDropdownTimeLimit(int val)
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

    public float TranslateDropdownTimePerPic(int val)
    {
        switch (val)
        {
            case 0:
                return 1f;
            case 1:
                return 1.25f;
            case 2:
                return 1.5f;
            case 3:
                return 1.75f;
            case 4:
                return 2f;
            case 5:
                return 2.25f;
            case 6:
                return 2.5f;
            case 7:
                return 2.75f;
            case 8:
                return 3f;
            case 9:
                return 5f;
            case 10:
                return 8f;
            case 11:
                return 99f;
            default:
                return -1;
        }
    }

    public void EnableAppropriateUI()
    {
        if (totalTimeToggle.isOn)
            questionNumText.gameObject.SetActive(true);
        else
            questionNumText.gameObject.SetActive(false);

        if (timePerPicToggle.isOn)
            timePerPicText.gameObject.SetActive(true);
        else
            timePerPicText.gameObject.SetActive(false);

        if (indicatiorToggle.isOn)
            correctnessIndicator.gameObject.SetActive(true);
        else
            correctnessIndicator.gameObject.SetActive(false);
    }

    #endregion

    #region advanced_stats
        
    private void ResetAdvancedStats()
    {
        cumulativeSpeed = 0f;
        questionSnapshots.Clear();
        foreach (Transform child in statsGroup.transform)
        {
            Destroy(child.gameObject);
        }
    }

    private void UpdateAdvancedStats(int questionNum, string correctAnswer, string inputAnswer, float speed)
    {
        //Q: when do you update advancedstats? A: when you press T/F, when timer runs out, or when user presses Esc

        GameObject g = Instantiate(rowStatPrefab, statsGroup);
        TMP_Text[] roundStatText = g.GetComponentsInChildren<TMP_Text>();

        if (roundStatText.Length != 4)
        {
            Debug.LogWarning("AdvancedStats cannot be displayed properly. See calling method.");
            return;
        }

        roundStatText[0].text = "#" + questionNum;
        roundStatText[1].text = correctAnswer;
        roundStatText[2].text = inputAnswer;
        roundStatText[3].text = speed.ToString("F3") + "s";

        Color outputColor = correctAnswer == inputAnswer ? Color.blue : Color.red;
        for (int i = 0; i < roundStatText.Length; i++)
        {
            roundStatText[i].color = outputColor;
        }

        if (correctAnswer == inputAnswer)
            cumulativeSpeed += speed;

        questionSnapshots.Add(g, TakeSnapshot(Camera.main));
    }

    private Sprite TakeSnapshot(Camera cam)
    {
        if (cam == null)
        {
            Debug.LogError("Camera is null! Cannot take a snapshot.");
            return null;
        }

        // Set the render texture
        RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24);
        cam.targetTexture = rt;
        cam.Render();

        // Read pixels from the render texture
        Texture2D snapshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        RenderTexture.active = rt;
        snapshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        snapshot.Apply();

        // Clean up
        cam.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        return TextureToSprite(snapshot);
    }

    private Sprite TextureToSprite(Texture2D tex)
    {
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    #endregion
}
