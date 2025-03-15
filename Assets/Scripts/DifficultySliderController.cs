using UnityEngine;
using UnityEngine.UI;

public class DifficultySliderController : MonoBehaviour
{
    private Slider difficultySlider;
    private Text difficultyText;

    private void Awake()
    {
        difficultySlider = GetComponent<Slider>();
        difficultyText = GetComponentInChildren<Text>();

        if (difficultySlider == null || difficultyText == null)
        {
            Debug.LogWarning("Missing components slider and text, destroying this.");
            Destroy(this);
        }
    }

    private void Update()
    {
        difficultyText.text = $"Difficulty({difficultySlider.value}/10)";
    }
}
