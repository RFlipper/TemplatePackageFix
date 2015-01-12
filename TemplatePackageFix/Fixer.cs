using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Plumsail.TemplatePackageFix.Helpers;

namespace Plumsail.TemplatePackageFix
{
    public sealed class Fixer
    {
        #region Declarations 
        readonly XNamespace ns           = "http://schemas.microsoft.com/sharepoint/";
        readonly string WFPackSolutionID = "64283c6e-6aea-4a86-a881-042359b3521a";
        readonly string WFFeaturePath    = "SP2013Workflows";
        readonly string BasePath;

        private XDocument wfFeature;
        private XDocument wfElements;
        private string wfFeatureDir;
        #endregion

        #region Constructor
        public Fixer(string path)
        {
            BasePath = path;
            CreateWorkflowFeature();
            ProcessManifest();
        }
        #endregion

        #region Control methods
        internal void ProcessManifest()
        {
            var manifestFilePath = Path.Combine(BasePath, "manifest.xml");

            //Load features
            var manifest = XDocument.Load(manifestFilePath);
            var features = manifest.Descendants(ns + "FeatureManifest");
            
            //Process features list defined in manifest.xml
            foreach (var featureDef in features)
            {
                var featureFile = Path.Combine(BasePath, featureDef.Attribute("Location").Value);
                ProcessFeature(featureFile);
            }

            //Add additional workflow nodes to manifest.xml
            manifest.Descendants(ns + "FeatureManifests").Single().Add(
                new XElement(ns + "FeatureManifest", 
                    new XAttribute("Location", Path.Combine(WFFeaturePath, "Feature.xml"))));

            wfElements.SaveToFile(Path.Combine(BasePath, WFFeaturePath, "Elements.xml"));
            wfFeature.SaveToFile(Path.Combine(BasePath, WFFeaturePath, "Feature.xml"));

            RemovePackageDependency(manifest);

            manifest.SaveToFile(manifestFilePath);
        }

        internal void ProcessFeature(string featureFile)
        {
            var featureDir   = Path.GetDirectoryName(featureFile);
            var elementsFile = Path.Combine(featureDir, "Elements.xml");

            var feature  = XDocument.Load(featureFile);
            var elements = XDocument.Load(elementsFile);

            RemoveCustomActions(feature, elements, featureDir);
            RemoveModules(feature, elements, featureDir);
            RemoveDependentFeatures(feature, elements, featureDir);
            MoveWorkflowModules(feature, elements, featureDir);

            //Save files
            elements.SaveToFile(elementsFile);
            feature.SaveToFile(featureFile);
        }
        #endregion 

        #region Service methods

        internal void CreateWorkflowFeature()
        {
            wfFeatureDir = Path.Combine(BasePath, WFFeaturePath);
            System.IO.Directory.CreateDirectory(wfFeatureDir);
            wfFeature = new XDocument(
                new XElement(ns + "Feature",
                    new XAttribute("Id", Guid.NewGuid().ToString()),
                    new XAttribute("Title", "Template workflows"),
                    new XAttribute("Description", "Template workflows"),
                    new XAttribute("Scope", "Web"),
                    new XAttribute("Version", "1.0.0.0"),
                    new XElement(ns + "ElementManifests",
                        new XElement(ns + "ElementManifest", new XAttribute("Location", "Elements.xml")))
                    ));

            wfElements = new XDocument(new XElement(ns + "Elements"));
        }

        private void MoveWorkflowModules(XDocument feature, XDocument elements, string featureDir)
        {
            Expression<Func<XElement, bool>> ex = el => el.Attribute("Location").Value.Contains("\\wfsvc\\");
            var apNodes =
                feature.Descendants(ns + "ElementFile")
                    .Where(el => el.Attribute("Location").Value.Contains("\\wfsvc\\") 
                             && !el.Attribute("Location").Value.Contains("Schema.xml"));

            if (apNodes.Count() < 1)
                return;

            var paths = new HashSet<string>();
            foreach (var elementFile in apNodes)
            {
                var fileRelativePath = elementFile.Attribute("Location").Value;
                var fileName = Path.GetFileName(fileRelativePath);
                var filePath = Path.GetDirectoryName(fileRelativePath);
                paths.Add(filePath);

                Directory.CreateDirectory(Path.Combine(wfFeatureDir, filePath));
                File.Move(Path.Combine(featureDir, fileRelativePath), Path.Combine(wfFeatureDir, fileRelativePath));
            }

            wfFeature.Descendants(ns + "ElementManifests").Single().Add(apNodes);
            apNodes.Remove();

            var elNodes = elements.Descendants(ns + "Module").Where(el => paths.Contains(el.Attribute("Path").Value));
            wfElements.Root.Add(elNodes);
            elNodes.Remove();
        }
        
        private void RemoveDependentFeatures(XDocument feature, XDocument elements, string featureDir)
        {
            var onet = feature.Descendants(ns + "ElementFile").SingleOrDefault(el => el.Attribute("Location").Value.Contains("\\ONet.xml"));
            if (onet == null)
                return;

            var onetFilePath = Path.Combine(featureDir, onet.Attribute("Location").Value);
            var onetDocument = XDocument.Load(onetFilePath);
            var features = onetDocument.Descendants(ns + "Feature")
                .Where(el => el.Attribute("ID").Value == "{9a5d1295-65c0-4c8e-a926-968da90d2ef9}"
                          || el.Attribute("ID").Value == "{d7891031-e7f5-4734-8077-9189dd35551c}");

            if (features.Count() < 1)
                return;

            features.Remove();
            onetDocument.SaveToFile(onetFilePath);
        }

        private void RemoveCustomActions(XDocument feature, XDocument elements, string featureDir)
        {
            var nodes = elements.Descendants(ns + "CustomAction")
                .Where(el => el.Attribute("Id").Value.Contains("WFPackAdminPage") 
                         || (el.Attribute("ScriptSrc") != null && el.Attribute("ScriptSrc").Value.Contains("Plumsail")));

            if (nodes.Count() < 1)
                return;

            nodes.Remove();
        }

        private void RemoveModules(XDocument feature, XDocument elements, string featureDir)
        {
            var workflowFiles =
                feature.Descendants(ns + "ElementFile")
                    .Where(el => el.Attribute("Location").Value.Contains("\\Workflows\\"));

            if (workflowFiles.Count() < 1)
                return;

            //Get list of nodes to remove from Elements.xml
            var filesToRemove = new List<XElement>();
            foreach (var elementFile in workflowFiles)
            {
                var fileRelativePath = elementFile.Attribute("Location").Value;
                var fileName = Path.GetFileName(fileRelativePath);
                var filePath = Path.GetDirectoryName(fileRelativePath);

                var fileNode = elements.Descendants(ns + "File").FirstOrDefault(
                    el => el.Parent.Attribute("Path").Value == filePath 
                       && el.Attribute("Path").Value == fileName);
                
                if (fileNode != null)
                    filesToRemove.Add(fileNode);

                //Delete physical file
                File.Delete(Path.Combine(featureDir, fileRelativePath));
            }

            //Remove nodes from Elements.xml
            filesToRemove.ForEach(el => el.Remove());
            //Remove empty module elements
            elements.Descendants(ns + "Module").Where(el => !el.HasElements).Remove();

            //Remove nodes from Feature.xml
            workflowFiles.Remove();
        }

        private void RemovePackageDependency(XDocument manifest)
        {
            var dependencyNode = manifest.Descendants(ns + "ActivationDependency")
                .SingleOrDefault(el => el.Attribute("SolutionId").Value == WFPackSolutionID);

            if (dependencyNode == null)
                return;

            var parent = dependencyNode.Parent;

            dependencyNode.Remove();

            //remove empty ActivationDependencies node
            if (!parent.HasElements)
                parent.Remove();
        }
        #endregion

    }
}
