using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kafka2SQS;

internal static class Telemetry
{
    public const string TraceName = "Kafka2SQS";

    public static ActivitySource Trace { get; } = new ActivitySource(TraceName);
}
