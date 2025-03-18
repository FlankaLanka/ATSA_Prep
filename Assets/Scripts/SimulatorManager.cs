using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SimulatorManager : MonoBehaviour
{
    [Header("UI")]
    public Dropdown numSessionsDropdown;
    public Slider difficultySlider;
    public Toggle mathQuestionsToggle;

    public TMP_Text statsText;
    public TMP_Text statsMathText;

    public TMP_Text frozenText;

    public GameObject settingsMenu;

    [Header("Game Loop Related")]
    public int curSession, totalSessions;
    public float timer, timePerRound = 7f;
    public bool gameRunning, instanceRunning;

    [Header("Game Logic Related")]
    public GameObject planePrefab;
    public SpriteRenderer bg;

    public List<GameObject> allPlanes;
    public int numCollisions, totalPlanes, planesDeleted, freezeCounter;
    public bool freezeDeletion;
    [Range(1f,5f)]
    public float distanceSpawnThreshold = 0.3f;

    public SimMathManager mathManager;


    public void StartSimulator()
    {
        settingsMenu.SetActive(false);
        mathManager.gameObject.SetActive(mathQuestionsToggle.isOn);

        curSession = 0;
        int.TryParse(numSessionsDropdown.options[numSessionsDropdown.value].text, out totalSessions);
        timer = 0;
        instanceRunning = false;

        numCollisions = 0;
        totalPlanes = 0;
        planesDeleted = 0;
        freezeCounter = 0;

        freezeDeletion = false;
        frozenText.gameObject.SetActive(false);

        gameRunning = true;
    }

    public void StopSimulator()
    {
        foreach (GameObject g in allPlanes)
        {
            Destroy(g);
            totalPlanes++;
        }
        allPlanes.Clear();

        statsText.text = $"You got {numCollisions} red planes from a total of {totalPlanes}. You deleted {planesDeleted} planes. You pressed '0' {freezeCounter} times.";
        if(mathQuestionsToggle.isOn)
            statsMathText.text = $"You got {mathManager.score} / {mathManager.totalAttempted} correct out of {mathManager.total} math questions.";
        else
            statsMathText.text = "No math questions this session.";

        mathManager.gameObject.SetActive(false);
        settingsMenu.SetActive(true);

        frozenText.gameObject.SetActive(false);

        gameRunning = false;
    }

    void Update()
    {
        if (!gameRunning)
            return;

        if(curSession > totalSessions || Input.GetKeyDown(KeyCode.Escape))
        {
            StopSimulator();
            return;
        }

        if (!instanceRunning)
        {
            foreach (GameObject g in allPlanes)
            {
                Destroy(g);
                totalPlanes++;
            }
            allPlanes.Clear();

            CreatePlanes();
            instanceRunning = true;
            frozenText.gameObject.SetActive(false);
            freezeDeletion = false;
            curSession++;
            timer = 0;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Keypad0) && !freezeDeletion)
        {
            freezeDeletion = true;
            frozenText.gameObject.SetActive(true);
            freezeCounter++;
        }

        
        if (AllPlanesInactive())
        {
            instanceRunning = false;
            return;
        }
    }

    public void CreatePlanes()
    {
        int numPlanes = Math2DHelpers.GetBiasedRandomNumber();
        for(int i = 1; i <= numPlanes; i++)
        {
            //TODO: find a better spawning algorithm instead of randomly spawning and checking if too close
            int j = 100;
            Vector2 randSpawnPos = GetRandomPointOnBounds(bg.bounds);
            while(TooCloseToOtherPlanes(allPlanes, randSpawnPos, distanceSpawnThreshold) && j > 0)
            {
                randSpawnPos = GetRandomPointOnBounds(bg.bounds);
                j--;
            }
            GameObject g = Instantiate(planePrefab, randSpawnPos, Quaternion.identity);
            allPlanes.Add(g);
            PlaneInstance p = g.GetComponent<PlaneInstance>();
            p.planeID = i;
        }
    }

    private bool AllPlanesInactive()
    {
        foreach(GameObject plane in allPlanes)
        {
            if (plane.activeInHierarchy)
                return false;
        }
        return true;
    }


    #region Helpers

    public Vector2 GetRandomPointOnBounds(Bounds bounds)
    {
        float left = bounds.min.x;
        float right = bounds.max.x;
        float top = bounds.max.y;
        float bottom = bounds.min.y;

        int edge = Random.Range(0, 4);

        switch (edge)
        {
            case 0: // Top edge
                return new Vector2(Random.Range(left, right), top);
            case 1: // Bottom edge
                return new Vector2(Random.Range(left, right), bottom);
            case 2: // Left edge
                return new Vector2(left, Random.Range(bottom, top));
            case 3: // Right edge
                return new Vector2(right, Random.Range(bottom, top));
            default:
                return bounds.center;
        }
    }

    public bool TooCloseToOtherPlanes(List<GameObject> planes, Vector2 spawnPoint, float distanceThreshold)
    {
        foreach (GameObject p in planes)
        {
            //manhattan distance
            if (Mathf.Abs(p.transform.position.x - spawnPoint.x) + Mathf.Abs(p.transform.position.y - spawnPoint.y) < distanceThreshold)
                return true;
        }

        return false;
    }

    private KeyCode? GetCurrentKeypadPressed()
    {
        for (KeyCode key = KeyCode.Keypad0; key <= KeyCode.Keypad9; key++)
        {
            if (Input.GetKeyDown(key))
            {
                return key;
            }
        }

        return null;
    }


    #endregion
}
