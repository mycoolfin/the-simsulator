using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace mycoolfin.TheSimsulator.Core.Genotype
{
    public static class GenotypeIO
    {
        public static async void SerializeAsync<TGenotype>(TGenotype genotype, string filePath, Action<bool> onSerialized) where TGenotype : IGenotype<TGenotype>
        {
            try
            {
                JsonSerializerSettings settings = new()
                {
                    ContractResolver = new IgnoreNamePropertyResolver(),
                    Formatting = Formatting.Indented
                };
                string json = JsonConvert.SerializeObject(genotype, settings);
                await File.WriteAllTextAsync(filePath, json);
                onSerialized?.Invoke(true);
            }
            catch
            {
                onSerialized?.Invoke(false);
            }
        }

        public static async void DeserializeAsync<TGenotype>(string filePath, Action<bool, TGenotype> onDeserialized) where TGenotype : IGenotype<TGenotype>
        {
            try
            {
                JsonSerializerSettings settings = new()
                {
                    ContractResolver = new IgnoreNamePropertyResolver(),
                    Formatting = Formatting.Indented
                };
                string json = await File.ReadAllTextAsync(filePath);
                TGenotype result = JsonConvert.DeserializeObject<TGenotype>(json);
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                result.Name = fileName;
                onDeserialized?.Invoke(true, result);
            }
            catch
            {
                onDeserialized?.Invoke(false, default);
            }
        }

        private class IgnoreNamePropertyResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);
                // Exclude the "Name" property, as it is defined by the file name.
                return properties.Where(p => p.PropertyName != "Name").ToList();
            }
        }
    }
}
