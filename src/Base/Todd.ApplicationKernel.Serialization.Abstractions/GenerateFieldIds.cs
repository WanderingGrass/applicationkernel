// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Todd.ApplicationKernel;
public enum GenerateFieldIds
{
    /// <summary>
    /// Only members explicitly annotated with a field id will be serialized. This is the default.
    /// </summary>
    None,
    /// <summary>
    /// Field ids will be automatically assigned to eligible public properties. To qualify, a property must have an accessible getter, and either an accessible setter or a corresponding constructor parameter.
    /// </summary>
    /// <remarks>
    /// The presence of an explicit field id annotation on any member of a type will automatically disable automatic field id generation for that type.
    /// </remarks>
    PublicProperties
}
