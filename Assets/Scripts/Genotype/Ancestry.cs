using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

[Serializable]
public class OffspringSpecification
{
    [SerializeField] private RecombinationOperation recombinationOperation;
    [SerializeField] private List<MutationOperation> mutationOperations;

    public RecombinationOperation RecombinationOperation => recombinationOperation;
    public ReadOnlyCollection<MutationOperation> MutationOperations => mutationOperations.AsReadOnly();

    public OffspringSpecification(RecombinationOperation recombinationOperation, List<MutationOperation> mutationOperations)
    {
        this.recombinationOperation = recombinationOperation;
        this.mutationOperations = mutationOperations.ToList();
    }

    public OffspringSpecification CreateCopy()
    {
        return new(recombinationOperation, mutationOperations.ToList());
    }
}

[Serializable]
public class Ancestry
{
    [SerializeField] private GenotypeWithoutAncestry first;
    [SerializeField] private List<OffspringSpecification> offspringSpecifications;

    public GenotypeWithoutAncestry First => first;
    public ReadOnlyCollection<OffspringSpecification> OffspringSpecifications => offspringSpecifications.AsReadOnly();

    public Ancestry(GenotypeWithoutAncestry first, List<OffspringSpecification> offspringSpecifications)
    {
        if (first == null)
            throw new ArgumentNullException("First cannot be null.");

        this.first = first;
        this.offspringSpecifications = offspringSpecifications ?? new();
    }

    public void RecordOffspringSpecification(OffspringSpecification offspringSpecification)
    {
        offspringSpecifications.Add(offspringSpecification);
    }

    public Ancestry CreateCopy()
    {
        return new(first, offspringSpecifications.Select(o => o.CreateCopy()).ToList());
    }
}
