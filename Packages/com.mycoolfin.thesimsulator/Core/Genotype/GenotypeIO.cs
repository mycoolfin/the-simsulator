using System;
using System.IO;
using Newtonsoft.Json;

namespace mycoolfin.TheSimsulator.Core.Genotype
{
    public static class GenotypeIO
    {
        public static async void SerializeAsync<TGenotype>(TGenotype genotype, string filePath, Action onSerialized) where TGenotype : IGenotype<TGenotype>
        {
            string json = JsonConvert.SerializeObject(genotype, Formatting.Indented);
            await File.WriteAllTextAsync(filePath, json);
            onSerialized?.Invoke();
        }

        public static async void DeserializeAsync<TGenotype>(string filePath, Action<TGenotype> onDeserialized) where TGenotype : IGenotype<TGenotype>
        {
            string json = await File.ReadAllTextAsync(filePath);
            TGenotype result = JsonConvert.DeserializeObject<TGenotype>(json);
            onDeserialized?.Invoke(result);
        }
    }
}
