using UnityEngine;
using mycoolfin.TheSimsulator.Sims.Phenotype;

public class PhenotypeRenderer : MonoBehaviour
{
    private SimsPhenotype Phenotype;

    private void Start()
    {
        if (Phenotype == null)
        {
            Debug.LogError("Phenotype is not assigned.");
            return;
        }
    }

    public void Initialise(SimsPhenotype phenotype, GameObject limbRendererPrefab, GameObject jointRendererPrefab)
    {
        Phenotype = phenotype;
        foreach (mycoolfin.TheSimsulator.Sims.Phenotype.Limb limb in Phenotype.Limbs)
        {
            LimbRenderer limbRenderer = Instantiate(limbRendererPrefab, transform).GetComponent<LimbRenderer>();
            limbRenderer.Initialise(limb, jointRendererPrefab);
        }
    }
}
