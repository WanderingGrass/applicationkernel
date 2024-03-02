// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Todd.ApplicationKernel.Logging;

public class CorrelationContextLoggingMiddleware : IMiddleware
{
    private readonly ILogger<CorrelationContextLoggingMiddleware> _logger;

    public CorrelationContextLoggingMiddleware(ILogger<CorrelationContextLoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var headers = Activity.Current.Baggage
            .ToDictionary(x => x.Key, x => x.Value);
        using (_logger.BeginScope(headers))
        {
            return next(context);
        }
    }
}
