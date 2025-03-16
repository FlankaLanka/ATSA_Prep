using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DifferencesAdvancedStats : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text generalStatsText;
    public GameObject rowStatPrefab;

    public DifferencesManager diffManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    #region ButtonUtil

    public void EnableAdvStats()
    {
        gameObject.SetActive(true);
    }

    public void DisableAdvStats()
    {
        gameObject.SetActive(false);
    }

    #endregion
}
