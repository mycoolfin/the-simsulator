using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// TODO: New paradigm - FocusGrid individuals will use a separate world. "Frames", "capsules" etc. should all use the same methodology.

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.TwoD
{
    using Evolution;
    using UI.Camera;

    public class FocusGrid : MonoBehaviour
    {
        public VisualTreeAsset focusedPhenotypeFrameAsset;
        // public GameObject followCameraPrefab;
        public int numVisibleFrames;
        public const int maxFrames = 12;

        private List<FocusFrame> frames;

        private const string phenotypeLayer = "Phenotype";
        private const string bestIndividualLayer = "Best Individual";

        private void Start()
        {
            VisualElement grid = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("grid");
            frames = new();
            // TODO
            // for (int i = 0; i < maxFrames; i++)
            //     frames.Add(new(transform, grid, focusedPhenotypeFrameAsset, followCameraPrefab));
        }

        private void Update()
        {
            for (int i = 0; i < frames.Count; i++)
            {
                frames[i].SetActive(i < numVisibleFrames);
            }
        }

        public void SetFrameTargets(List<IAssessableCreature> focusedIndividuals)
        {
            for (int i = 0; i < frames.Count; i++)
            {
                if (i < focusedIndividuals.Count)
                {
                    // TODO: New paradigm.
                    // // Unset old target layer.
                    // if (frames[i].focusedIndividual?.Phenotype != null)
                    //     frames[i].focusedIndividual.Phenotype.SetLayer(phenotypeLayer);

                    // string layerName = bestIndividualLayer + " " + (i + 1);
                    // if (focusedIndividuals[i].Phenotype != null)
                    //     focusedIndividuals[i].Phenotype.SetLayer(layerName);
                    // frames[i].SetTarget(focusedIndividuals[i], layerName, "#" + (i + 1) + ": ");
                }
            }
        }

        public FollowCamera GetBestIndividualCamera()
        {
            if (frames.Count == 0)
                return null;
            return frames[0].followCamera;
        }
    }
}
