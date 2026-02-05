using System.Buffers;
using System.IO.Pipelines;

using GenHTTP.Engine.Draft.Types;

using Glyph11.Parser;

namespace GenHTTP.Engine.Draft.Parser;

public static class RequestParser
{

    public static bool TryParse(ReadOnlySequence<byte> buffer, Request into, out int bytesRead)
    {
        var raw = into.Source;

        if (Parser11.TryExtractFullHeader(ref buffer, raw, out bytesRead))
        {
            into.Apply();
            return true;
        }

        return false;
    }

}
