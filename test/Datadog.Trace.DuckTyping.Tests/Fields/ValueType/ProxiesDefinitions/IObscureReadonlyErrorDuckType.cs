// <copyright file="IObscureReadonlyErrorDuckType.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Datadog.Trace.DuckTyping.Tests.Fields.ValueType.ProxiesDefinitions
{
    public interface IObscureReadonlyErrorDuckType
    {
        [Duck(Name = "_publicReadonlyValueTypeField", Kind = DuckKind.Field)]
        int PublicReadonlyValueTypeField { get; set; }
    }
}
