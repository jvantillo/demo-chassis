using Confluent.Kafka;
using OpenTracing;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace JP.Demo.Chassis.SharedCode.Kafka.Tracing
{
    public static class KafkaTracingHelper
    {
        public static ISpanBuilder ObtainConsumerSpanBuilder(ITracer tracer, Headers headers, string spanName)
        {
            // See if we have a trace header
            IHeader traceHeader = null;

            // For whatever insane reason this linq query is not working ??
            //traceHeader = cr.Message.Headers.FirstOrDefault(p => p.Key == "tracing-id");
            foreach (var header in headers)
            {
                bool isKey = header.Key == "tracing-id";
                if (isKey)
                {
                    traceHeader = header;
                    break;
                }
            }

            ISpanBuilder spanBuilder;
            if (traceHeader != null)
            {
                var map = KafkaTracingContextHelper.Decode(traceHeader.GetValueBytes());
                var ctx = tracer.Extract(BuiltinFormats.TextMap, new TextMapExtractAdapter(map));
                spanBuilder = tracer.BuildSpan(spanName)
                    .WithTag(Tags.SpanKind.Key, Tags.SpanKindConsumer)
                    .AsChildOf(ctx);
            }
            else
            {
                spanBuilder = tracer.BuildSpan(spanName)
                    .WithTag(Tags.SpanKind.Key, Tags.SpanKindConsumer);
            }

            return spanBuilder;
        }
    }
}
