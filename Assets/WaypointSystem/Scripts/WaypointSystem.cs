using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ASWS {
    public enum ConnectionType { bezier, line }
    public class WaypointSystem : MonoBehaviour, ISerializationCallbackReceiver
    {
        public static WaypointSystem Instance;


        public float nsapDistance = 2;
        public int WorldSize;
        public bool autoset = false;
        public bool freeMoveHandles = false;
        public bool GraphUpdated = false;
        public ConnectionType curveType;
        public void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(this);
            else
                Instance = this;

        }

        public List<WaypointLoop> loops;

        public Dictionary<Waypoint,List<WaypointLink>> waypointgraph;
        [SerializeField]
        List<graphValueSaver> graphSaver;
        public void PrintBranch()
        {
            if (waypointgraph != null)
            {
                string fulldata = "";
                foreach(var p in waypointgraph)
                {
                    string data="[";
                    foreach(var w in p.Value)
                    {
                        data += w.next.name+",";
                    }
                    data += "]";
                    fulldata+=p.Key.name +": "+data+"\n";
                }
                Debug.Log(fulldata);
            }
        }

        void Start()
        {
            
        }

      
#if UNITY_EDITOR
        
        public void DrawBranch()
        {
           
        }
#endif
        public bool checkBranchDest()
        {

            return false;
        }
        public WaypointLoop AddLoop(Vector3 position)
        {
            WaypointLoop newloop = new GameObject("waypointLoop" + loops.Count.ToString(), typeof(WaypointLoop)).GetComponent<WaypointLoop>();
            newloop.transform.position = position;
            newloop.Setup(this);
            newloop.transform.parent = transform;
            loops.Add(newloop);
            newloop.gameObject.hideFlags |= HideFlags.HideInHierarchy;
           
            return newloop;
        }

   
        public void ScanLoops()
        {
            loops = new List<WaypointLoop>(GetComponentsInChildren<WaypointLoop>());
        }

        public void markEnEx()
        {
            //remove previous Marks
            for (int i = 0; i < loops.Count; i++)
            {
                for (int j = 0; j < loops[i].waypoints.Count; j++)
                {
                    loops[i].waypoints[j].enterance = false;
                    loops[i].waypoints[j].exit = false;
                }
            }

            for (int i = 0; i < loops.Count; i++)
            {
                for (int j = 0; j < loops[i].entrances.Count; j++)
                {
                    loops[i].entrances[j].enterance = true;
                }
                for (int j = 0; j < loops[i].exits.Count; j++)
                {
                    loops[i].exits[j].exit = true;
                }
            }

        }
        public void CreaeGraph()
        {
            GraphUpdated = true;
            markEnEx();
            waypointgraph = new Dictionary<Waypoint, List<WaypointLink>>();

            //create inLoop links
            for (int i = 0; i < loops.Count; i++)
            {
                var loop = loops[i];
                for (int en = 0; en < loop.entrances.Count; en++)
                {
                    var curr = loop.entrances[en];
                    var next = curr.Next;
                    float commulativeDistance = 0;
                    List<WaypointLink> links = new List<WaypointLink>();
                    while (next != null && next != curr)
                    {
                        float len = Bezier.CalculateApproxCurveLength(curr.transform.position, curr.HandleB.position, next.HandleA.position, next.transform.position);
                        commulativeDistance += len;
                        if (next.exit == true)
                        {
                            WaypointLink link = new WaypointLink(next, (next.transform.position - curr.transform.position).magnitude, commulativeDistance);
                            links.Add(link);
                        }
                        next = next.Next;
                    }
                    if (links.Count > 0)
                        waypointgraph.Add(curr, links);
                }
                for (int ex = 0; ex < loop.exits.Count; ex++)
                {
                    var curr = loop.exits[ex];
                    if (curr == null) continue;
                    var next = curr.Next;
                    float commulativeDistance = 0;

                    List<WaypointLink> links = new List<WaypointLink>();
                    while (next != null && next != curr)
                    {
                        float len = Bezier.CalculateApproxCurveLength(curr.transform.position, curr.HandleB.position, next.HandleA.position, next.transform.position);
                        commulativeDistance += len;
                        if (next.exit == true)
                        {
                            WaypointLink link = new WaypointLink(next, (next.transform.position - curr.transform.position).magnitude, commulativeDistance);
                            links.Add(link);
                        }
                        next = next.Next;
                    }
                    if(curr.Branches!=null)
                    for (int j = 0; j < curr.Branches.Count; j++)
                    {

                        next = curr.Branches[j];
                        float len = Bezier.CalculateApproxCurveLength(curr.transform.position, curr.HandleB.position, next.HandleA.position, next.transform.position);
                        links.Add(new WaypointLink(next, (curr.transform.position - next.transform.position).magnitude, len));
                    }
                    if (!waypointgraph.ContainsKey(curr))
                    {
                        waypointgraph.Add(curr, links);
                    }
                    else
                    {
                        waypointgraph[curr].AddRange(links);

                    }
                }

            }


            PrintBranch();
            saveBranchData(); 
        }
        public List<Waypoint> GetpathBetweenPoints(Waypoint source, Waypoint dest)
        {
            if (source.parent != dest.parent) return null;
            List<Waypoint> pathpoints = new List<Waypoint>();
            var start = source;
            pathpoints.Add(start);
            start = start.Next;

            while (true)
            {
                if (start == source ||start==null)
                    return null;
                pathpoints.Add(start);
                if (start == dest)
                    return pathpoints;
                start = start.Next;
            }
        }

        public List<Bezier.BezierSegment> GetPathpoints(Waypoint source, Waypoint dest , bool Bezier=true)
        {
            
            if (Bezier)
                return GetPathpointsBezier(source, dest);
            else
                return GetPathStraight(source, dest);

            

        }
        public List<Bezier.BezierSegment> GetPathStraight(Waypoint source,Waypoint dest)
        {
            var points = EvalGraph(source, dest);
            if (points == null) return null;

            List<Bezier.BezierSegment> path = new List<Bezier.BezierSegment>();
            for (int i = 0; i < points.Count-1; i++)
            {
                path.Add(new Bezier.BezierSegment {A= points[i].transform.position,B = points[i].transform.position,C = points[i+1].transform.position, D = points[i + 1].transform.position ,NormalA= points[i].normalDir, NormalD = points[i+1].normalDir });
            }
            return path;
        }


        public List<Bezier.BezierSegment> TraverseLoopSegments(int LoopIndex, int startPoint,int LastPoint)
        {
            
            if (loops != null && LoopIndex < loops.Count && LoopIndex>=0)
            {
                Debug.Log("get Path");
                
                var points= loops[LoopIndex].GetPathWayPoints(startPoint, LastPoint);
                
                return GetPathSegments(points);
            }
            return null;
        }
        public List<Bezier.BezierSegment> GetPathSegments(List<Waypoint> points)
        {
            List<Bezier.BezierSegment> segments = new List<Bezier.BezierSegment>();
            for (int i = 0; i < points.Count - 1; i++)
            {
                segments.Add(new Bezier.BezierSegment { A = points[i].transform.position, B = points[i].HandleB.position, C = points[i + 1].HandleA.position, D = points[i + 1].transform.position, NormalA = points[i].normalDir, NormalD = points[i + 1].normalDir });
            }

            return segments;
        }
        public List<Vector3> TraverseLoopPoints(int LoopIndex, int startPoint, int LastPoint)
        {
            if (loops != null && LoopIndex < loops.Count)
            {
                return loops[LoopIndex].GetPathPointsFromPoint(startPoint, LastPoint);
            }
            return null;
        }
        
        public List<Bezier.BezierSegment> GetPathpointsBezier(Waypoint source, Waypoint dest)
        {
            var points = EvalGraph(source,dest);
            if (points == null) return null;
            return GetPathSegments(points);
        }



        public List<Waypoint> EvalGraph(Waypoint source ,Waypoint dest)
        {
            if (!GraphUpdated) Debug.LogWarning("Graph is not Updated, this may lead to unwanted behavior Please Recompile the your System (WaypointSystem.CreateGraph();)");
            if (source.parent == dest.parent)
                return GetpathBetweenPoints(source, dest);

            var start = source;
            var end = dest;
            List<Waypoint> pathpoints=new List<Waypoint>();
            //get nearst branch point
            while (start.exit == false )
            {
                
                pathpoints.Add(start);
                start = start.Next;
                if (start == source || start == null)
                    return null;
            }
            while (end.enterance == false )
            {
                end = end.previous;
                if (end == dest || end == null)
                    return null;
            }
            //get path
            List<Node> points = new List<Node>();
            List<Node> closed = new List<Node>();
            points.Add(new Node { point=start,f= Vector3.Distance(start.transform.position, end.transform.position), h=Vector3.Distance(start.transform.position,end.transform.position),g=0});
            Node ReachedEnd=null;
            while (points.Count > 0)
            {
                //Debug.Log("opened:  "+points[0].point.name+" parent: "+ points[0].point.parent.name);
                var p = points[0];
                points.RemoveAt(0);
                var links = waypointgraph[p.point];
                for(int i = 0; i < links.Count; i++)
                {

                    //Debug.Log(points.Count);
                    var n = new Node();
                    n.point = links[i].next;

                    if(n.point == end)
                    {
                        n.parent = p;

                        ReachedEnd = n;
                        goto finished;
                    }
                    float g = p.g + links[i].CurveDistance;
                    float h = Vector3.Distance(n.point.transform.position, end.transform.position);
                    float f = g + h;

                    //Debug.Log("discover: " + n.point.name + " loop : " + n.point.parent.name + " factors: g=" + g + ",h=" + h + ",f=" + f);
                    int o = contains(points, n);
                    int c = contains(closed, n);
                    if (o==-1 && c==-1)
                    {
                        n.g = g;
                        n.f = f;
                        n.h = h;
                        n.parent = p;
                        bool inserted = false;
                        for (int j = 0; j < points.Count; j++)
                        {
                            if (points[j].f > n.f)
                            {
                                inserted = true;
                                points.Insert(j, n);
                                break;
                            }
                        }
                        if (!inserted)
                            points.Add(n);

                    }
                    else
                    {
                        
                        if(n.f <f)
                        {
                            n.parent = p;
                            n.f = f;
                            n.h = h;
                            n.g = g;
                            if(c!=-1)
                            {

                                bool inserted = false;

                                for (int j = 0; j < points.Count; j++)
                                {
                                    if (points[j].f > n.f)
                                    {
                                        inserted = true;
                                        points.Insert(j, n);
                                        break;
                                    }
                                }
                                if (!inserted)
                                    points.Add(n);
                                //closed.RemoveAt(c);
                            }
                        }
                    }
                    


                }
                closed.Add(p);

            }
            finished:
            var point = ReachedEnd;
            List<Waypoint> revPoints= new List<Waypoint>();
            revPoints.Add(point.point);
            int ii = 0;
            while (point.point != start )
            {
                point = point.parent;
                revPoints.Insert(0, point.point);
                if (ii >= waypointgraph.Count)
                    return null;
            }
            pathpoints.AddRange(revPoints);
            Waypoint pd = ReachedEnd.point;
            while(pd!= dest)
            {
                pathpoints.Add(pd);
                pd = pd.Next;
            }
           

            return pathpoints;
        }


        public int contains(List<Node> nodes,Node n )
        {
            int index = -1;
            for (int i = 0;  i < nodes.Count; i++)
            {
                if (nodes[i].point == n.point)
                    return i;
            }
            return index;
        }

        /*
         * make an empty list C of closed nodes
make a list O of open nodes and their respective f values containing the start node
while O isn't empty:
    pick a node n from O with the best value for f
    if n is target:
        return solution
    for every m which is a neighbor of n:
        if (m is not in C) and (m is not in O):
            add m to O, set n as m's parent
            calculate g(m) and f(m) and save them
        else:
            if f(m) from last iteration is better than f(m) from this iteration:
                set n as m's parent
                update g(m) and f(m)
                if m is in C:
                    move m to O
    move n from O to C

return that there's no solution
         */
        public void OnBeforeSerialize()
        {
            
        } 
        public void saveBranchData()
        {
            graphSaver = new List<graphValueSaver>();
            foreach(var e in waypointgraph)
            {
                graphSaver.Add(new graphValueSaver(e.Key, e.Value));
            }
        }

        public void OnAfterDeserialize()
        {
            if (waypointgraph == null) waypointgraph = new Dictionary<Waypoint, List<WaypointLink>>();


            for(int i=0;i< graphSaver.Count; i++)
            {
                Waypoint key; List< WaypointLink > val;
                (key) = graphSaver[i].point;
                val = graphSaver[i].links;
                waypointgraph.Add(key,val);
            }
        }
        

        [Serializable]
        private class graphValueSaver
        {
            public graphValueSaver(Waypoint npoint, List<WaypointLink> nlinks)
            {
                point = npoint;
                links = nlinks;
            }
            public Waypoint point;
            public List<WaypointLink> links;
        }
    }
   
    [Serializable]
    public class WaypointLink
    {
       public  WaypointLink(Waypoint p , float d ,float cd)
        {
            next = p;
            absoluteDistance = d;
            CurveDistance = cd;
        } 
        public Waypoint next;
        public float absoluteDistance, CurveDistance;
    }

    public class Node
    {
        public Node parent;
        public Waypoint point;
        public float f, g, h;
    }

   
    


}