using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.Agent;
using Datadog.Trace.Configuration;
using Datadog.Trace.Sampling;
using FluentAssertions;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.Tests
{
    public class SpanTests
    {
        private readonly ITestOutputHelper _output;
        private readonly Mock<IAgentWriter> _writerMock;
        private readonly Tracer _tracer;

        public SpanTests(ITestOutputHelper output)
        {
            _output = output;

            var settings = new TracerSettings();
            _writerMock = new Mock<IAgentWriter>();
            var samplerMock = new Mock<ISampler>();

            _tracer = new Tracer(settings, _writerMock.Object, samplerMock.Object, scopeManager: null, statsd: null);
        }

        [Fact]
        public void SetTag_KeyValue_KeyValueSet()
        {
            const string key = "Key";
            const string value = "Value";
            var span = _tracer.StartSpan("Operation");
            Assert.Null(span.GetTag(key));

            span.SetTag(key, value);

            _writerMock.Verify(x => x.WriteTrace(It.IsAny<Span[]>()), Times.Never);
            Assert.Equal(span.GetTag(key), value);
        }

        [Fact]
        public void Finish_StartTimeInThePastWithNoEndTime_DurationProperlyComputed()
        {
            // The 100 additional milliseconds account for the clock precision
            var startTime = DateTimeOffset.UtcNow.AddMinutes(-1).AddMilliseconds(-100);
            var span = _tracer.StartSpan("Operation", startTime: startTime);

            span.Finish();

            Assert.True(span.Duration >= TimeSpan.FromMinutes(1) && span.Duration < TimeSpan.FromMinutes(2));
        }

        [Fact]
        public async Task Finish_NoEndTimeProvided_SpanWriten()
        {
            var span = _tracer.StartSpan("Operation");
            await Task.Delay(TimeSpan.FromMilliseconds(1));
            span.Finish();

            _writerMock.Verify(x => x.WriteTrace(It.IsAny<Span[]>()), Times.Once);
            Assert.True(span.Duration > TimeSpan.Zero);
        }

        [Fact]
        public void Finish_EndTimeProvided_SpanWritenWithCorrectDuration()
        {
            var startTime = DateTimeOffset.UtcNow;
            var endTime = DateTime.UtcNow.AddMilliseconds(10);
            var span = _tracer.StartSpan("Operation", startTime: startTime);

            span.Finish(endTime);

            Assert.Equal(endTime - startTime, span.Duration);
        }

        [Fact]
        public void Finish_EndTimeInThePast_DurationIs0()
        {
            var startTime = DateTimeOffset.UtcNow;
            var endTime = DateTime.UtcNow.AddMilliseconds(-10);
            var span = _tracer.StartSpan("Operation", startTime: startTime);

            span.Finish(endTime);

            Assert.Equal(TimeSpan.Zero, span.Duration);
        }

        [Fact]
        public void Accurate_Duration()
        {
            // Check that span has the same precision as stopwatch

            const int iterations = 10;
            var stopwatch = new Stopwatch();

            for (int i = 0; i < iterations; i++)
            {
                Span span;

                using (span = _tracer.StartSpan("Operation"))
                {
                    stopwatch.Restart();
                    stopwatch.Stop();
                }

                span.Duration.Should().BeGreaterOrEqualTo(stopwatch.Elapsed);

                stopwatch.Restart();
                using (span = _tracer.StartSpan("Operation"))
                {
                }

                stopwatch.Stop();

                span.Duration.Should().BeLessOrEqualTo(stopwatch.Elapsed);
            }
        }

        [Fact]
        public void TopLevelSpans()
        {
            var spans = new List<(Scope Scope, bool IsTopLevel)>();

            spans.Add((_tracer.StartActive("Root", serviceName: "root"), true));
            spans.Add((_tracer.StartActive("Child1", serviceName: "root"), false));
            spans.Add((_tracer.StartActive("Child2", serviceName: "child"), true));
            spans.Add((_tracer.StartActive("Child3", serviceName: "child"), false));
            spans.Add((_tracer.StartActive("Child4", serviceName: "root"), true));

            foreach (var (scope, expectedResult) in spans)
            {
                scope.Span.Should().Match<Span>(s => s.IsTopLevel == expectedResult);
            }
        }
    }
}
