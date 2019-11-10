using Juniper.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace Juniper.Collections
{
    [Serializable]
    public class Graph<NodeT> : ISerializable
        where NodeT : IComparable<NodeT>
    {
        public static Graph<NodeT> Load(Stream stream)
        {
            var json = new JsonFactory<Graph<NodeT>>();
            return json.Deserialize(stream);
        }

        public static Graph<NodeT> Load(FileInfo file)
        {
            using (var stream = file.OpenRead())
            {
                return Load(stream);
            }
        }

        public static Graph<NodeT> Load(string path)
        {
            return Load(new FileInfo(path));
        }

        private class Schedule : Dictionary<NodeT, Route<NodeT>> { }

        private class Network : Dictionary<NodeT, Schedule> { }

        private readonly Dictionary<string, NodeT> namedNodes;
        private readonly Dictionary<NodeT, string> nodeNames;
        private readonly Network network;

        private bool dirty;

        public Graph()
        {
            namedNodes = new Dictionary<string, NodeT>();
            nodeNames = new Dictionary<NodeT, string>();
            network = new Network();
            dirty = false;
        }

        public Graph<NodeT> Clone()
        {
            var graph = new Graph<NodeT>();
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

        private bool saveAllPaths;

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Serialize only the minimal information that we need to restore
            // the graph.
            info.AddValue(nameof(namedNodes), namedNodes);
            if (saveAllPaths)
            {
                info.AddValue(nameof(network), Routes);
            }
            else
            {
                info.AddValue(nameof(network), Connections.Distinct());
            }
        }

        public void Save(Stream stream, bool saveAll = false)
        {
            saveAllPaths = saveAll;
            var json = new JsonFactory<Graph<NodeT>>();
            json.Serialize(stream, this);
        }

        public void Save(FileInfo file, bool saveAll = false)
        {
            using(var stream = file.Open(FileMode.Create, FileAccess.Write, FileShare.None))
            {
                Save(stream, saveAll);
            }
        }

        public void Save(string path, bool saveAll = false)
        {
            Save(new FileInfo(path), saveAll);
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
            RemoveRoutes(from connect in GetConnections(start)
                         where connect.End.Equals(end)
                         from route in Routes
                         where route.Contains(connect)
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

        private IEnumerable<Route<NodeT>> GetConnections(NodeT node)
        {
            return from route in GetRoutes(node)
                   where route.IsConnection
                   select route;
        }

        public IEnumerable<NodeT> GetExits(NodeT node)
        {
            return from route in GetConnections(node)
                   select route.End;
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
                var stack = new Stack<Route<NodeT>>(Routes);

                while (stack.Count > 0)
                {
                    var route = stack.Pop();
                    foreach (var next
                        in from extension in GetConnections(route.End)
                           where route.CanConnectTo(extension)
                           let curRoute = GetRoute(route.Start, extension.End)
                           where curRoute is null
                            || curRoute.Cost > route.Cost + extension.Cost
                           select route + extension)
                    {
                        AddRoute(next);
                        stack.Push(next);
                    }
                }

                dirty = false;
            }
        }
    }
}
