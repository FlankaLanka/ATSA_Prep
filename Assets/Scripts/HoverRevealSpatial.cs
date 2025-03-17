using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class HoverRevealSpatial : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image rowBackground;
    private Color originalColor;
    private Color hoverColor = Color.yellow;
    private SpatialManager sm;
    private SpatialSnapshotImage si;
    private Image snapshotImage;


    private void Awake()
    {
        sm = FindFirstObjectByType<SpatialManager>();
        si = FindFirstObjectByType<SpatialSnapshotImage>();
        if(sm == null || si == null)
            Debug.Log("Required SpatialManager and SpatialSnapshotImage for advanced stats to exist. Destroying this.");

        snapshotImage = si.gameObject.GetComponent<Image>();
        if(snapshotImage == null)
            Debug.Log("Required SpatialSnapshotImage to contain Image. Destroying this.");

        rowBackground = GetComponent<Image>();
        originalColor = rowBackground.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        rowBackground.color = hoverColor;

        if (sm.questionSnapshots.ContainsKey(gameObject))
        {
            snapshotImage.sprite = sm.questionSnapshots[gameObject];
            snapshotImage.color = new Color(1, 1, 1, 1);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rowBackground.color = originalColor;
        snapshotImage.color = new Color(1, 1, 1, 0);
    }
}
