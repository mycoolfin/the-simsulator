using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace mycoolfin.TheSimsulator
{
    public readonly struct WeightedChoice<T>
    {
        public T Item { get; }
        public float Weight { get; }

        public WeightedChoice(T item, float weight)
        {
            Item = item;
            Weight = weight;
        }
    }

    public class WeightedChoiceList<T> : IEnumerable<WeightedChoice<T>>
    {
        private readonly List<WeightedChoice<T>> choices = new();

        public IReadOnlyList<WeightedChoice<T>> Choices => choices;

        public void Add(T item, float weight)
        {
            if (weight <= 0)
                throw new ArgumentException("Weight must be greater than zero.", nameof(weight));

            choices.Add(new WeightedChoice<T>(item, weight));
        }
        public T Choose()
        {
            if (choices.Count == 0)
                throw new InvalidOperationException("No choices available.");

            float totalWeight = choices.Sum(c => c.Weight);
            float randomValue = (float)SharedRandom.NextDouble() * totalWeight;

            float cumulative = 0f;
            foreach (var c in choices)
            {
                cumulative += c.Weight;
                if (randomValue < cumulative)
                    return c.Item;
            }

            // If we reach here due to floating-point precision issues, return the last choice.
            return choices[^1].Item;
        }

        public IEnumerator<WeightedChoice<T>> GetEnumerator() => choices.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
