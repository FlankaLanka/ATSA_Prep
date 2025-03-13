using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlaneInstance : MonoBehaviour
{
    private SimulatorManager sm;
    private SpriteRenderer bg;
    private RectMask2D canvasMask;
    private SpriteRenderer sp;

    public int planeID;
    private KeyCode keyID;
    private TMP_Text numberText;
    public Vector2 direction;
    public float speed;
    private bool collided = false;

    public float randAngleThreshold = 70f;

    private void Start()
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

        keyID = KeyCode.Keypad0 + planeID;
        numberText = GetComponentInChildren<TMP_Text>();
        if (numberText != null)
            numberText.text = planeID.ToString();

        direction = Math2DHelpers.GetRandomUnitVectorWithinAngle(bg.transform.position - transform.position, randAngleThreshold);
        speed = Random.Range(1f, 2.5f);
        speed = 0;
    }

    // Update is called once per frame
    void Update()
    {
        //move plane
        transform.position += speed * Time.deltaTime * (Vector3)direction;

        //check user input
        if(Input.GetKeyDown(keyID) && !collided)
        {
            gameObject.SetActive(false);
        }

        //update sprite mask (for canvas rect mask, spriteRenderer sprite mask is done with bg)
        UpdateCanvasSpriteMask();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collided)
        {
            collided = true;
            sp.color = Color.red;
        }
    }

    private void UpdateCanvasSpriteMask()
    {
        if (canvasMask == null)
            return;

        //the canvas is a square so height and width are same
        float widthLen = sp.bounds.max.x - sp.bounds.min.x;
        float dist = Math2DHelpers.SignedDistanceFromPointToLine(transform.position, bg.bounds.max, new Vector2(1,0));
        Debug.Log(dist);
        dist = Mathf.Clamp(dist, -widthLen, widthLen);
        float normalizedPadding = Math2DHelpers.NormalizeValue(dist / widthLen, 0.5f, 1f);

        Vector4 newPadding = canvasMask.padding;
        newPadding.w = normalizedPadding;
        canvasMask.padding = newPadding;
    }

    //private float ClampVectorValue()
    //{

    //}
}
