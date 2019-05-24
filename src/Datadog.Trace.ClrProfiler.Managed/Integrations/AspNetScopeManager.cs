#if !NETSTANDARD2_0

using System;
using System.Web;
using Datadog.Trace.Logging;

namespace Datadog.Trace.ClrProfiler.Integrations
{
    internal class AspNetScopeManager : IScopeManager
    {
        private static readonly ILog Log = LogProvider.For<AspNetScopeManager>();

        private readonly string _name = "__Datadog_Scope_Current__" + Guid.NewGuid();
        private readonly AsyncLocalCompat<Scope> _activeScopeFallback = new AsyncLocalCompat<Scope>();

        public Scope Active
        {
            get
            {
                var activeScope = HttpContext.Current?.Items[_name] as Scope;
                if (activeScope != null)
                {
                    return activeScope;
                }

                return _activeScopeFallback.Get();
            }
        }

        public Scope Activate(Span span, bool finishOnClose)
        {
            var activeScope = Active;
            var scope = new Scope(activeScope, span, this, finishOnClose);
            SetScope(scope);
            return scope;
        }

        public void Close(Scope scope)
        {
            var current = Active;

            if (current != null && current == scope)
            {
                // if the scope that was just closed was the active scope,
                // set its parent as the new active scope
                SetScope(current.Parent);
            }
        }

        private void SetScope(Scope scope)
        {
            var httpContext = HttpContext.Current;
            if (httpContext != null)
            {
                httpContext.Items[_name] = scope;
            }

            _activeScopeFallback.Set(scope);
        }
    }
}
#endif
