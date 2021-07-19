// <copyright file="Intake.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datadog.Trace.Abstractions;
using Datadog.Trace.Vendors.Newtonsoft.Json;

namespace Datadog.Trace.Security.IntegrationTests.DeserializeModels.Attack
{
    internal class Intake : Datadog.Trace.AppSec.EventModel.Batch.Intake
    {
        [JsonProperty("events")]
        internal new IEnumerable<Datadog.Trace.AppSec.EventModel.Attack> Events { get; set; }
    }
}
