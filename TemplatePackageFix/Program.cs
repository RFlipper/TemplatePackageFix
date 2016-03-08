using System;

namespace Plumsail.TemplatePackageFix
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                var message = @"Example: TemplatePackageFix.exe  C:\temp\HelpDeskTemplate.wsp" + 
                "\nThe program will remove any Plumsail Dependencies from the web template package.";

                Console.WriteLine(message);
                return;
            }

            var path = args[0];
            Console.Write("Processing file {0} ", path);
            using (var packer = new Packer(path))
            {
                var baseTempFolder = packer.TempFolder;
                new Fixer(baseTempFolder);
            }

            Console.WriteLine(" - fixed");
        }
    }
}
