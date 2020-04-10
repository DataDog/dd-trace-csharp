using MessagePack;
using MessagePack.Formatters;

namespace Datadog.Trace.Agent.MessagePack
{
    internal struct FormatterResolverWrapper : IFormatterResolver
    {
        private readonly IFormatterResolver _resolver;

        public FormatterResolverWrapper(IFormatterResolver resolver)
        {
            _resolver = resolver;

#if NETSTANDARD2_0
            Options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
#endif
        }

#if NETSTANDARD2_0
        public MessagePackSerializerOptions Options { get; }
#endif

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return _resolver.GetFormatter<T>();
        }
    }
}
