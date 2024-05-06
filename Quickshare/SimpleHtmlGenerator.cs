using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quickshare
{
    internal static class SimpleHtmlGenerator
    {
        internal static void GenerateHtmlFile(Dictionary<string, string> files, Stream stream)
        {
            StreamWriter writer = new StreamWriter(stream);
            writer.WriteLine("<!DOCTYPE html>");
            writer.WriteLine("<html lang=\"en\">");
            writer.WriteLine("<head>");
            writer.WriteLine("    <meta charset=\"UTF-8\">");
            writer.WriteLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            writer.WriteLine("    <title>Shared-files (powered by Storj)</title>");
            writer.WriteLine("</head>");
            writer.WriteLine("<body>");
            writer.WriteLine("    <h1>Here are your files (powered by <a href='https://storj.io'>Storj</a>)</h1>");
            writer.WriteLine("    <ul>");

            foreach (var file in files)
            {
                writer.WriteLine($"        <li><a href=\"{file.Value}\">{file.Key}</a></li>");
            }

            writer.WriteLine("    </ul>");
            writer.WriteLine("</body>");
            writer.WriteLine("</html>");
            writer.Flush();
        }
    }
}
