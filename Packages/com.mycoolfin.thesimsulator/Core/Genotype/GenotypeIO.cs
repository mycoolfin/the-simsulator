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
        private static readonly JsonSerializerSettings Settings = new()
        {
            ContractResolver = new IgnoreNamePropertyResolver(),
            Formatting = Formatting.Indented
        };

        public static async void SerializeAsync<TGenotype>(TGenotype genotype, string filePath, Action<bool> onSerialized) 
            where TGenotype : IGenotype<TGenotype>
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                string json = JsonConvert.SerializeObject(genotype, Settings);
                await File.WriteAllTextAsync(filePath, json);
            }, onSerialized);
        }

        public static async void DeserializeAsync<TGenotype>(string filePath, Action<bool, TGenotype> onDeserialized) 
            where TGenotype : IGenotype<TGenotype>
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                string json = await File.ReadAllTextAsync(filePath);
                return DeserializeAndSetName<TGenotype>(json, filePath);
            }, onDeserialized);
        }

        public static void Serialize<TGenotype>(TGenotype genotype, string filePath) 
            where TGenotype : IGenotype<TGenotype>
        {
            string json = JsonConvert.SerializeObject(genotype, Settings);
            File.WriteAllText(filePath, json);
        }

        public static TGenotype Deserialize<TGenotype>(string filePath) 
            where TGenotype : IGenotype<TGenotype>
        {
            string json = File.ReadAllText(filePath);
            return DeserializeAndSetName<TGenotype>(json, filePath);
        }

        private static TGenotype DeserializeAndSetName<TGenotype>(string json, string filePath) 
            where TGenotype : IGenotype<TGenotype>
        {
            TGenotype result = JsonConvert.DeserializeObject<TGenotype>(json, Settings);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            result.Name = fileName;
            return result;
        }

        private static async System.Threading.Tasks.Task ExecuteWithErrorHandlingAsync(Func<System.Threading.Tasks.Task> asyncAction, Action<bool> onCompleted)
        {
            try
            {
                await asyncAction();
                onCompleted?.Invoke(true);
            }
            catch
            {
                onCompleted?.Invoke(false);
            }
        }

        private static async System.Threading.Tasks.Task ExecuteWithErrorHandlingAsync<T>(Func<System.Threading.Tasks.Task<T>> asyncFunc, Action<bool, T> onCompleted)
        {
            try
            {
                T result = await asyncFunc();
                onCompleted?.Invoke(true, result);
            }
            catch
            {
                onCompleted?.Invoke(false, default);
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
