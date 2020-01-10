using System;
using System.Collections.Generic;
using KellermanSoftware.CompareNetObjects;
using Newtonsoft.Json.Linq;

namespace PoeShared.Services
{
    internal sealed class ComparisonService : IComparisonService
    {
        private readonly CompareLogic diffLogic = new CompareLogic(
            new ComparisonConfig
            {
                DoublePrecision = 0.01,
                MaxDifferences = byte.MaxValue,
                ClassTypesToIgnore = new List<Type> { typeof(JToken), typeof(JValue), typeof(JProperty), typeof(JArray), typeof(JConstructor), typeof(JObject), typeof(JRaw) },
                CompareStaticFields = false,
                CompareStaticProperties = false,
                DecimalPrecision = 0.01m,
            });
        
        public ComparisonResult Compare(object first, object second)
        {
            return diffLogic.Compare(first, second);
        }
    }
}