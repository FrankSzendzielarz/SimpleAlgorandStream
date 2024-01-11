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
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            property.ShouldSerialize = instance => true; // Ignore the ShouldSerialize methods
            property.Converter = null; // Ignore the custom converters

            return property;
        }
    }

}
