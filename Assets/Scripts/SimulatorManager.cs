using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SimulatorManager : MonoBehaviour
{
    [Header("UI")]
    public Dropdown numSessionsDropdown;
    public Toggle mathQuestionsToggle;

    public GameObject settingsMenu;
    public GameObject mathManager;

    [Header("Game Loop Related")]
    public int curSession, totalSessions;
    public float timer, timePerRound = 7f;
    public bool gameRunning, instanceRunning;


    [Header("Game Logic Related")]
    public GameObject planePrefab;
    public SpriteRenderer bg;

    public List<GameObject> allPlanes;
    public int numCollisions;
    [Range(1f,5f)]
    public float distanceSpawnThreshold = 0.3f;


    public void StartSimulator()
    {
        settingsMenu.SetActive(false);
        mathManager.SetActive(mathQuestionsToggle.isOn);

        int.TryParse(numSessionsDropdown.options[numSessionsDropdown.value].text, out totalSessions);
        numCollisions = 0;
        gameRunning = true;
    }

    public void StopSimulator()
    {
        settingsMenu.SetActive(true);
        mathManager.SetActive(false);
        gameRunning = false;
    }


    // Update is called once per frame
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
                Destroy(g);
            allPlanes.Clear();

            CreatePlanes();
            instanceRunning = true;
            curSession++;
            timer = 0;
            return;
        }

        if(timer >= timePerRound)
        {
            instanceRunning = false;
            return;
        }
        timer += Time.deltaTime;
    }

    public void CreatePlanes()
    {
        int numPlanes = Math2DHelpers.GetBiasedRandomNumber();
        for(int i = 0; i < numPlanes; i++)
        {
            int j = 10;
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
            if (Mathf.Abs(p.transform.position.x - spawnPoint.x) + Mathf.Abs(p.transform.position.y - spawnPoint.y) > distanceThreshold)
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
