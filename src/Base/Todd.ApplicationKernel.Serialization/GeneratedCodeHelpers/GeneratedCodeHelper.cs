// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Todd.ApplicationKernel.Serialization.GeneratedCodeHelpers
{
    public static class GeneratedCodeHelper
    {
        private static readonly ThreadLocal<RecursiveServiceResolutionState> ResolutionState = new ThreadLocal<RecursiveServiceResolutionState>(() => new RecursiveServiceResolutionState());

        private sealed class RecursiveServiceResolutionState
        {
            private int _depth;

            public List<object> Callers { get; } = new List<object>();

            public void Enter(object caller)
            {
                ++_depth;
                if (caller is not null)
                {
                    Callers.Add(caller);
                }
            }

            public void Exit()
            {
                if (--_depth <= 0)
                {
                    Callers.Clear();
                }
            }
        }

        /// <summary>
        /// Unwraps the provided service if it was wrapped.
        /// </summary>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <param name="caller">The caller.</param>
        /// <param name="service">The service.</param>
        /// <returns>The unwrapped service.</returns>
        public static TService UnwrapService<TService>(object caller, TService service)
        {
            var state = ResolutionState.Value;

            try
            {
                state.Enter(caller);

                foreach (var c in state.Callers)
                {
                    if (c is TService s and not IServiceHolder<TService>)
                    {
                        return s;
                    }
                }

                return Unwrap(service);
            }
            finally
            {
                state.Exit();
            }

            static TService Unwrap(TService val)
            {
                while (val is IServiceHolder<TService> wrapping)
                {
                    val = wrapping.Value;
                }

                return val;
            }
        }
        internal static object TryGetService(Type serviceType)
        {
            var state = ResolutionState.Value;
            foreach (var c in state.Callers)
            {
                var type = c?.GetType();
                if (serviceType == type)
                {
                    return c;
                }
            }

            return null;
        }
    }
}
