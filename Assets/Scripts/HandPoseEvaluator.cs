using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Gestures;
using UnityEngine.UI;
using TMPro;

public class HandPoseEvaluator : MonoBehaviour
{
    [SerializeField] private XRHandShape targetHandShape;
    [SerializeField] private XRHandTrackingEvents handTrackingEvents;

    [SerializeField] private float similarityPrecision;

    [Range(0f, 1f)]
    public float similarity;

    private HandPoseEvaluator evaluator;
    [SerializeField] private Slider slider;


    [SerializeField] private TextMeshProUGUI similarityText;
    [SerializeField] private Slider thresholdSlider;

    void OnEnable() => handTrackingEvents.jointsUpdated.AddListener(OnJointsUpdated);
    void OnDisable() => handTrackingEvents.jointsUpdated.RemoveListener(OnJointsUpdated);


    void Awake()
    {
        evaluator = this;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.interactable = false;
    }

    void Update()
    {
        slider.value = evaluator.similarity;
        similarityText.text = $"{(evaluator.similarity * 100f):F1}%";
        var fill = slider.fillRect.GetComponent<Image>();
        fill.color = Color.Lerp(Color.red, Color.green, evaluator.similarity);
        similarityPrecision = thresholdSlider.value;
    }

    void OnJointsUpdated(XRHandJointsUpdatedEventArgs eventArgs)
    {
        if (!eventArgs.hand.isTracked || targetHandShape == null)
        {
            similarity = 0f;
            return;
        }

        float totalSimilarity = 0f;
        int totalTargets = 0;

        foreach (var condition in targetHandShape.fingerShapeConditions)
        {
            // Calcula todos los tipos de shape que necesita esta condición
            var typesNeeded = XRFingerShapeTypes.None;
            foreach (var target in condition.targets)
                typesNeeded |= (XRFingerShapeTypes)(1 << (int)target.shapeType);

            if (typesNeeded == XRFingerShapeTypes.None) continue;

            var fingerShape = eventArgs.hand.CalculateFingerShape(condition.fingerID, typesNeeded);

            foreach (var target in condition.targets)
            {
                float value;
                bool hasValue;

                switch (target.shapeType)
                {
                    case XRFingerShapeType.FullCurl:
                        hasValue = fingerShape.TryGetFullCurl(out value); break;
                    case XRFingerShapeType.BaseCurl:
                        hasValue = fingerShape.TryGetBaseCurl(out value); break;
                    case XRFingerShapeType.TipCurl:
                        hasValue = fingerShape.TryGetTipCurl(out value); break;
                    case XRFingerShapeType.Pinch:
                        hasValue = fingerShape.TryGetPinch(out value); break;
                    case XRFingerShapeType.Spread:
                        hasValue = fingerShape.TryGetSpread(out value); break;
                    default:
                        continue;
                }

                if (!hasValue) continue;

                float diff = Mathf.Abs(value - target.desired);

                // Usamos una zona de precision fija, ignorando el threshold
                // 0.15f = tienes que estar a menos de 0.15 del desired para llegar al 100%
                float precisionZone = similarityPrecision;
                float fingerSimilarity = 1f - Mathf.Clamp01(diff / precisionZone);

                totalSimilarity += fingerSimilarity;
                totalTargets++;
            }
        }

        similarity = totalTargets > 0 ? totalSimilarity / totalTargets : 0f;
    }
}