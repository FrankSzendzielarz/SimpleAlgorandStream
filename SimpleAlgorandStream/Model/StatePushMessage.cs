using Algorand.Algod.Model;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Reflection;

namespace SimpleAlgorandStream.Model
{
    internal class StatePushMessage
    {
        public CertifiedBlock Block { get; set; }
        public LedgerStateDelta StateDelta { get; set; }
    }

    internal class IgnoreShouldSerializeContractResolver : DefaultContractResolver
    {
        private readonly bool useFriendlyName;

        public IgnoreShouldSerializeContractResolver(bool useFriendlyName)
        {
            this.useFriendlyName = useFriendlyName;
        }
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            if (useFriendlyName)
            {
                var attr = property.AttributeProvider.GetAttributes(typeof(JsonPropertyAttribute), true);
                if (attr.Any())
                {
                    property.PropertyName = property.UnderlyingName;
                }
            }
            property.ShouldSerialize = instance => true; // Ignore the ShouldSerialize methods
            property.Converter = null; // Ignore the custom converters

            return property;
        }
    }

    


}
