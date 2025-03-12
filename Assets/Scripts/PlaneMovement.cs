using UnityEngine;

public class PlaneMovement : MonoBehaviour
{
    public SimulatorManager sm;

    public Vector2 direction;
    public float speed;

    private SpriteRenderer sp;

    private void Start()
    {
        sp = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        sp.color = Color.red;
    }
}
