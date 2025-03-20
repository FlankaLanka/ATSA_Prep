using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class HoverRevealCollisions : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    private Image rowBackground;
    private Color originalColor;
    private Color hoverColor = Color.yellow;
    private SimulatorManager sm;
    private ReplaySystemTab replaySystem;

    private void Awake()
    {
        sm = FindFirstObjectByType<SimulatorManager>();
        replaySystem = FindFirstObjectByType<ReplaySystemTab>(findObjectsInactive: FindObjectsInactive.Include);
        if (sm == null || replaySystem == null)
            Debug.Log("Required SimulatorManager and CollisionSessionVideoImage for advanced stats to exist. Destroying this.");


        rowBackground = GetComponent<Image>();
        originalColor = rowBackground.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        rowBackground.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rowBackground.color = originalColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        TMP_Text sessionText = gameObject.GetComponentInChildren<TMP_Text>();
        int sessionNum = 1;
        int.TryParse(sessionText.text, out sessionNum);
        sm.curReplaySession = sessionNum - 1;

        replaySystem.gameObject.SetActive(true);
    }
}
