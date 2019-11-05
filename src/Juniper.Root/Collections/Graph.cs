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

        private bool dirty;

        private readonly List<NodeT> endPoints;
        private readonly Dictionary<string, NodeT> namedEndPoints;
        private readonly Network network;

        public Graph()
        {
            dirty = false;
            endPoints = new List<NodeT>();
            namedEndPoints = new Dictionary<string, NodeT>();
            network = new Network();
        }

        public Graph<NodeT> Clone()
        {
            var graph = new Graph<NodeT>();
            graph.dirty = true;
            graph.endPoints.AddRange(endPoints);
            foreach(var pair in namedEndPoints)
            {
                graph.namedEndPoints.Add(pair.Key, pair.Value);
            }
            foreach(var schedule in network.Values)
            {
                foreach (var route in schedule.Values)
                {
                    if(route.Count == 2)
                    {
                        graph.Connect(route.Start, route.End, route.Cost);
                    }
                }
            }
            return graph;
        }

        protected Graph(SerializationInfo info, StreamingContext context)
        {
            dirty = true;
            endPoints = info.GetList<NodeT>(nameof(endPoints));
            namedEndPoints = info.GetValue<Dictionary<string, NodeT>>(nameof(namedEndPoints));

            network = new Network();
            var routes = info.GetValue<Route<NodeT>[]>(nameof(network));
            foreach (var route in routes)
            {
                if (!network.ContainsKey(route.Start))
                {
                    network[route.Start] = new Schedule();
                }

                network[route.Start][route.End] = route;
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            Compress();

            info.AddList(nameof(endPoints), endPoints);
            info.AddValue(nameof(namedEndPoints), namedEndPoints);
            var routes = (from x in network
                          from y in x.Value
                          select y.Value)
                .ToArray();
            info.AddValue(nameof(network), routes);
        }

        public void Connect(NodeT startPoint, NodeT endPoint, float cost)
        {
            if (!startPoint.Equals(endPoint))
            {
                dirty = true;
                var nextRoute = new Route<NodeT>(startPoint, endPoint, cost);
                Remove(startPoint, endPoint);
                Add(nextRoute);
            }
        }

        private void Add(Route<NodeT> nextRoute)
        {
            AddSingle(nextRoute);
            AddSingle(~nextRoute);
        }

        private void AddSingle(Route<NodeT> route)
        {
            if (!network.ContainsKey(route.Start))
            {
                network[route.Start] = new Schedule();
            }
            network[route.Start][route.End] = route;
        }

        public bool Exists(NodeT node)
        {
            return network.ContainsKey(node);
        }

        public bool Exists(NodeT startPoint, NodeT endPoint)
        {
            return Exists(startPoint)
                && network[startPoint].ContainsKey(endPoint);
        }

        public IReadOnlyList<NodeT> EndPoints
        {
            get
            {
                return endPoints;
            }
        }

        public void AddEndPoint(NodeT endPoint)
        {
            if (!endPoints.Contains(endPoint))
            {
                dirty = true;
                endPoints.Add(endPoint);
            }
        }

        public void RemoveEndPoint(NodeT endPoint)
        {
            if (endPoints.Contains(endPoint))
            {
                dirty = true;
                endPoints.Remove(endPoint);
            }
        }

        public IReadOnlyDictionary<string, NodeT> NamedEndPoints
        {
            get
            {
                return namedEndPoints;
            }
        }

        public string GetEndPointName(NodeT node)
        {
            foreach (var endPoint in namedEndPoints)
            {
                if (endPoint.Value.CompareTo(node) == 0)
                {
                    return endPoint.Key;
                }
            }

            return null;
        }

        public void SetEndPointName(NodeT endPoint, string name)
        {
            namedEndPoints[name] = endPoint;
        }

        public void RemoveEndPointName(string name)
        {
            namedEndPoints.Remove(name);
        }

        public NodeT GetNamedEndPoint(string name)
        {
            if (namedEndPoints.ContainsKey(name))
            {
                return namedEndPoints[name];
            }
            else
            {
                return default;
            }
        }

        private void RemoveSingle(NodeT startPoint, NodeT endPoint, bool removeRaw)
        {
            if (Exists(startPoint, endPoint))
            {
                var toRemove = new List<Route<NodeT>>();
                foreach (var schedule in network.Values)
                {
                    foreach (var route in schedule.Values)
                    {
                        if (route.Contains(startPoint)
                            && route.Contains(endPoint)
                            && (route.Count > 2
                                || removeRaw))
                        {
                            toRemove.Add(route);
                        }
                    }
                }

                dirty = toRemove.Count > 0;

                foreach (var route in toRemove)
                {
                    network[route.Start].Remove(route.End);
                }

                if(network[startPoint].Count == 0)
                {
                    dirty = true;
                    network.Remove(startPoint);
                    RemoveEndPoint(startPoint);
                }
            }
        }

        public void Remove(NodeT startPoint, NodeT endPoint, bool removeRaw = false)
        {
            if (!startPoint.Equals(endPoint))
            {
                RemoveSingle(startPoint, endPoint, removeRaw);
                RemoveSingle(endPoint, startPoint, removeRaw);
            }
        }

        public void Disconnect(NodeT startPoint, NodeT endPoint)
        {
            if (!startPoint.Equals(endPoint))
            {
                var route = GetRoute(startPoint, endPoint);
                if (route != null)
                {
                    Remove(route.Start, route.End, true);
                }
            }
        }

        public void Remove(NodeT node)
        {
            if (Exists(node))
            {
                var toRemove = network[node].Values.ToArray();
                foreach (var route in toRemove)
                {
                    Remove(route.Start, route.End, true);
                }
            }
        }

        /// <summary>
        /// Deletes all routes that have more than 2 nodes. 2-node routes
        /// are "raw connections" that define the overall graph. Without them,
        /// the structure cannot be rebuilt.
        /// </summary>
        public void Compress()
        {
            var longRoutes = (from schedule in network.Values
                              from route in schedule.Values
                              where route.Count > 2
                              select route)
                            .ToArray();

            foreach (var route in longRoutes)
            {
                Remove(route.Start, route.End, false);
            }

            var toRemove = new List<string>();
            foreach(var name in namedEndPoints.Keys)
            {
                if (!endPoints.Contains(namedEndPoints[name]))
                {
                    toRemove.Add(name);
                }
            }

            foreach(var name in toRemove)
            {
                namedEndPoints.Remove(name);
            }
        }

        public void Solve()
        {
            if (dirty)
            {
                Compress();

                var q = new Queue<Route<NodeT>>(
                    from endPoint in endPoints
                    where network.ContainsKey(endPoint)
                    let schedule = network[endPoint]
                    from route in schedule.Values
                    select route);

                while (q.Count > 0)
                {
                    var route = q.Dequeue();
                    foreach (var extension in network[route.End].Values)
                    {
                        var nextCost = route.Cost + extension.Cost;
                        var nextCount = route.Count + extension.Count - 1;
                        var curRoute = GetRoute(route.Start, extension.End);
                        if (curRoute == null
                            || nextCost < curRoute.Cost
                            || nextCost == curRoute.Cost
                                && nextCount < curRoute.Count)
                        {
                            var nextRoute = route + extension;
                            Add(nextRoute);
                            q.Enqueue(nextRoute);
                        }
                    }
                }

                dirty = false;
            }
        }

        public IRoute<NodeT> GetRoute(NodeT startPoint, NodeT endPoint)
        {
            return Exists(startPoint, endPoint)
                ? network[startPoint][endPoint]
                : default;
        }

        public IEnumerable<NodeT> GetExits(NodeT startPoint)
        {
            if (Exists(startPoint))
            {
                foreach (var route in network[startPoint].Values)
                {
                    if (route.Count == 2)
                    {
                        yield return route.End;
                    }
                }
            }
        }
    }
}
