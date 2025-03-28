using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlaneInstance : MonoBehaviour
{
    private SimulatorManager sm;
    private SpriteRenderer bg;
    private RectMask2D canvasMask;
    private SpriteRenderer sp;

    public float randAngleThreshold = 70f;

    public int planeID;
    private KeyCode keyID;
    private TMP_Text numberText;
    public Vector2 direction;
    public float speed;

    public bool isFake = false; //used for replay
    public float timer = 0f, timeOfDelete = 99999f; //timeOfDelete is actually only referenced by case 0 of InitiateReplay(int replayType) in SimulatorManager.cs
    public SimulatorManager.FakePlaneInfoStats fakePlaneReference;

    //TODO: use contructor to assign values instead of manual inputs in SimulatorManager.cs
    //public PlaneInstance(bool fake)
    //{
    //}

    private void Awake()
    {
        sm = FindFirstObjectByType<SimulatorManager>();
        if (sm == null)
        {
            Debug.LogWarning("Cannot run plane without SimulatorManager, destroying this.");
            Destroy(gameObject);
        }
        bg = sm.bg;

        canvasMask = GetComponentInChildren<RectMask2D>();
        sp = GetComponent<SpriteRenderer>();

        if(!isFake)
        {
            direction = Math2DHelpers.GetRandomUnitVectorWithinAngle(bg.transform.position - transform.position, randAngleThreshold);
            speed = GetRandomPlaneSpeed(sm.difficultySlider.value);
        }

    }

    private void Start()
    {
        keyID = KeyCode.Keypad0 + planeID;
        numberText = GetComponentInChildren<TMP_Text>();
        numberText.text = planeID.ToString();

        if(isFake)
        {
            //render invisible to main camera
            SetLayerRecursively(gameObject, "FakePlanes");
        }
    }

    // Update is called once per frame
    void Update()
    {
        //move plane
        transform.position += speed * Time.deltaTime * (Vector3)direction;

        if(!isFake)
        {
            //check user input
            if (Input.GetKeyDown(keyID) && !sm.replaySessions[sm.curSession].freezeDeletion)
            {
                sm.replaySessions[sm.curSession].actualDeletes.Add(planeID);
                gameObject.SetActive(false);
                fakePlaneReference.timeOfDelete = timer;
            }
        }
        else
        {
            if(timer >= timeOfDelete)
            {
                gameObject.SetActive(false);
            }
        }
        timer += Time.deltaTime;

        //update sprite mask (for canvas rect mask, spriteRenderer sprite mask is done with bg)
        UpdateCanvasSpriteMask();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlaneInstance otherPlaneInst = collision.GetComponent<PlaneInstance>();
        if (otherPlaneInst != null && bg.bounds.Contains(collision.ClosestPoint(transform.position)))
        {
            sp.color = Color.red;

            if(isFake)
                return;

            sm.replaySessions[sm.curSession].actualCollisions.Add((planeID, otherPlaneInst.planeID));
            if (sm.replaySessions[sm.curSession].freezeDeletion)
                sm.replaySessions[sm.curSession].wasGoodFreeze = false;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        //this is the condition for checking when to start a new session of planes, see SimulatorManager
        if (collision.gameObject.GetComponent<SimulatorBackground>() != null)
        {
            gameObject.SetActive(false);
        }
    }

    private void UpdateCanvasSpriteMask()
    {
        if (canvasMask == null)
            return;

        //the canvas is a square so height and width are same
        float widthLen = sp.bounds.max.x - sp.bounds.min.x;

        //top padding
        float topDist = Math2DHelpers.SignedDistanceFromPointToLine(transform.position, bg.bounds.max, new Vector2(1, 0));
        topDist = Mathf.Clamp(topDist + widthLen / 2, 0, widthLen);
        topDist = Math2DHelpers.NormalizeValue(topDist / widthLen, 0f, 1f);

        //bottom padding
        float botDist = Math2DHelpers.SignedDistanceFromPointToLine(transform.position, bg.bounds.min, new Vector2(-1, 0));
        botDist = Mathf.Clamp(botDist + widthLen / 2, 0, widthLen);
        botDist = Math2DHelpers.NormalizeValue(botDist / widthLen, 0f, 1f);

        //left padding
        float leftDist = Math2DHelpers.SignedDistanceFromPointToLine(transform.position, bg.bounds.min, new Vector2(0, 1));
        leftDist = Mathf.Clamp(leftDist + widthLen / 2, 0, widthLen);
        leftDist = Math2DHelpers.NormalizeValue(leftDist / widthLen, 0f, 1f);

        //right padding
        float rightDist = Math2DHelpers.SignedDistanceFromPointToLine(transform.position, bg.bounds.max, new Vector2(0, -1));
        rightDist = Mathf.Clamp(rightDist + widthLen / 2, 0, widthLen);
        rightDist = Math2DHelpers.NormalizeValue(rightDist / widthLen, 0f, 1f);

        //xyzw = lbrt
        canvasMask.padding = new Vector4(leftDist, botDist, rightDist, topDist);
    }

    private float GetRandomPlaneSpeed(float difficulty)
    {
        float minspeed = 0.5f, maxspeed = 1.25f;
        minspeed += difficulty * .25f;
        maxspeed += difficulty * .25f;
        return Random.Range(minspeed, maxspeed);
    }

    void SetLayerRecursively(GameObject obj, string newLayer)
    {
        obj.layer = LayerMask.NameToLayer(newLayer);
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}

