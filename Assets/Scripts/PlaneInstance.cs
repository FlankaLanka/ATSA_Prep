using UnityEngine;
using TMPro;

public class PlaneInstance : MonoBehaviour
{
    public SimulatorManager sm;

    public SpriteRenderer sp;
    public int planeID;
    public KeyCode keyID;
    public TMP_Text numberText;
    public Vector2 direction;
    public float speed;
    public bool collided = false;


    private void Start()
    {
        sm = FindFirstObjectByType<SimulatorManager>();

        sp = GetComponent<SpriteRenderer>();

        keyID = KeyCode.Keypad0 + planeID;
        numberText = GetComponentInChildren<TMP_Text>();
        if (numberText != null)
            numberText.text = planeID.ToString();

        direction = GetRandomUnitVector2D();
        speed = Random.Range(0.5f, 3f);
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
