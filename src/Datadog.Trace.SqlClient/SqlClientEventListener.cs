﻿using System;
using System.Diagnostics.Tracing;
using System.Globalization;

namespace Datadog.Trace.SqlClient
{
    internal class SqlClientEventListener : EventListener
    {
        /// <summary>
        /// The Ado.Net event source name
        /// </summary>
        private const string EventSourceName = "Microsoft-AdoNet-SystemData";

        /// <summary>
        /// Defines EventId for BeginExecute (Reader, Scalar, NonQuery, XmlReader).
        /// </summary>
        private const int BeginExecuteEventId = 1;

        /// <summary>
        /// Defines EventId for EndExecute (Reader, Scalar, NonQuery, XmlReader).
        /// </summary>
        private const int EndExecuteEventId = 2;

        private readonly Tracer _tracer;

        private string _serviceName;

        public SqlClientEventListener(Tracer tracer, string serviceName)
        {
            _tracer = tracer;
            _serviceName = serviceName;
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == EventSourceName)
            {
                EnableEvents(eventSource, EventLevel.Informational);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            try
            {
                switch (eventData.EventId)
                {
                    case BeginExecuteEventId:
                        ProcessBeginExecute(eventData);
                        break;
                    case EndExecuteEventId:
                        ProcessEndExecute(eventData);
                        break;
                }
            }
            catch
            {
                // TODO logme
            }
        }

        private void ProcessEndExecute(EventWrittenEventArgs eventData)
        {
            using (var scope = _tracer.ActiveScope)
            {
                int compositeState = Convert.ToInt32(eventData.Payload[1], CultureInfo.InvariantCulture);
                bool error = (compositeState & 1) == 0;
                scope.Span.Error = error;
                var sqlExceptionNumber = Convert.ToInt32(eventData.Payload[2], CultureInfo.InvariantCulture);
                if (sqlExceptionNumber != 0)
                {
                    scope.Span.SetTag("sql.exceptionNumber", sqlExceptionNumber.ToString());
                }
            }
        }

        private void ProcessBeginExecute(EventWrittenEventArgs eventData)
        {
            var scope = _tracer.StartActive("sqlclient.command", serviceName: _serviceName);
            var span = scope.Span;
            var database = eventData.Payload[2] as string;
            var commandText = eventData.Payload[3] as string;
            if (!string.IsNullOrEmpty(commandText))
            {
                span.ResourceName = commandText;
                span.SetTag(Tags.SqlQuery, commandText);
            }

            span.SetTag(Tags.SqlDatabase, database);
            span.Type = "sql";
        }
    }
}
