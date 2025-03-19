using UnityEngine;

public class SimulatorOrMathStatsToggler : MonoBehaviour
{
    public GameObject collisionStats;
    public GameObject mathStats;

    public void ToggleStats()
    {
        if(collisionStats.activeInHierarchy)
        {
            collisionStats.SetActive(false);
            mathStats.SetActive(true);
        }
        else
        {
            collisionStats.SetActive(true);
            mathStats.SetActive(false);
        }
    }
}
