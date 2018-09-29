using System.Collections.Generic;
using System.Linq;

namespace KolonyTools
{
    public static class OrbitalLogisticsExtensions
    {
        /// <summary>
        /// Gets the <see cref="PartResourceDefinition"/> for each of the resources (with mass) the vessel can store.
        /// </summary>
        /// <param name="vessel"></param>
        /// <returns></returns>
        public static List<OrbitalLogisticsResource> GetResources(this Vessel vessel)
        {
            List<OrbitalLogisticsResource> resources;

            if (vessel.packed && !vessel.loaded) // inactive vessel
            {
                resources = vessel.protoVessel.protoPartSnapshots
                    .SelectMany(p => p.resources.Where(r => r.definition.density > 0).Select(r => r.definition))
                    .Distinct()
                    .Select(r => new OrbitalLogisticsResource(r, vessel))
                    .ToList();
            }
            else // active vessel
            {
                var vResList = new List<PartResource>();
                foreach (var part in vessel.Parts)
                {
                    var rCount = part.Resources.Count;
                    for (int i = 0; i < rCount; ++i)
                    {
                        var res = part.Resources[i];
                        vResList.Add(res);
                    }
                }
                resources = vResList
                    .Where(r => r.info.density > 0)
                    .Select(r => r.info)
                    .Distinct()
                    .Select(r => new OrbitalLogisticsResource(r, vessel))
                    .ToList();
            }

            // Sort by resource name
            resources.Sort((a, b) =>
            {
                return a.Name.CompareTo(b.Name);
            });

            return resources;
        }
    }
}
