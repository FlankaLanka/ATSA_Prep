using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class SimulatorManager : MonoBehaviour
{
    //these 2 classes are used for running the replay systems
    public class ReplaySessionInfoStats
    {
        public int sessionNum;
        public List<int> optimalDeletes = new();
        public List<FakePlaneInfoStats> currentPlanesInfo = new();

        public ReplaySessionInfoStats(int sn)
        {
            sessionNum = sn;
        }
    }

    public class FakePlaneInfoStats
    {
        public int planeID;
        public Vector2 spawnPos;
        public float speed;
        public Vector2 direction;
        public float timeOfDelete = 99999f;

        public FakePlaneInfoStats(int planeID, Vector2 spawnPos, float speed, Vector2 direction, float timeOfDelete = 99999f)
        {
            this.planeID = planeID;
            this.spawnPos = spawnPos;
            this.speed = speed;
            this.direction = direction;
            this.timeOfDelete = timeOfDelete;
        }
    }


    [Header("UI")]
    public Dropdown numSessionsDropdown;
    public Slider difficultySlider;
    public Toggle mathQuestionsToggle;

    public TMP_Text frozenText;

    public GameObject settingsMenu;

    [Header("Game Loop Related")]
    public int curSession, totalSessions;
    public float zeroTimer, roundTimer;
    public bool gameRunning, instanceRunning;

    [Header("Game Logic Related")]
    public GameObject planePrefab;
    public SpriteRenderer bg;
    public List<GameObject> allPlanes;
    public int numCollisions, totalPlanes, planesDeleted, freezeCounter;
    public List<(int, int)> inputCollisions = new();
    public bool freezeDeletion;
    [Range(1f,5f)]
    public float distanceSpawnThreshold = 0.3f;

    public SimMathManager mathManager;

    [Header("Stats")]
    public TMP_Text statsText;
    public TMP_Text statsMathText;
    public Transform statsGroup;
    public GameObject rowStatPrefab;

    private Dictionary<GameObject, List<GameObject>> collisionsGraph;
    private List<GameObject> planesToDelete;
    public List<int> inputDeletes = new();

    [Header("For visual replay system")]
    public List<ReplaySessionInfoStats> replaySessions = new();
    public List<GameObject> fakePlanesGameObjects = new(); //stores the physical fake planes on screen, use this to destroy
    public int curReplaySession = 0;


    public void StartSimulator()
    {
        settingsMenu.SetActive(false);
        mathManager.gameObject.SetActive(mathQuestionsToggle.isOn);

        curSession = 0;
        int.TryParse(numSessionsDropdown.options[numSessionsDropdown.value].text, out totalSessions);
        instanceRunning = false;

        numCollisions = 0;
        totalPlanes = 0;
        planesDeleted = 0;
        freezeCounter = 0;

        ResetAdvancedStats();
        mathManager.ResetAdvancedStats();

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

        if (!instanceRunning) // each instance refers to one wave of planes spawning and flying across
        {
            foreach (GameObject g in allPlanes)
            {
                Destroy(g);
                totalPlanes++;
            }
            allPlanes.Clear();

            CreatePlanes();
            freezeDeletion = false;
            frozenText.gameObject.SetActive(false);
            curSession++;
            zeroTimer = 0f;
            roundTimer = 0f;
            inputCollisions = new();
            inputDeletes = new();

            instanceRunning = true;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Keypad0) && !freezeDeletion)
        {
            freezeDeletion = true;
            frozenText.gameObject.SetActive(true);
            freezeCounter++;
        }
        if (!freezeDeletion)
            zeroTimer += Time.deltaTime;
        roundTimer += Time.deltaTime;

        if (AllPlanesInactive())
        {
            UpdateAdvancedStats(curSession, collisionsGraph, planesToDelete, inputDeletes, inputCollisions, freezeDeletion, zeroTimer, roundTimer);
            instanceRunning = false;
            return;
        }
    }

    public void CreatePlanes()
    {
        replaySessions.Add(new ReplaySessionInfoStats(curSession));

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

            //for replay stats
            FakePlaneInfoStats newFakePlane = new(p.planeID, randSpawnPos, p.speed, p.direction);
            p.fakePlaneReference = newFakePlane;
            replaySessions[curSession].currentPlanesInfo.Add(newFakePlane);
        }
        collisionsGraph = CalculateCollisionsMap(allPlanes);
        planesToDelete = GetOptimalDeletions(collisionsGraph);
        replaySessions[curSession].optimalDeletes = planesToDelete.Select(plane => plane.GetComponent<PlaneInstance>().planeID).ToList();
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

    #region advanced_stats

    public void ResetAdvancedStats()
    {
        replaySessions = new();
        foreach (GameObject child in fakePlanesGameObjects)
        {
            Destroy(child);
        }
        fakePlanesGameObjects.Clear();
        foreach (Transform child in statsGroup.transform)
        {
            Destroy(child.gameObject);
        }
    }


    public void UpdateAdvancedStats(int curSession, Dictionary<GameObject, List<GameObject>> collisionsGraph, List<GameObject> planesToDelete,
        List<int> inputDeletes, List<(int, int)> inputCollisions, bool freezeDeletion, float zeroTimer, float roundTimer)
    {
        //Q: when do you update advancedstats? A: when round ends or when esc pressed

        GameObject g = Instantiate(rowStatPrefab, statsGroup);
        TMP_Text[] roundStatText = g.GetComponentsInChildren<TMP_Text>();

        if (roundStatText.Length != 6)
        {
            Debug.LogWarning("AdvancedStats cannot be displayed properly. See calling method.");
            return;
        }

        roundStatText[0].text = curSession.ToString();
        roundStatText[1].text = LogPossibleCollisions(collisionsGraph);
        roundStatText[2].text = "";
        foreach ((int, int) collidedPair in inputCollisions)
        {
            if (collidedPair.Item1 < collidedPair.Item2) //ensure that we only write to text once per pair
            {
                roundStatText[2].text += $"[{collidedPair.Item1},{collidedPair.Item2}] ";
            }
        }
        roundStatText[3].text = "";
        foreach (GameObject d in planesToDelete)
        {
            roundStatText[3].text += d.GetComponent<PlaneInstance>().planeID + ", ";
        }
        roundStatText[4].text = "";
        foreach (int input in inputDeletes)
        {
            roundStatText[4].text += input + ", ";
        }
        roundStatText[5].text = (freezeDeletion ? zeroTimer.ToString("F3") : "N/A") + "s / " + roundTimer.ToString("F3") + "s";

    }

    public string LogPossibleCollisions(Dictionary<GameObject, List<GameObject>> graph)
    {
        string output = "";
        HashSet<(int, int)> reportedConnections = new();

        foreach (var kvp in graph)
        {
            int nodeX = kvp.Key.GetComponent<PlaneInstance>().planeID;
            foreach (GameObject connectedNode in kvp.Value)
            {
                // Ensure we only log each connection once
                int nodeY = connectedNode.GetComponent<PlaneInstance>().planeID;

                (int,int) edge = (nodeX, nodeY);
                (int,int) reverseEdge = (nodeY, nodeX);

                if (!reportedConnections.Contains(reverseEdge))
                {
                    output += $"[{nodeX},{nodeY}] ";
                    reportedConnections.Add(edge);
                }
            }
        }

        return output;
    }


    public Dictionary<GameObject, List<GameObject>> CalculateCollisionsMap(List<GameObject> allPlanes)
    {
        Dictionary<GameObject, List<GameObject>> collisionGraph = new();

        //allPlanes is prefab that contains PlaneInstance.cs and a collider.
        foreach (GameObject plane in allPlanes)
        {
            CircleCollider2D planeCollider = plane.GetComponent<CircleCollider2D>();
            PlaneInstance planeInst = plane.GetComponent<PlaneInstance>();

            for(int i = 0; i < allPlanes.Count; i++)
            {
                if (plane == allPlanes[i]) //dont check plane collision with self
                    continue;

                GameObject otherPlane = allPlanes[i];
                CircleCollider2D otherPlaneCollider = otherPlane.GetComponent<CircleCollider2D>();
                PlaneInstance otherPlaneInst = otherPlane.GetComponent<PlaneInstance>();
                Vector2? collisionPoint = GetCollisionPoint(plane.transform.position, planeCollider.radius * plane.transform.lossyScale.x, planeInst.speed, planeInst.direction,
                                                            otherPlane.transform.position, otherPlaneCollider.radius * otherPlane.transform.lossyScale.x, otherPlaneInst.speed, otherPlaneInst.direction,
                                                            bg.bounds);
                if(collisionPoint != null)
                {
                    if (!collisionGraph.ContainsKey(plane))
                        collisionGraph[plane] = new();

                    collisionGraph[plane].Add(otherPlane);
                }
            }

        }

        return collisionGraph;
    }

    public static Vector2? GetCollisionPoint(
            Vector2 posA, float radiusA, float speedA, Vector2 directionA,
            Vector2 posB, float radiusB, float speedB, Vector2 directionB,
            Bounds bounds)
    {
        // Compute velocity vectors
        Vector2 velocityA = directionA * speedA;
        Vector2 velocityB = directionB * speedB;

        // Relative position and velocity
        Vector2 R = posA - posB;
        Vector2 V = velocityA - velocityB;
        float rSum = radiusA + radiusB;

        // Quadratic coefficients: a*t^2 + b*t + c = 0
        float a = Vector2.Dot(V, V);
        float b = 2 * Vector2.Dot(R, V);
        float c = Vector2.Dot(R, R) - rSum * rSum;

        float discriminant = b * b - 4 * a * c;
        if (discriminant < 0)
            return null; // No real solution means no collision

        float sqrtDiscriminant = Mathf.Sqrt(discriminant);
        // Get the smallest non-negative solution (earliest collision time)
        float t1 = (-b - sqrtDiscriminant) / (2 * a);
        float t2 = (-b + sqrtDiscriminant) / (2 * a);

        float t = (t1 >= 0) ? t1 : (t2 >= 0) ? t2 : -1;
        if (t < 0)
            return null; // Both solutions are negative: collision in the past

        // Compute the collision point based on circle A's position
        Vector2 collisionPointA = posA + velocityA * t;
        Vector2 collisionPointB = posB + velocityB * t;
        Vector2 collisionPoint = (collisionPointA + collisionPointB) / 2;
        Debug.Log(collisionPoint);

        // Check if the collision point is within bounds
        if (bounds.Contains(new Vector3(collisionPoint.x, collisionPoint.y, 0)))
            return collisionPoint;

        return null; // Collision occurs outside the bounding box
    }


    private List<GameObject> GetOptimalDeletions(Dictionary<GameObject, List<GameObject>> collisionsGraph)
    {
        List<GameObject> toDelete = new();
        Dictionary<GameObject, HashSet<GameObject>> graph = collisionsGraph.ToDictionary(
            kvp => kvp.Key, kvp => new HashSet<GameObject>(kvp.Value)
        );

        while (graph.Count > 0)
        {
            // Find node with the highest degree (most connections)
            GameObject maxNode = graph.OrderByDescending(kvp => kvp.Value.Count).First().Key;

            // Remove the node and all its edges
            foreach (var neighbor in graph[maxNode])
            {
                graph[neighbor].Remove(maxNode);
            }
            graph.Remove(maxNode);

            toDelete.Add(maxNode);

            // Remove isolated nodes (nodes with no edges left)
            List<GameObject> isolatedNodes = graph.Where(kvp => kvp.Value.Count == 0)
                                                  .Select(kvp => kvp.Key).ToList();
            foreach (var node in isolatedNodes)
            {
                graph.Remove(node);
            }
        }

        return toDelete;
    }



    #endregion


    #region replay_system

    public void InitiateReplay(int replayType)
    {
        //replayType
        //0 -> User
        //1 -> Optimal
        //2 -> NoDelete

        ClearFakePlanes();

        foreach (FakePlaneInfoStats fakePlaneInfo in replaySessions[curReplaySession].currentPlanesInfo)
        {
            GameObject fplane = Instantiate(planePrefab, fakePlaneInfo.spawnPos, Quaternion.identity);
            PlaneInstance fpInst = fplane.GetComponent<PlaneInstance>();

            fpInst.isFake = true;
            fpInst.planeID = fakePlaneInfo.planeID;
            fpInst.speed = fakePlaneInfo.speed;
            fpInst.direction = fakePlaneInfo.direction;

            if (replayType == 0) //timeOfDelete for manual delete is set in PlaneInstance
            {
                fpInst.timeOfDelete = fakePlaneInfo.timeOfDelete;
            }
            else if (replayType == 1) //timeOfDelete for optimal delete is set in CreatePlanes() here
            {
                    fpInst.timeOfDelete = replaySessions[curReplaySession].optimalDeletes.Contains(fakePlaneInfo.planeID) ? 0.5f : 99999f;
            }
            else if (replayType == 2)
            {
                fpInst.timeOfDelete = 99999f;
            }

            fakePlanesGameObjects.Add(fplane);
        }
    }


    private void ClearFakePlanes()
    {
        if (curReplaySession < 0 || curReplaySession >= replaySessions.Count)
            return;

        foreach (GameObject g in fakePlanesGameObjects)
            Destroy(g);
        fakePlanesGameObjects.Clear();
    }

    #endregion
}
