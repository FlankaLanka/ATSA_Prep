using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class HoverRevealSpatial : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image image;
    private Color originalColor;

    [SerializeField] private Color hoverColor = Color.yellow; // Change to desired hover color

    private void Start()
    {
        image = GetComponent<Image>();
        if (image != null)
        {
            originalColor = image.color;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (image != null)
        {
            image.color = hoverColor;
        }
        foreach(TMP_Text text in transform.GetComponentsInChildren<TMP_Text>())
        {
            text.color = Color.magenta;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (image != null)
        {
            image.color = originalColor;
        }

        foreach (TMP_Text text in transform.GetComponentsInChildren<TMP_Text>())
        {
            text.color = Color.yellow;
        }
    }
}
