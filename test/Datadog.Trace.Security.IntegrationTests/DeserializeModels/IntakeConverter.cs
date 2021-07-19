// <copyright file="IntakeConverter.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.AppSec.EventModel.Batch;
using Datadog.Trace.Vendors.Newtonsoft.Json;
using Datadog.Trace.Vendors.Newtonsoft.Json.Linq;

namespace Datadog.Trace.Security.IntegrationTests.DeserializeModels
{
    internal class IntakeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(Intake).IsAssignableFrom(objectType);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            var children = jo.Children();
            var events = children.First(c => c.Path == "events");
            var attacks = serializer.Deserialize<List<Datadog.Trace.AppSec.EventModel.Attack>>(reader);
            var intake = new Intake
            {
                Events = attacks,
                IdemPotencyKey = children.First(c => c.Path == "idempotencykey").Value<string>(),
                ProtocolVersion = children.First(c => c.Path == "protocolversion").Value<int>(),
            };

            return jo;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => serializer.Serialize(writer, value);
    }
}
