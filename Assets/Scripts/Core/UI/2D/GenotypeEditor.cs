using System;
using UnityEngine.UIElements;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.UI.TwoD
{
    using TheSimsulator.Core.Genotype;

    public abstract class GenotypeEditor<TGenotype>
        where TGenotype : IGenotype<TGenotype>
    {
        protected readonly VisualElement root;
        protected readonly Action<TGenotype> onGenotypeChanged;

        public GenotypeEditor(UIDocument uiDocument, Action<TGenotype> onGenotypeChanged)
        {
            root = uiDocument.rootVisualElement;
            this.onGenotypeChanged = onGenotypeChanged;
        }

        public abstract void LoadGenotype(TGenotype genotype);
    }
}
