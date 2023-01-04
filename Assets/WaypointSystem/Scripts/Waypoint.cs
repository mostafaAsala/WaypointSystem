using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ASWS { 


    enum WaypointType
    {
        Road,
        walker
    }


    public class Waypoint : MonoBehaviour
    {
        #region Variables

        private int _id;
        [Range(0, 360)]
        public float angle;
        public  float magnitude;
        public  Rect Bound;
        
        
        public WaypointLoop parent;
        
        public Waypoint Next;
        public Waypoint previous;
        public Waypoint rightLane;
        public Waypoint LeftLane;
        public Transform HandleA;
        public Transform HandleB;
        public bool LockHandles;
        private bool DrawLink = false;
        private float localYoffset;
        private float HandleAoffset;
        private float HandleBoffset;
        public List<Waypoint> Branches;
        public bool enterance=false, exit = false;
        public float normalangle = 0;
        public Vector3 normalDir;
        #region Gizmo
        [Range(0,1)]
        public static float gizmoSize=0.1f;
        private Color selectedColor = Color.blue;
        private Color normalColor = Color.green;
        private Color color;

        #endregion
        #endregion


        #region Setters and Getters

        public int id { get { return _id; } }


        #endregion
        // Start is called before the first frame update

        void Start()
        {
            if (Branches == null)
                Branches = new List<Waypoint>();
            var relativeposition = (transform.position - WaypointSystem.Instance.transform.position);
            _id = (int)(relativeposition.x + WaypointSystem.Instance.WorldSize * relativeposition.z);
        }
        public Vector3 getForwardVec()
        {
            return (HandleB.transform.position - transform.position).normalized;
        }

        public void SetFlat(bool flat)
        {
            if (flat) { 
                localYoffset = transform.localPosition.y;
                HandleAoffset = HandleA.localPosition.y;
                HandleBoffset = HandleB.localPosition.y;
                transform.localPosition = new Vector3(transform.localPosition.x,0, transform.localPosition.z);
                HandleA.localPosition = new Vector3(HandleA.localPosition.x, 0, HandleA.localPosition.z);
                HandleB.localPosition = new Vector3(HandleB.localPosition.x, 0, HandleB.localPosition.z);
            }
            else
            {
                transform.localPosition = new Vector3(transform.localPosition.x, localYoffset, transform.localPosition.z);
                HandleA.localPosition = new Vector3(HandleA.localPosition.x, HandleAoffset, HandleA.localPosition.z);
                HandleB.localPosition = new Vector3(HandleB.localPosition.x, HandleBoffset, HandleB.localPosition.z);
            
            }
        }
        public void saveYOffset()
        {
            localYoffset = transform.localPosition.y;
            HandleAoffset = HandleA.localPosition.y;
            HandleBoffset = HandleB.localPosition.y;
        }

        public void AddBranch(Waypoint branch)
        {

            if(parent==branch.parent)
            {
                Debug.Log("can't link two waypoints belonging to the same loop.");
                return;
            }
            if (Branches == null) Branches = new List<Waypoint>();
            if (!Branches.Contains(branch))
            {
                Branches.Add(branch);
                parent.exits.Add(this);
                branch.parent.entrances.Add(branch);
            }
            else
                Debug.Log("branch already exits");
        }

        public void CreateHandles()
        {
            if (HandleA == null)
            {
                HandleA = new GameObject("HandleA").transform;
                HandleA.parent = transform;
                HandleA.transform.position = transform.position - transform.forward;
            }
            if (HandleB == null)
            {
                HandleB = new GameObject("HandleB").transform;
                HandleB.parent = transform;

                HandleB.transform.position = transform.position + transform.forward;
            }
        }

        // Update is called once per frame
        void Update()
        {
        
        }
#if UNITY_EDITOR

        public void RecalculateID()
        {
            var relativeposition = (transform.position - WaypointSystem.Instance.transform.position);
            _id = (int)(relativeposition.x + WaypointSystem.Instance.WorldSize * relativeposition.z);
        }
#endif
        public Waypoint GetNextWaypoint()
        {
            return Next;
        }


        public void UpdateHandle( bool updateA)
        {
            if (!(HandleA && HandleB)) return;
            var point = updateA ? HandleB.transform : HandleA.transform;
            var otherpoint = updateA ? HandleA.transform : HandleB.transform;
            var distance = (otherpoint.position - transform.position).magnitude;
            var dir = -(point.position - transform.position).normalized;
            otherpoint.position = dir * distance + transform.position;
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawMesh(ASWS_Resources.instance.ArrowMesh,transform.position,transform.rotation);
            //Gizmos.DrawWireSphere(transform.position, gizmoSize);
            var color = normalColor;
            Gizmos.color = color;
            
            if(Next && DrawLink)
                Gizmos.DrawLine(transform.position, Next.transform.position);
            //DrawHandle(HandleA);
            //DrawHandle(HandleB);
        }
        public void DrawHandle(Transform h)
        {
            Gizmos.color = Color.cyan;
            if (h == null) return;
            Gizmos.DrawWireSphere(h.position, gizmoSize * 0.5f);

            Gizmos.DrawLine(transform.position, h.position);
            
        }

        
       
    }
}