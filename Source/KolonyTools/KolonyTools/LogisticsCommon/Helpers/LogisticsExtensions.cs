using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Debug = UnityEngine.Debug;
using Object = System.Object;

namespace KolonyTools
{
    public static class LogisticsExtensions
    {
        /// <summary>
        /// Gets the 'mu' value for the <see cref="CelestialBody"/> (i.e. gravitional constant X body mass).
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public static double Mu(this CelestialBody body)
        {
            return body.Mass * 6.67408e-11;
        }

        /// <summary>
        /// Calculates the average orbital velocity of a vessel, ignoring eccentricity.
        /// </summary>
        /// <param name="vessel"></param>
        /// <returns></returns>
        public static double AverageOrbitalVelocity(this Vessel vessel)
        {
            double sma = vessel.packed && !vessel.loaded
                ? vessel.protoVessel.orbitSnapShot.semiMajorAxis
                : vessel.orbit.semiMajorAxis;

            return OrbitalVelocity(vessel.mainBody, sma);
        }

        /// <summary>
        /// Calculates orbital velocity for a circular orbit.
        /// </summary>
        /// <param name="body">The <see cref="CelestialBody"/> of the orbit.</param>
        /// <param name="semiMajorAxis">The semi major axis of the orbit.</param>
        /// <returns></returns>
        public static double OrbitalVelocity(CelestialBody body, double semiMajorAxis)
        {
            return Math.Sqrt(body.Mu() * 1 / semiMajorAxis);
        }

        /// <summary>
        /// Calculates the orbital period for an orbit.
        /// </summary>
        /// <param name="body"></param>
        /// <param name="semiMajorAxis"></param>
        /// <returns></returns>
        public static double OrbitalPeriod(CelestialBody body, double semiMajorAxis)
        {
            return Math.Sqrt(4 * Math.Pow(Math.PI, 2) / body.Mu() * Math.Pow(semiMajorAxis, 3));
        }

        /// <summary>
        /// Determines if a <see cref="Vessel"/> can store a given <see cref="PartResourceDefinition"/>.
        /// </summary>
        /// <param name="vessel"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        public static bool HasResource(this Vessel vessel, PartResourceDefinition resource)
        {
            if (vessel.packed && !vessel.loaded) // inactive vessel
            {
                return vessel.protoVessel.protoPartSnapshots
                    .Any(p => p.resources.Any(r => r.definition.id == resource.id));
            }
            else // active vessel
            {
                foreach (var part in vessel.Parts)
                {
                    if (part.Resources.Any(r => r.info.id == resource.id))
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the current available amount of the currency used for transporting goods via Orbital Logistics.
        /// </summary>
        /// <param name="vessel"></param>
        /// <returns></returns>
        public static double GetTransportCapacity(this Vessel vessel)
        {
            PartResourceDefinition transportCredits = PartResourceLibrary.Instance.GetDefinition("TransportCredits");

            if (transportCredits == null || !vessel.HasResource(transportCredits))
                return 0;

            OrbitalLogisticsResource transportCreditsResource = new OrbitalLogisticsResource(transportCredits, vessel);

            return transportCreditsResource.GetAvailableAmount();
        }

        /// <summary>
        /// Determines if the vessel can fulfill the requirements to complete an Orbital Logistics transfer.
        /// </summary>
        /// <param name="resourceUnits">The amount of the transport resource required for the transfer.</param>
        /// <returns></returns>
        public static bool CanAffordTransport(this Vessel vessel, double resourceUnits)
        {
            return vessel.GetTransportCapacity() >= resourceUnits;
        }

        /// <summary>
        /// Attempts to fulfill the requirements to complete an orbital logistics transfer.
        /// </summary>
        /// <param name="vessel"></param>
        /// <param name="resourceUnits">The amount of the transport resource required for the transfer.</param>
        /// <returns><c>true</c> if the requirements were met, <c>false</c> otherwise.</returns>
        public static bool DeductTransportCost(this Vessel vessel, double resourceUnits)
        {
            if (!vessel.CanAffordTransport(resourceUnits))
                return false;

            // Deduct resource units from vessel
            PartResourceDefinition transportCredits = PartResourceLibrary.Instance.GetDefinition("TransportCredits");

            vessel.ExchangeResources(transportCredits.id, -resourceUnits);

            return true;
        }

        public static void Log(this ConfigNode node, string marker = "[MKS] ")
        {
            Debug.Log(marker+node.name);
            foreach (ConfigNode node1 in node.nodes)
            {
                node1.Log(marker+"  ");
            }
            foreach (ConfigNode.Value value in node.values)
            {
                Debug.Log(marker+value.name+" : "+value.value);
            }
        }

        public static void Log(this Object obj, string msg)
        {
            Debug.Log("[MKS] ["+(new StackTrace()).GetFrame(1).GetMethod().Name+"] "+msg);
        }

        public static double ExchangeResources(this Vessel vessel, int resourceID, double amount)
        {
            return vessel.ExchangeResources(PartResourceLibrary.Instance.GetDefinition(resourceID),amount);
        }

        public static double ExchangeResources(this Vessel vessel, string resourceName, double amount)
        {
            return vessel.ExchangeResources(PartResourceLibrary.Instance.GetDefinition(resourceName), amount);
        }

        public static double ExchangeResources(this Vessel vessel, PartResourceDefinition resource, double amount)
        {
            vessel.Log("ExchangeResource: vessel="+vessel.name+", resource="+resource.name+", amount="+amount);

            if (Math.Abs(amount) < 0.000001)
                return amount;

            double amountExchanged = 0;
            bool done = false;
            
            if (vessel.packed && !vessel.loaded)
            {
                vessel.Log("ExchangeResource:packed");

                foreach (ProtoPartSnapshot p in vessel.protoVessel.protoPartSnapshots)
                {
                    if (done)
                        break;

                    foreach (ProtoPartResourceSnapshot r in p.resources)
                    {
                        if (done)
                            break;

                        if (r.resourceName != resource.name)
                            continue;

                        double amountInPart = Convert.ToDouble(r.amount);
                        if (amount < 0)
                        {
                            if (amountInPart < Math.Abs(amount - amountExchanged))
                            {
                                amountExchanged -= amountInPart;
                                r.amount = 0;
                            }
                            else
                            {
                                r.amount = amountInPart + (amount - amountExchanged);
                                amountExchanged = amount;
                                done = true;
                            }
                        }
                        else
                        {
                            double max = Convert.ToDouble(r.maxAmount);
                            if (max - amountInPart < amount - amountExchanged)
                            {
                                amountExchanged += (max - amountInPart);
                                r.amount = max;
                            }
                            else
                            {
                                r.amount = amountInPart + (amount - amountExchanged);
                                amountExchanged = amount;
                                done = true;
                            }
                        }
                    }
                }
            }
            else
            {
                vessel.Log("ExchangeResource:loaded");

                foreach (Part p in vessel.parts)
                {
                    if (done)
                        break;

                    int rCount = p.Resources.Count;
                    for (int i = 0; i < rCount; ++i)
                    {
                        if (done)
                            break;

                        var r = p.Resources[i];
                        if (r.info.id != resource.id)
                            continue;

                        if (amount < 0)
                        {
                            if (r.amount < Math.Abs(amount - amountExchanged))
                            {
                                amountExchanged -= r.amount;
                                r.amount = 0;
                            }
                            else
                            {
                                r.amount += (amount - amountExchanged);
                                amountExchanged = amount;
                                done = true;
                            }
                        }
                        else
                        {
                            if (r.maxAmount - r.amount < amount - amountExchanged)
                            {
                                amountExchanged += (r.maxAmount - r.amount);
                                r.amount = r.maxAmount;
                            }
                            else
                            {
                                r.amount += (amount - amountExchanged);
                                amountExchanged = amount;
                                done = true;
                            }
                        }
                    }
                }
            }

            return amountExchanged;
        }

        public static IEnumerable<ModuleResourceConverter> GetActiveConverters(this Vessel vessel)
        {
            return vessel.GetConverters().Where(mod => mod.IsActivated);
        }
        
        public static IEnumerable<ModuleResourceConverter> GetConverters(this Vessel vessel)
        {
            var mksParts = vessel.parts.Where(p => p.Modules.Contains("MKSModule"));
            return mksParts.SelectMany(part => part.Modules.OfType<ModuleResourceConverter>());
        }

        public static IEnumerable<Part> GetConverterParts(this Vessel vessel)
        {
            var mksParts = vessel.parts.Where(p => p.Modules.Contains("MKSModule"));
            return mksParts.Where(part => part.FindModuleImplementing<ModuleResourceConverter>() != null);
        }

        //provided by sehe at http://stackoverflow.com/questions/5489987/linq-full-outer-join
        internal static IList<TR> FullOuterJoin<TA, TB, TK, TR>(
            this IEnumerable<TA> a,
            IEnumerable<TB> b,
            Func<TA, TK> selectKeyA,
            Func<TB, TK> selectKeyB,
            Func<TA, TB, TK, TR> projection,
            TA defaultA = default(TA),
            TB defaultB = default(TB),
            IEqualityComparer<TK> cmp = null)
        {
            cmp = cmp ?? EqualityComparer<TK>.Default;
            var alookup = a.ToLookup(selectKeyA, cmp);
            var blookup = b.ToLookup(selectKeyB, cmp);

            var keys = new HashSet<TK>(alookup.Select(p => p.Key), cmp);
            keys.UnionWith(blookup.Select(p => p.Key));

            var join = from key in keys
                       from xa in alookup[key].DefaultIfEmpty(defaultA)
                       from xb in blookup[key].DefaultIfEmpty(defaultB)
                       select projection(xa, xb, key);

            return join.ToList();
        }
    }
}
