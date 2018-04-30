﻿using System;
using OpenTracing;
using OpenTracing.Propagation;

namespace Datadog.Trace
{
    internal class HttpHeadersCodec : ICodec
    {
        public HttpHeadersCodec()
        {
        }

        public OpenTracingSpanContext Extract(object carrier)
        {
            ITextMap map = carrier as ITextMap;
            if (map == null)
            {
                throw new NotSupportedException("Carrier should have type ITextMap");
            }

            string parentIdHeader = null;
            string traceIdHeader = null;
            foreach (var keyVal in map)
            {
                if (keyVal.Key.Equals(Constants.HttpHeaderParentId, StringComparison.InvariantCultureIgnoreCase))
                {
                    parentIdHeader = keyVal.Value;
                }

                if (keyVal.Key.Equals(Constants.HttpHeaderTraceId, StringComparison.InvariantCultureIgnoreCase))
                {
                    traceIdHeader = keyVal.Value;
                }
            }

            if (parentIdHeader == null)
            {
                throw new ArgumentException($"{Constants.HttpHeaderParentId} should be set.");
            }

            if (traceIdHeader == null)
            {
                throw new ArgumentException($"{Constants.HttpHeaderTraceId} should be set.");
            }

            ulong parentId;
            try
            {
                parentId = Convert.ToUInt64(parentIdHeader);
            }
            catch (FormatException)
            {
                throw new FormatException($"{Constants.HttpHeaderParentId} should contain an unsigned integer value");
            }

            ulong traceId;
            try
            {
                traceId = Convert.ToUInt64(traceIdHeader);
            }
            catch (FormatException)
            {
                throw new FormatException($"{Constants.HttpHeaderTraceId} should contain an unsigned integer value");
            }

            return new OpenTracingSpanContext(traceId, parentId);
        }

        public void Inject(OpenTracingSpanContext spanContext, object carrier)
        {
            ITextMap map = carrier as ITextMap;
            if (map == null)
            {
                throw new NotSupportedException("Carrier should have type ITextMap");
            }

            map.Set(Constants.HttpHeaderParentId, spanContext.SpanId.ToString());
            map.Set(Constants.HttpHeaderTraceId, spanContext.TraceId.ToString());
        }
    }
}
