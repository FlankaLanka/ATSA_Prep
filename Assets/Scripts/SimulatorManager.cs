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
        public float zeroPressedTimer = 0f; //updated in Update()
        public float roundTimer = 0f; //updated in Update()
        public bool freezeDeletion = false; //check Update()
        public bool wasGoodFreeze = true; //updated in PlaneInstance.cs

        public Dictionary<GameObject, List<GameObject>> collisionsGraph; //updated at start of session when CreatePlanes()
        public List<int> optimalDeletes = new(); //updated at start of session when CreatePlanes()
        public List<(int, int)> potentialCollisions = new(); //updated at start of session when CreatePlanes()
        public List<int> actualDeletes = new(); //updated in PlaneInstance.cs
        public List<(int, int)> actualCollisions = new(); //updated in PlaneInstance.cs

        public List<FakePlaneInfoStats> currentPlanesInfo = new(); //updated at start of session when CreatePlanes()
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
    public ReplaySystemTab replaySystemTab;

    public GameObject settingsMenu;

    [Header("Game Loop Related")]
    public int curSession, totalSessions;
    public bool gameRunning, instanceRunning;

    [Header("Game Logic Related")]
    public GameObject planePrefab;
    public SpriteRenderer bg;
    public List<GameObject> allPlanes;
    [Range(1f,5f)]
    public float distanceSpawnThreshold = 0.3f;

    public SimMathManager mathManager;

    [Header("Stats")]
    public TMP_Text statsText;
    public TMP_Text statsMathText;
    public TMP_Text advancedStatsText;
    public TMP_Text advancedStatsMathText;
    public Transform statsGroup;
    public GameObject rowStatPrefab;

    [Header("For visual replay system")]
    public List<ReplaySessionInfoStats> replaySessions = new();
    public List<GameObject> fakePlanesGameObjects = new(); //stores the physical fake planes on screen, use this to destroy
    public int curReplaySession = 0;


    public void StartSimulator()
    {
        settingsMenu.SetActive(false);
        mathManager.gameObject.SetActive(mathQuestionsToggle.isOn);

        curSession = -1;
        int.TryParse(numSessionsDropdown.options[numSessionsDropdown.value].text, out totalSessions);
        instanceRunning = false;

        ResetAdvancedStats();
        mathManager.ResetAdvancedStats();

        gameRunning = true;
    }

    public void StopSimulator()
    {
        ClearPlanes();

        statsText.text = LogGeneralStatsCollision(false);
        advancedStatsText.text = LogGeneralStatsCollision(true);

        if (mathQuestionsToggle.isOn)
        {
            statsMathText.text = $"You got {mathManager.score} / {mathManager.totalAttempted} correct out of {mathManager.total} math questions.";
            advancedStatsMathText.text = $"You got {mathManager.score} / {mathManager.totalAttempted} correct out of {mathManager.total} math questions.";
        }
        else
        {
            statsMathText.text = "No math questions this session.";
            advancedStatsMathText.text = "No math questions this session.";
        }

        mathManager.gameObject.SetActive(false);
        settingsMenu.SetActive(true);

        frozenText.gameObject.SetActive(false);

        gameRunning = false;
    }

    private void OnEnable()
    {
        replaySystemTab.OnTargetDisabled += ClearPlanes;
    }

    private void OnDisable()
    {
        replaySystemTab.OnTargetDisabled -= ClearPlanes;
    }

    void Update()
    {
        if (!gameRunning)
            return;

        if(curSession >= totalSessions || Input.GetKeyDown(KeyCode.Escape))
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                int sessionNum = Mathf.Min(curSession, totalSessions - 1);
                UpdateAdvancedStats(sessionNum);
            }

            StopSimulator();
            return;
        }

        if (!instanceRunning) // each instance refers to one wave of planes spawning and flying across
        {
            curSession++;
            ClearPlanes();
            CreatePlanes();
            frozenText.gameObject.SetActive(false);

            instanceRunning = true;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Keypad0) && !replaySessions[curSession].freezeDeletion)
        {
            replaySessions[curSession].freezeDeletion = true;
            frozenText.gameObject.SetActive(true);
        }
        if (!replaySessions[curSession].freezeDeletion)
            replaySessions[curSession].zeroPressedTimer += Time.deltaTime;
        replaySessions[curSession].roundTimer += Time.deltaTime;

        if (AllPlanesInactive())
        {
            UpdateAdvancedStats(curSession);
            instanceRunning = false;
            return;
        }
    }

    public void ClearPlanes()
    {
        foreach (GameObject g in allPlanes)
        {
            Destroy(g);
        }
        allPlanes.Clear();
    }

    public void CreatePlanes()
    {
        replaySessions.Add(new ReplaySessionInfoStats());

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
        replaySessions[curSession].collisionsGraph = CalculateCollisionsMap(allPlanes);
        replaySessions[curSession].optimalDeletes = GetOptimalDeletions(replaySessions[curSession].collisionsGraph);
        replaySessions[curSession].potentialCollisions = GetPossibleCollisions(replaySessions[curSession].collisionsGraph);
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


    public void UpdateAdvancedStats(int curSession)
    {
        //Q: when do you update advancedstats? A: when round ends or when esc pressed

        GameObject g = Instantiate(rowStatPrefab, statsGroup);
        TMP_Text[] roundStatText = g.GetComponentsInChildren<TMP_Text>();

        if (roundStatText.Length != 6)
        {
            Debug.LogWarning("AdvancedStats cannot be displayed properly. See calling method.");
            return;
        }

        //assign variables for readability
        bool freezeDeletion = replaySessions[curSession].freezeDeletion;
        float zeroPressedTimer = replaySessions[curSession].zeroPressedTimer;
        float roundTimer = replaySessions[curSession].roundTimer;
        Dictionary<GameObject, List<GameObject>> collisionsGraph = replaySessions[curSession].collisionsGraph;
        List<int> optimalDeletes = replaySessions[curSession].optimalDeletes;
        List<int> inputDeletes = replaySessions[curSession].actualDeletes;
        List<(int, int)> actualCollisions = replaySessions[curSession].actualCollisions;

        //for later replay use
        foreach (FakePlaneInfoStats fakePlaneInfo in replaySessions[curSession].currentPlanesInfo)
        {
            //check amongst time when input-deleted vs roundtime for life cycle
            fakePlaneInfo.timeOfDelete = Mathf.Min(fakePlaneInfo.timeOfDelete, replaySessions[curSession].roundTimer);
        }

        //fill in text
        roundStatText[0].text = (curSession + 1).ToString();
        roundStatText[1].text = LogPossibleCollisions(collisionsGraph);
        roundStatText[2].text = "";
        foreach ((int, int) collidedPair in actualCollisions)
        {
            if (collidedPair.Item1 < collidedPair.Item2) //ensure that we only write to text once per pair
            {
                roundStatText[2].text += $"[{collidedPair.Item1},{collidedPair.Item2}] ";
            }
        }
        roundStatText[3].text = "";
        foreach (int d in optimalDeletes)
        {
            roundStatText[3].text += d + ", ";
        }
        roundStatText[4].text = "";
        foreach (int input in inputDeletes)
        {
            roundStatText[4].text += input + ", ";
        }
        roundStatText[5].text = (freezeDeletion ? zeroPressedTimer.ToString("F3") + "s" : "Not pressed") + " / " + roundTimer.ToString("F3") + "s";
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

    public List<(int, int)> GetPossibleCollisions(Dictionary<GameObject, List<GameObject>> graph)
    {
        HashSet<(int, int)> reportedConnections = new();

        foreach (var kvp in graph)
        {
            int nodeX = kvp.Key.GetComponent<PlaneInstance>().planeID;
            foreach (GameObject connectedNode in kvp.Value)
            {
                int nodeY = connectedNode.GetComponent<PlaneInstance>().planeID;
                (int, int) edge = (nodeX, nodeY);
                (int, int) reverseEdge = (nodeY, nodeX);

                if (!reportedConnections.Contains(reverseEdge))
                {
                    reportedConnections.Add(edge);
                }
            }
        }

        return reportedConnections.ToList();
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
        //Debug.Log(collisionPoint);

        // Check if the collision point is within bounds
        if (bounds.Contains(new Vector3(collisionPoint.x, collisionPoint.y, 0)))
            return collisionPoint;

        return null; // Collision occurs outside the bounding box
    }


    private List<int> GetOptimalDeletions(Dictionary<GameObject, List<GameObject>> collisionsGraph)
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
            List<GameObject> isolatedNodes = graph.Where(kvp => kvp.Value.Count == 0).Select(kvp => kvp.Key).ToList();
            foreach (var node in isolatedNodes)
            {
                graph.Remove(node);
            }
        }

        List<int> toDeleteInt = toDelete.Select(plane => plane.GetComponent<PlaneInstance>().planeID).ToList();

        return toDeleteInt;
    }


    public string LogGeneralStatsCollision(bool advanced)
    {
        int totalPotentialCollisions = 0;
        int totalActualCollisions = 0;

        int minDeletionsRequired = 0;
        int excessDeletions = 0;
        int actualDeletions = 0;

        foreach (ReplaySessionInfoStats session in replaySessions)
        {
            totalPotentialCollisions += session.potentialCollisions.Count;
            totalActualCollisions += session.actualCollisions.Count;

            minDeletionsRequired += session.optimalDeletes.Count;
            excessDeletions += Mathf.Max(session.actualDeletes.Count - session.optimalDeletes.Count, 0);
            actualDeletions += session.actualDeletes.Count;
        }

        float collisionsScore = totalPotentialCollisions == 0 ? 100 : (float)totalActualCollisions / totalPotentialCollisions * 100;
        float deletionScore = minDeletionsRequired == 0 ? 100 : (float)minDeletionsRequired / actualDeletions * 100;

        if(advanced)
        {
            return $"You had {totalActualCollisions}/{totalPotentialCollisions} possible collisions. Score: {collisionsScore:F2}. " +
                   $"You removed {actualDeletions} planes. You removed {excessDeletions} more than the optimal solution." +
                   $"Score: (IDK, still figuring out the math lol). " +
                   $"<color=yellow>Hightlight and click a round to see your replay.</color>.";
        }
        else
        {
            return $"You had {totalActualCollisions}/{totalPotentialCollisions} possible collisions. Score: {((float)totalActualCollisions / totalPotentialCollisions):F2}.";
        }

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

        if (curReplaySession < 0 || curReplaySession >= replaySessions.Count)
            return;

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
                fpInst.timeOfDelete = replaySessions[curReplaySession].optimalDeletes.Contains(fakePlaneInfo.planeID) ? 0.25f : 99999f;
            }
            else if (replayType == 2)
            {
                fpInst.timeOfDelete = 99999f;
            }

            fakePlanesGameObjects.Add(fplane);
        }
    }

    public void ClearFakePlanes()
    {
        foreach (GameObject g in fakePlanesGameObjects)
            Destroy(g);
        fakePlanesGameObjects.Clear();
    }

    #endregion
}
