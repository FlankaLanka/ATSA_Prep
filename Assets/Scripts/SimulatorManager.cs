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

    [Header("Game Loop Related")]
    public int curSession, totalSessions;
    public float timer, timePerRound = 7f;
    public bool gameRunning, instanceRunning;


    [Header("Game Logic Related")]
    public GameObject planePrefab;
    public SpriteRenderer spawnbox;

    public List<GameObject> allPlanes;
    public int numCollisions;


    public void StartSimulator()
    {
        settingsMenu.SetActive(false);

        int.TryParse(numSessionsDropdown.options[numSessionsDropdown.value].text, out totalSessions);
        numCollisions = 0;
        gameRunning = true;
    }

    public void StopSimulator()
    {
        settingsMenu.SetActive(true);
        gameRunning = false;
    }


    // Update is called once per frame
    void Update()
    {
        if (!gameRunning)
            return;

        if(curSession > totalSessions)
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
        int numPlanes = GetBiasedRandomNumber();
        for(int i = 0; i < numPlanes; i++)
        {
            GameObject g = Instantiate(planePrefab, GetRandomPointOnBounds(spawnbox.bounds), Quaternion.identity);
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

    public int GetBiasedRandomNumber()
    {
        int[] numbers = { 2, 3, 4, 5, 6, 7, 8, 9 };
        float[] weights = { 0.05f, 0.10f, 0.15f, 0.20f, 0.20f, 0.15f, 0.10f, 0.05f }; // Higher chance for 5 and 6

        float totalWeight = 0;
        foreach (float weight in weights) totalWeight += weight;

        float randomValue = Random.Range(0f, totalWeight);
        float cumulative = 0;

        for (int i = 0; i < numbers.Length; i++)
        {
            cumulative += weights[i];
            if (randomValue <= cumulative)
                return numbers[i];
        }

        return 5;
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
