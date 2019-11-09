using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Juniper.Collections
{
    [Serializable]
    public class Graph<NodeT> : ISerializable
        where NodeT : IComparable<NodeT>
    {
        private class Schedule : Dictionary<NodeT, Route<NodeT>> { }

        private class Network : Dictionary<NodeT, Schedule> { }

        private readonly List<NodeT> endPoints;
        private readonly Dictionary<string, NodeT> namedNodes;
        private readonly Dictionary<NodeT, string> nodeNames;
        private readonly Network network;

        private bool dirty;

        public Graph()
        {
            endPoints = new List<NodeT>();
            namedNodes = new Dictionary<string, NodeT>();
            nodeNames = new Dictionary<NodeT, string>();
            network = new Network();
            dirty = false;
        }

        public Graph<NodeT> Clone()
        {
            var graph = new Graph<NodeT>();
            graph.endPoints.AddRange(endPoints);
            foreach (var pair in namedNodes)
            {
                graph.namedNodes.Add(pair.Key, pair.Value);
                graph.nodeNames.Add(pair.Value, pair.Key);
            }

            foreach (var route in Connections)
            {
                graph.AddRoute(route);
            }

            graph.dirty = true;

            return graph;
        }

        protected Graph(SerializationInfo info, StreamingContext context)
        {
            endPoints = info.GetList<NodeT>(nameof(endPoints));
            foreach (var pair in info)
            {
                if (pair.Name == nameof(namedNodes) || pair.Name == "namedEndPoints")
                {
                    namedNodes = info.GetValue<Dictionary<string, NodeT>>(pair.Name);
                }
            }

            nodeNames = namedNodes.Invert();

            network = new Network();

            var routes = info.GetValue<Route<NodeT>[]>(nameof(network));
            foreach (var route in routes)
            {
                if (route.IsValid)
                {
                    FillNetworks(route);
                }
            }

            dirty = true;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Serialize only the minimal information that we need to restore
            // the graph.
            info.AddList(nameof(endPoints), endPoints);
            info.AddValue(nameof(namedEndPoints), namedEndPoints);
            var routes = (from route in Connections
                          select route)
                        .Distinct()
                        .ToArray();
            info.AddValue(nameof(network), routes);
        }

        public IReadOnlyList<NodeT> EndPoints
        {
            get
            {
                return endPoints;
            }
        }

        public void Connect(NodeT startPoint, NodeT endPoint, float cost)
        {
            if (!startPoint.Equals(endPoint))
            {
                AddRoute(new Route<NodeT>(cost, startPoint, endPoint));
            }
        }

        private void AddRoute(Route<NodeT> route)
        {
            var curRoute = GetRoute(route.Start, route.End);
            if (route != curRoute)
            {
                if (curRoute != null)
                {
                    RemoveRoutes(from r in Routes
                                 where r.Contains(curRoute)
                                 select r);
                }

                FillNetworks(route);
            }
        }

        private void FillNetworks(Route<NodeT> route)
        {
            FillNetwork(route);
            FillNetwork(~route);
        }

        private void FillNetwork(Route<NodeT> route)
        {
            if (!network.ContainsKey(route.Start))
            {
                network[route.Start] = new Schedule();
            }

            if (!network[route.Start].ContainsKey(route.End)
                || route != network[route.Start][route.End])
            {
                dirty = true;
                network[route.Start][route.End] = route;
            }
        }

        public IEnumerable<NodeT> Nodes
        {
            get
            {
                return network.Keys;
            }
        }

        public IEnumerable<Route<NodeT>> Routes
        {
            get
            {
                return from schedule in network.Values
                       from r in schedule.Values
                       select r;
            }
        }

        public IEnumerable<Route<NodeT>> Connections
        {
            get
            {
                return from route in Routes
                       where route.IsConnection
                       select route;
            }
        }

        public IEnumerable<Route<NodeT>> Paths
        {
            get
            {
                return from route in Routes
                       where route.IsPath
                       select route;
            }
        }

        public void Disconnect(NodeT start, NodeT end)
        {
            RemoveRoutes(from connect in Connections
                         where connect.Contains(start)
                            && connect.Contains(end)
                         from route in Routes
                         where route == connect
                            || ((route.Contains(connect.Start)
                                    || route.Contains(connect.End))
                                && route.IntersectsWith(connect))
                         select route);
        }

        public void Remove(NodeT node)
        {
            RemoveRoutes(from route in Routes
                         where route.Contains(node)
                         select route);
        }

        private void RemoveRoutes(IEnumerable<Route<NodeT>> toRemove)
        {
            var arr = toRemove.ToArray();
            dirty |= arr.Length > 0;
            foreach (var r in arr)
            {
                network[r.Start].Remove(r.End);
                if (network[r.Start].Count == 0)
                {
                    network.Remove(r.Start);
                }
            }
        }

        public bool NodeExists(NodeT node)
        {
            return network.ContainsKey(node);
        }

        public bool RouteExists(NodeT startPoint, NodeT endPoint)
        {
            return !startPoint.Equals(endPoint)
                && NodeExists(startPoint)
                && network[startPoint].ContainsKey(endPoint);
        }

        public Route<NodeT> GetRoute(NodeT startPoint, NodeT endPoint)
        {
            return RouteExists(startPoint, endPoint)
                ? network[startPoint][endPoint]
                : default;
        }

        public IEnumerable<Route<NodeT>> GetRoutes(NodeT node)
        {
            if (NodeExists(node))
            {
                foreach (var route in network[node].Values)
                {
                    yield return route;
                }
            }
        }

        public IEnumerable<NodeT> GetConnections(NodeT node)
        {
            foreach (var route in GetRoutes(node))
            {
                if (route.IsConnection)
                {
                    yield return route.End;
                }
            }
        }

        public void AddEndPoint(NodeT endPoint)
        {
            dirty |= endPoints.MaybeAdd(endPoint);
        }

        public void RemoveEndPoint(NodeT endPoint)
        {
            dirty |= endPoints.Remove(endPoint);
        }

        public IReadOnlyDictionary<string, NodeT> NamedNodes
        {
            get
            {
                return namedNodes;
            }
        }

        public IReadOnlyDictionary<NodeT, string> NodeNames
        {
            get
            {
                return nodeNames;
            }
        }

        public string GetNodeName(NodeT node)
        {
            return nodeNames.Get(node);
        }

        public NodeT GetNamedNode(string name)
        {
            return namedNodes.Get(name);
        }

        public void SetNodeName(NodeT endPoint, string name)
        {
            namedNodes[name] = endPoint;
            nodeNames[endPoint] = name;
        }

        public void RemoveNodeName(string name)
        {
            if (namedNodes.ContainsKey(name))
            {
                var endPoint = namedNodes[name];
                namedNodes.Remove(name);
                nodeNames.Remove(endPoint);
            }
        }

        public void Solve()
        {
            if (dirty)
            {
                RemoveRoutes(Paths);

                int added;
                do
                {
                    added = 0;

                    var stack = new Stack<Route<NodeT>>(
                        from endPoint in endPoints
                        from route in GetRoutes(endPoint)
                        select route);

                    var toAdd = new List<Route<NodeT>>();

                    while (stack.Count > 0)
                    {
                        var route = stack.Pop();
                        foreach (var extension in GetRoutes(route.End))
                        {
                            if (route.CanConnectTo(extension))
                            {
                                var nextCost = route.Cost + extension.Cost;
                                var nextCount = route.Count + extension.Count - 1;
                                var curRoute = GetRoute(route.Start, extension.End);
                                if (curRoute is null
                                    || curRoute.Cost > nextCost
                                    || curRoute.Cost == nextCost
                                        && curRoute.Count > nextCount)
                                {
                                    toAdd.MaybeAdd(route + extension);
                                }
                            }
                        }

                        foreach (var nextRoute in toAdd)
                        {
                            AddRoute(nextRoute);
                            stack.Push(nextRoute);
                        }

                        added += toAdd.Count;
                        toAdd.Clear();
                    }
                } while (added > 0);

                dirty = false;
            }
        }
    }
}
