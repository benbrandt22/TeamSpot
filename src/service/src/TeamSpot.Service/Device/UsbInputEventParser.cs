using System.Reflection;

namespace TeamSpot.Service.Device
{

    /// <summary>
    /// Scans the assembly for implementations of IUsbInputEventParser and uses them to
    /// parse incoming USB input reports into strongly-typed IUsbInputEvent instances.
    /// </summary>
    public static class UsbInputEventParser
    {
        private static readonly IReadOnlyList<EventParserEntry> _parsers = DiscoverParsers();

        private record EventParserEntry(
            Func<byte[], bool> CanParse,
            Func<byte[], IUsbInputEvent> Parse
        );

        private static IReadOnlyList<EventParserEntry> DiscoverParsers()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t is { IsClass: true, IsAbstract: false }
                    && typeof(IUsbInputEventParser).IsAssignableFrom(t))
                .Select(t =>
                {
                    var canParse = t.GetMethod(nameof(IUsbInputEventParser.CanParse), BindingFlags.Public | BindingFlags.Static)!;
                    var parse = t.GetMethod(nameof(IUsbInputEventParser.Parse), BindingFlags.Public | BindingFlags.Static)!;

                    return new EventParserEntry(
                        CanParse: x => (bool)canParse.Invoke(null, [x])!,
                        Parse: x => (IUsbInputEvent)parse.Invoke(null, [x])!
                    );
                })
                .ToList();
        }

        public static IUsbInputEvent? TryParse(byte[] inputReport)
        {
            foreach (var parser in _parsers)
            {
                if (parser.CanParse(inputReport))
                {
                    return parser.Parse(inputReport);
                }
            }
            return null;
        }
    }

}
