using UnityEngine;
using TMPro;

public class PlaneInstance : MonoBehaviour
{
    private SimulatorManager sm;
    private SpriteRenderer bg;
    private SpriteRenderer sp;

    public int planeID;
    private KeyCode keyID;
    private TMP_Text numberText;
    private Vector2 direction;
    private float speed;
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

        sp = GetComponent<SpriteRenderer>();

        keyID = KeyCode.Keypad0 + planeID;
        numberText = GetComponentInChildren<TMP_Text>();
        if (numberText != null)
            numberText.text = planeID.ToString();

        direction = Math2DHelpers.GetRandomUnitVectorWithinAngle(bg.transform.position - transform.position, randAngleThreshold);
        speed = Random.Range(1f, 2.5f);
    }

    // Update is called once per frame
    void Update()
    {
        //move plane
        transform.position += speed * Time.deltaTime * (Vector3)direction;

        //check user input
        if(Input.GetKeyDown(keyID) && !collided)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        collided = true;
        sp.color = Color.red;
    }

    public Vector2 GetRandomUnitVector2D()
    {
        float angle = Random.Range(0f, Mathf.PI * 2);
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }
}
