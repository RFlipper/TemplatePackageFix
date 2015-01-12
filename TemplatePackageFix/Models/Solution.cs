using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plumsail.TemplatePackageFix.Models
{
    public class Solution
    {
        public class FeatureManifest
        {
            public string Location { get; set; }
        }

        public class ActivationDependency
        {
            public Guid SolutionId { get; set; }
            public string SolutionName { get; set; }
        }

        public Guid SolutionId { get; set; }
        public string SharePointProductVersion { get; set; }

        public List<FeatureManifest> FeatureManifests { get; set; }
        public List<ActivationDependency> ActivationDependencies { get; set; }
    }
}
