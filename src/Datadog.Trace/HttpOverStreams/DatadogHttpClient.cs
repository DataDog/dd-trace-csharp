using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Datadog.Trace.HttpOverStreams.HttpContent;
using Datadog.Trace.Logging;

namespace Datadog.Trace.HttpOverStreams
{
    internal class DatadogHttpClient
    {
        /// <summary>
        /// Typical headers sent to the agent are small.
        /// Allow enough room for future expansion of headers.
        /// </summary>
        private const int MaxRequestHeadersBufferSize = 4096;

        private const string ContentLengthHeaderKey = "Content-Length";

        private static readonly IDatadogLogger Logger = DatadogLogging.GetLoggerFor<DatadogHttpClient>();

        public async Task<HttpResponse> SendAsync(HttpRequest request, Stream requestStream, Stream responseStream)
        {
            await SendRequestAsync(request, requestStream).ConfigureAwait(false);
            return await ReadResponseAsync(responseStream).ConfigureAwait(false);
        }

        private async Task SendRequestAsync(HttpRequest request, Stream requestStream)
        {
            // Headers are always ASCII per the HTTP spec
            using (var writer = new StreamWriter(requestStream, Encoding.ASCII, bufferSize: MaxRequestHeadersBufferSize, leaveOpen: true))
            {
                await DatadogHttpHeaderHelper.WriteLeadingHeaders(request, writer).ConfigureAwait(false);

                foreach (var header in request.Headers)
                {
                    await DatadogHttpHeaderHelper.WriteHeader(writer, header).ConfigureAwait(false);
                }

                await DatadogHttpHeaderHelper.WriteEndOfHeaders(writer).ConfigureAwait(false);
            }

            await request.Content.CopyToAsync(requestStream).ConfigureAwait(false);
            Logger.Debug("Datadog HTTP: Flushing stream.");
            await requestStream.FlushAsync().ConfigureAwait(false);
        }

        private async Task<HttpResponse> ReadResponseAsync(Stream responseStream)
        {
            var headers = new HttpHeaders();
            char currentChar = char.MinValue;
            int streamPosition = 0;

            // https://tools.ietf.org/html/rfc2616#section-4.2
            // HTTP/1.1 200 OK
            // HTTP/1.1 XXX MESSAGE

            const int statusCodeStart = 9;
            const int statusCodeEnd = 12;
            const int startOfReasonPhrase = 13;
            const int bufferSize = 10;

            // TODO: Get this from StringBuilderCache after we determine safe maximum capacity
            var stringBuilder = new StringBuilder();

            var chArray = new byte[bufferSize];

            async Task GoNextChar()
            {
                var bytesRead = await responseStream.ReadAsync(chArray, offset: 0, count: 1).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    throw new InvalidOperationException($"Unexpected end of stream at position {streamPosition}");
                }

                currentChar = Encoding.ASCII.GetChars(chArray)[0];
                streamPosition++;
            }

            async Task SkipUntil(int requiredStreamPosition)
            {
                var advanceCount = requiredStreamPosition - streamPosition;
                // Not required in release mode, as should only arise from programming error
                System.Diagnostics.Debug.Assert(advanceCount > 0, "RequiredStreamPosition should be greater than 0");
                System.Diagnostics.Debug.Assert(advanceCount <= bufferSize, "RequiredStreamPosition should be less than buffer size");
                var totalBytesRead = 0;

                while (totalBytesRead < advanceCount)
                {
                    var bytesRead = await responseStream.ReadAsync(chArray, offset: 0, count: advanceCount).ConfigureAwait(false);
                    if (bytesRead == 0)
                    {
                        throw new InvalidOperationException($"Unexpected end of stream at position {streamPosition}");
                    }

                    totalBytesRead += bytesRead;
                }

                currentChar = Encoding.ASCII.GetChars(chArray)[advanceCount - 1];
                streamPosition += advanceCount;
            }

            async Task ReadUntil(StringBuilder builder, char stopChar)
            {
                while (!currentChar.Equals(stopChar))
                {
                    builder.Append(currentChar);
                    await GoNextChar().ConfigureAwait(false);
                }
            }

            async Task ReadUntilNewLine(StringBuilder builder)
            {
                do
                {
                    if (await IsNewLine().ConfigureAwait(false))
                    {
                        break;
                    }

                    await ReadUntil(builder, DatadogHttpValues.CarriageReturn).ConfigureAwait(false);
                }
                while (true);
            }

            async Task<bool> IsNewLine()
            {
                if (currentChar.Equals(DatadogHttpValues.CarriageReturn))
                {
                    // end of headers
                    // Next character should be a LineFeed, regardless of Linux/Windows
                    // Skip the newline indicator
                    await GoNextChar().ConfigureAwait(false);

                    if (!currentChar.Equals(DatadogHttpValues.LineFeed))
                    {
                        throw new Exception($"Unexpected character {currentChar} in headers: CR must be followed by LF");
                    }

                    return true;
                }

                return false;
            }

            // Skip to status code
            await SkipUntil(statusCodeStart).ConfigureAwait(false);

            // Read status code
            while (streamPosition < statusCodeEnd)
            {
                await GoNextChar().ConfigureAwait(false);
                stringBuilder.Append(currentChar);
            }

            var potentialStatusCode = stringBuilder.ToString();
            stringBuilder.Clear();

            if (!int.TryParse(potentialStatusCode, out var statusCode))
            {
                throw new DatadogHttpRequestException("Invalid response, can't parse status code. Line was:" + potentialStatusCode);
            }

            // Skip to reason
            await SkipUntil(startOfReasonPhrase).ConfigureAwait(false);

            // Read reason
            await GoNextChar().ConfigureAwait(false);
            await ReadUntilNewLine(stringBuilder).ConfigureAwait(false);

            var reasonPhrase = stringBuilder.ToString();
            stringBuilder.Clear();

            // Read headers
            do
            {
                await GoNextChar().ConfigureAwait(false);

                // Check for end of headers
                if (await IsNewLine().ConfigureAwait(false))
                {
                    // Empty line, content starts next
                    break;
                }

                // Read key
                await ReadUntil(stringBuilder, stopChar: ':').ConfigureAwait(false);

                var name = stringBuilder.ToString().Trim();
                stringBuilder.Clear();

                // skip separator
                await GoNextChar().ConfigureAwait(false);

                // Read value
                await ReadUntilNewLine(stringBuilder).ConfigureAwait(false);

                var value = stringBuilder.ToString().Trim();
                stringBuilder.Clear();

                headers.Add(name, value);
            }
            while (true);

            var length = long.TryParse(headers.GetValue(ContentLengthHeaderKey), out var headerValue) ? headerValue : (long?)null;

            return new HttpResponse(statusCode, reasonPhrase, headers, new StreamContent(responseStream, length));
        }
    }
}
