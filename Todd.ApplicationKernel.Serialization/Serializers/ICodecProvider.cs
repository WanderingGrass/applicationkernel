// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Todd.ApplicationKernel.Serialization.Cloning;

namespace Todd.ApplicationKernel.Serialization.Serializers
{
    public interface ICodecProvider: IDeepCopierProvider
    {
        IServiceProvider Services { get; }
    }
}
