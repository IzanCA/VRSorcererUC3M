// RandomSpellGenerator.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RandomSpellGenerator : MonoBehaviour
{
    [Header("Spell Settings")]
    public string[] spellNames = { "Purple", "Fire", "Water", "Electricity", "Red" };

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI spellNameText;
    [SerializeField] private Button skipButton;
    [SerializeField] private Slider progressSlider;

    [Header("Timing")]
    [SerializeField] private float holdTimeRequired = 3f;
    [SerializeField] private TMP_InputField holdtimeInputField;



    [Header("Dependencies")]
    [SerializeField] private SaveInformation saveInformation;
    [SerializeField] private HandPoseEvaluator[] handPoseEvaluatorsRight; // 5 evaluadores Right
    [SerializeField] private HandPoseEvaluator[] handPoseEvaluatorsLeft;  // 5 evaluadores Left
    [SerializeField] private ChangeHandManager changeHandManager;

    [Header("State")]
    public int currentSpell;
    public bool spellCompleted = false;
    public bool skipEnabled = true;

    private float currentHoldTime = 0f;
    private bool correctGestureActive = false;

    void Start()
    {
        if (skipButton != null)
            skipButton.onClick.AddListener(SkipSpell);

        GenerateRandomSpell();

        holdtimeInputField.onEndEdit.AddListener(value =>
        {
            if (float.TryParse(value, out float result))
                holdTimeRequired = result;
        });
    }

    void Update()
    {
        if (spellCompleted) return;

        if (correctGestureActive)
        {
            currentHoldTime += Time.deltaTime;

            if (progressSlider != null)
                progressSlider.value = currentHoldTime / holdTimeRequired;

            if (progressSlider != null)
            {
                var fill = progressSlider.fillRect.GetComponent<Image>();
                fill.color = Color.Lerp(Color.red, Color.green, currentHoldTime / holdTimeRequired);
            }

            if (currentHoldTime >= holdTimeRequired)
                CompleteSpell();
        }
        else
        {
            ResetProgress();
        }

        if (Input.GetKeyDown(KeyCode.S))
            SkipSpell();

        
    }

    private void GenerateRandomSpell()
    {
        currentSpell = Random.Range(1, spellNames.Length + 1);
        spellCompleted = false;

        Debug.Log("Hechizo actual: " + GetCurrentSpellName());

        if (spellNameText != null)
            spellNameText.text = GetCurrentSpellName();

        ResetProgress();
    }

    private void CompleteSpell()
    {
        spellCompleted = true;

        // Recoge la similitud del evaluador correspondiente al hechizo actual
        float similarity = GetCurrentSimilarity();
        saveInformation?.OnSpellCompleted(GetCurrentSpellName(), similarity);

        Debug.Log($"Hechizo completado: {GetCurrentSpellName()} con {similarity * 100f:F1}%");

        ResetProgress();
        GenerateRandomSpell();
    }

    private void ResetProgress()
    {
        currentHoldTime = 0f;
        correctGestureActive = false;

        if (progressSlider != null)
            progressSlider.value = 0f;
    }

    // Devuelve la similitud del evaluador activo según la mano seleccionada
    private float GetCurrentSimilarity()
    {
        int index = currentSpell - 1;

        // 0 = Right, 1 = Left, 2 = Both
        int handMode = changeHandManager != null ? changeHandManager.CurrentHandMode : 0;

        if (handMode == 1 && handPoseEvaluatorsLeft != null && index < handPoseEvaluatorsLeft.Length)
            return handPoseEvaluatorsLeft[index].similarity;

        if (handPoseEvaluatorsRight != null && index < handPoseEvaluatorsRight.Length)
            return handPoseEvaluatorsRight[index].similarity;

        return 0f;
    }

    public void OnGestureDetected(int spellIndex, bool isActive)
    {
        if (spellCompleted) return;
        if (spellIndex != currentSpell)
        {
            correctGestureActive = false;
            return;
        }
        correctGestureActive = isActive;
    }

    public void StartGesture(int spellIndex) => OnGestureDetected(spellIndex, true);
    public void EndGesture(int spellIndex) => OnGestureDetected(spellIndex, false);

    public void SkipSpell()
    {
        if (!skipEnabled) return;
        saveInformation?.OnSpellSkipped(GetCurrentSpellName());
        Debug.Log("Skipped: " + GetCurrentSpellName());
        GenerateRandomSpell();
    }

    public string GetCurrentSpellName()
    {
        if (currentSpell >= 1 && currentSpell <= spellNames.Length)
            return spellNames[currentSpell - 1];
        return "Unknown";
    }
}