using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpatialManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text totalTimeText;
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
    public float totalTime = 30f;
    public float totalTimer = 0f;
    public float timePerPicture = 2f;
    public float curPicTimer = 0f;
    public bool gameRunning = false;

    [Header("Game Logic Related")]
    public int score, total;
    public bool correctAnswer = false;
    public SpriteRenderer spawnbox;


    public void StartSpatial()
    {
        settingsMenu.SetActive(false);

        score = 0;
        total = 0;
        totalTimer = 0f;
        timePerPicture = 0f;

        totalTime = TranslateDropdownTimeLimit(totalTimeDropdown.value);
        timePerPicture = TranslateDropdownTimePerPic(timePerPicDropdown.value);

        EnableAppropriateUI();

        LoadNewOrientations();

        gameRunning = true;
    }

    public void StopSpatial()
    {
        gameRunning = false;

        leftRightText.text = "";
        totalTimeText.text = "Session Time";
        timePerPicText.text = "Per Image Time";
        correctnessIndicator.color = Color.white;

        redPlane.transform.position = new Vector2(1000, 1000);
        blackPlane.transform.position = new Vector2(2000, 1000);
        eye.transform.position = new Vector2(3000, 1000);
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
        total++;
        curPicTimer = 0f;
    }


    private void Update()
    {
        if (!gameRunning)
            return;

        if(totalTimer >= totalTime || Input.GetKeyDown(KeyCode.Escape))
        {
            StopSpatial();
            return;
        }
        totalTimer += Time.deltaTime;
        totalTimeText.text = GlobalFormatter.FormatTimeMinSec(totalTime - totalTimer);

        if(curPicTimer >= timePerPicture)
        {
            LoadNewOrientations();
        }
        curPicTimer += Time.deltaTime;
        timePerPicText.text = GlobalFormatter.FormatTimeSecMilli(timePerPicture - curPicTimer);

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
            //TODO: sometimes the eye may line up with the black and red plane making it hard to tell L/R, try to remove these cases
        }

        Physics2D.SyncTransforms();
    }

    public bool WithinAngleRange(Transform obj1, Transform obj2)
    {
        Vector2 directionToTarget = obj2.position - obj1.position;
        float angle = Vector2.SignedAngle(obj1.up, directionToTarget);
        return (angle >= -15f && angle <= 15f) || angle >= 165f || angle <= -165f;
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
            totalTimeText.gameObject.SetActive(true);
        else
            totalTimeText.gameObject.SetActive(false);

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
}
