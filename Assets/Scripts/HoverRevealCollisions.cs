using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverRevealCollisions : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image rowBackground;
    private Color originalColor;
    private Color hoverColor = Color.yellow;
    private SimulatorManager sm;
    private CollisionSessionVideoImage sv;
    private Image videoImage;


    private void Awake()
    {
        sm = FindFirstObjectByType<SimulatorManager>();
        sv = FindFirstObjectByType<CollisionSessionVideoImage>();
        if (sm == null || sv == null)
            Debug.Log("Required SimulatorManager and CollisionSessionVideoImage for advanced stats to exist. Destroying this.");

        videoImage = sv.gameObject.GetComponent<Image>();
        if (videoImage == null)
            Debug.Log("Required CollisionSessionVideoImage to contain Image. Destroying this.");

        rowBackground = GetComponent<Image>();
        originalColor = rowBackground.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        rowBackground.color = hoverColor;
        videoImage.color = new Color(1, 1, 1, 1);

        //if (sm.questionSnapshots.ContainsKey(gameObject))
        //{
        //    snapshotImage.sprite = sm.questionSnapshots[gameObject];
        //    snapshotImage.color = new Color(1, 1, 1, 1);
        //}
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rowBackground.color = originalColor;
        videoImage.color = new Color(1, 1, 1, 0);
    }
}
