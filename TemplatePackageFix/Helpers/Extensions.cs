using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Plumsail.TemplatePackageFix.Helpers
{
    public static class Extensions
    {
        public static void SaveToFile(this XDocument doc, string path)
        {
            var settings = new XmlWriterSettings()
            {
                OmitXmlDeclaration = true
                , Indent = true
                , IndentChars = "\t"
                , NewLineChars = Environment.NewLine
                , NewLineHandling = NewLineHandling.Replace
            };
            using (var stream = System.IO.File.Create(path))
            {
                using (var writer = XmlWriter.Create(stream, settings))
                {
                    doc.Save(writer);
                }
            }
        }

    }
}
