using BepInEx.Logging;
using KSP.Game;
using KSP.Sim;
using System;
using System.Collections.Generic;
using System.Text;

namespace Planety
{
    internal class AddContent
    {
        public static SerializedGalaxyDefinition AlterSerializedGalaxyDefinition(SerializedGalaxyDefinition galaxy)
        {
            ContentLoader.GetContent();

            GameManager.Instance.Game.CelestialBodies.InterstellarNeighborhood = GameManager.Instance.Game.CelestialBodies.InterstellarNeighborhood ?? new() { children = new() };

            for (int i = 0; i < galaxy.CelestialBodies.Count; i++)
            {
                var body = galaxy.CelestialBodies[i];
                var pbody = ContentLoader.GetBody(body.GUID);

                if (pbody.removed)
                {
                    galaxy.CelestialBodies.RemoveAt(i);
                    i--;
                }

                else if (pbody.dirty)
                {
                    if (pbody.orbitColor.HasValue)
                    {
                        body.OrbiterProperties = new SerializedOribiterDefinition()
                        {
                            orbitColor = pbody.orbitColor.Value,
                            nodeColor = pbody.nodeColor.Value
                        };
                    }

                    if (pbody.orbit == null)
                    {
                        body.OrbitProperties = new SerializedOrbitProperties();
                        if (pbody.galacticPosition.HasValue)
                            GameManager.Instance.Game.CelestialBodies.InterstellarNeighborhood.children.Add(new() { friendlyName = pbody.id, offset = pbody.galacticPosition.Value });
                    }
                    else
                    {
                        GameManager.Instance.Game.CelestialBodies.InterstellarNeighborhood.children.RemoveAll(s => s.friendlyName == pbody.id);
                        body.OrbitProperties = new SerializedOrbitProperties()
                        {
                            argumentOfPeriapsis = pbody.orbit.argumentOfPeriapsis,
                            eccentricity = pbody.orbit.eccentricity,
                            epoch = pbody.orbit.epoch,
                            inclination = pbody.orbit.inclination,
                            longitudeOfAscendingNode = pbody.orbit.longitudeOfAscendingNode,
                            meanAnomalyAtEpoch = pbody.orbit.meanAnomalyAtEpoch,
                            referenceBodyGuid = pbody.orbit.parentBody,
                            semiMajorAxis = pbody.orbit.semiMajorAxis.Value
                        };
                    }
                }
            }

            foreach (CelestialBodyData body in ContentLoader.GetContent())
            {
                if (!body.removed)
                {
                    galaxy.CelestialBodies.Add(ToSerializedCelestialBody(body));
                    if (body.galacticPosition.HasValue)
                        GameManager.Instance.Game.CelestialBodies.InterstellarNeighborhood.children.Add(new() { friendlyName = body.id, offset = body.galacticPosition.Value });
                }
            }

            bool dirty = true;
            while (dirty)
            {
                dirty = false;
                for (int i = 0; i < galaxy.CelestialBodies.Count; i++)
                {
                    var this_body = galaxy.CelestialBodies[i];
                    int parent_body_index = -1;

                    if (string.IsNullOrEmpty(this_body.OrbitProperties.referenceBodyGuid))
                        continue;

                    for (int j = 0; j < galaxy.CelestialBodies.Count; j++)
                    {
                        if (galaxy.CelestialBodies[j].GUID == this_body.OrbitProperties.referenceBodyGuid)
                        {
                            parent_body_index = j;
                            break;
                        }
                    }

                    if (parent_body_index >= i)
                    {
                        galaxy.CelestialBodies.RemoveAt(i);
                        galaxy.CelestialBodies.Insert(parent_body_index, this_body);
                        dirty = true;
                        break;
                    }
                }
            }

            return galaxy;
        }

        static SerializedCelestialBody ToSerializedCelestialBody(CelestialBodyData data)
        {
            return new SerializedCelestialBody()
            {
                GUID = data.id,
                referenceBodyGuid = data.orbit?.parentBody,
                OrbitProperties = data.orbit == null ? new SerializedOrbitProperties() : new SerializedOrbitProperties()
                {
                    referenceBodyGuid = data.orbit.parentBody,
                    inclination = data.orbit.inclination,
                    eccentricity = data.orbit.eccentricity,
                    semiMajorAxis = data.orbit.semiMajorAxis.Value,
                    longitudeOfAscendingNode = data.orbit.longitudeOfAscendingNode,
                    argumentOfPeriapsis = data.orbit.argumentOfPeriapsis,
                    meanAnomalyAtEpoch = data.orbit.meanAnomalyAtEpoch,
                    epoch = data.orbit.epoch
                },
                OrbiterProperties = new SerializedOribiterDefinition()
                {
                    orbitColor = data.orbitColor.Value,
                    nodeColor = data.nodeColor.Value
                }
            };
        }
    }
}
