using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class FocusGrid : MonoBehaviour
{
    public VisualTreeAsset focusedPhenotypeFrameAsset;
    public GameObject followCameraPrefab;
    public EvolutionSimulator simulator;

    private List<FocusFrame> frames;
    private VisualElement grid;

    private const string phenotypeLayer = "Phenotype";
    private const string bestIndividualLayer = "Best Individual";

    private const int numFrames = 12;

    private void Start()
    {
        grid = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("grid");

        frames = new();
        for (int i = 0; i < numFrames; i++)
            frames.Add(new(transform, grid, focusedPhenotypeFrameAsset, followCameraPrefab, simulator));

        simulator.OnIterationStart += () =>
        {
            List<Individual> bestIndividuals = simulator.GetTopIndividuals(numFrames);
            for (int i = 0; i < frames.Count; i++)
            {
                if (i < bestIndividuals.Count)
                {
                    string layerName = bestIndividualLayer + " " + (i + 1);
                    if (bestIndividuals[i].phenotype != null)
                        bestIndividuals[i].phenotype.SetLayer(layerName);
                    frames[i].SetTarget(bestIndividuals[i], layerName, "#" + (i + 1) + ": ");
                }
                frames[i].SetActive(i < simulator.focusBestCreatures);
            }
        };
    }

    private void Update()
    {
        for (int i = 0; i < frames.Count; i++)
        {
            frames[i].SetActive(i < simulator.focusBestCreatures);
        }
    }
}
