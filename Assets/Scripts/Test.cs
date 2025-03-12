using UnityEngine;

public class Test : MonoBehaviour
{
    public Transform c1;
    public Transform c2;

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.A))
        {
            Collider2D c = Physics2D.OverlapCircle(c1.position, 0.5f);
            Debug.Log(c);
        }
    }
}
