using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class VoluLightRay : MonoBehaviour
{
    public enum Mode
    {
        Spot,
        Point,
        Parallel
    }
    public enum Shape
    {
        Circle,
        Rectangle
    }
    [SerializeField] public Mode CurrentMode;
    [SerializeField] public Shape CurrentEmitterShape;
    [Tooltip("Light source transform, only needed for Spot")][SerializeField] public GameObject Anchor;
    [SerializeField] public int RayCount;
    [SerializeField] public Vector2 Spread;
    /*[Range(0, float.PositiveInfinity)] */[SerializeField] public float MaxDistance = 1000;
    /*[Range(.01f, 10)] */[SerializeField] public float RayWidth = 1f;
    [SerializeField] public GameObject Ray;
    [SerializeField] private Material RayMaterial;
    [SerializeField] public List<string> CollisionLayersInclusive;
    private int _layermask = 0;
    [SerializeField] private bool Baked = false;
    [SerializeField] private bool BakeToNewGameObject = false;

    
    [HideInInspector] public bool _baked = false;
    //private Vector3[] RayStart;
    private Vector3[] RayHit;
    
    private int RayCountLastFrame;
    private Vector2 SpreadLastFrame;

    void Awake()
    {
        RayMaterial = Ray.GetComponent<MeshRenderer>().sharedMaterial;
        RayCountLastFrame = RayCount;
        SpreadLastFrame = Spread;
        RecreateHelpers();
        //RayStart = BatchRayPosAndDestroyHelpers(false);
    }

    void Start()
    {

    }
    private void SetRayPos(GameObject go, int i)
    {
        if(CurrentMode == Mode.Spot || CurrentMode == Mode.Parallel)
        {
            switch(CurrentEmitterShape)
            {
                case Shape.Circle:
                    var pos = Random.insideUnitCircle;
                    go.transform.localPosition = new Vector3(pos.x*Spread.x, 0, pos.y*Spread.y);
                    break;
                case Shape.Rectangle:
                    go.transform.localPosition = new Vector3(Random.Range(-Spread.x/2, Spread.x/2), 0, Random.Range(-Spread.y/2, Spread.y/2));
                    break;
            }
        }
        else if(CurrentMode == Mode.Point)
        {
            go.transform.localPosition = Random.insideUnitSphere*.1f;
        }
    }

    private void RecreateHelpers()
    {
        int n = RayCount;
        
        #if !UNITY_EDITOR
        foreach (Transform child in this.transform)
        {
            Destroy(child.gameObject);
        }
        #endif
        
        #if UNITY_EDITOR
        foreach (Transform child in this.transform)
        {
            DestroyImmediate(child.gameObject);
        }
        #endif

        for (int i = 0; i < n; i++)
        {
            GameObject go = Instantiate(Ray); 
            go.transform.SetParent(this.transform);
            SetRayPos(go,i);
            go.transform.Rotate(new Vector3(Random.Range(0,360),0,0), Space.Self);
        }
        
    }

    private Vector3[] SendRays()
    {
        Vector3[] op = new Vector3[transform.childCount];
        int i = 0;
        foreach (Transform child in this.transform)
        {
            Vector3 Source;
            Vector3 Direction;
            if(CurrentMode == Mode.Spot)
            {
                Source = Anchor.transform.position;
                Direction = (child.transform.position - Source).normalized;
            }
            else if(CurrentMode == Mode.Parallel)
            {
                Source = child.transform.position;
                Direction = transform.up;
            }
            else
            {
                Source = transform.position;
                Direction = (child.transform.position - Source).normalized;
            }
            
            RaycastHit _hit;
            if(Physics.Raycast(Source, Direction, out _hit , MaxDistance, _layermask)) op[i] = _hit.point;
            else op[i] = child.transform.position + Direction * MaxDistance;
            i++;
        }


        return op;
    }

    private void UpdateQuads()
    {
        int i = 0;
        foreach (Transform child in this.transform)
        {
            child.transform.LookAt(RayHit[i], child.transform.up);
            child.transform.localScale = new Vector3(RayWidth,1,(RayHit[i]-child.transform.position).magnitude);
            i++;
        }
    }
    public void UpdateRays()
    {
        if(RayCountLastFrame != RayCount) RecreateHelpers();
        if(SpreadLastFrame != Spread)
        {
            int i = 0;
            foreach(Transform child in this.transform)
            {
                SetRayPos(child.gameObject,i);
                i++;
            }
        
        }
        RayHit = SendRays();
        UpdateQuads();
        RayCountLastFrame = RayCount;
        SpreadLastFrame = Spread;
    }
    public GameObject BakeRays(bool NewObject)
    {
        GameObject op = gameObject;
        
        if(NewObject) op = new GameObject("BakedRays");
        else _baked = true;
        
        MeshFilter opMeshFilter = op.AddComponent<MeshFilter>();
        #if !UNITY_EDITOR
        op.AddComponent<MeshRenderer>().material = RayMaterial;
        #endif
        #if UNITY_EDITOR
        op.AddComponent<MeshRenderer>().sharedMaterial = RayMaterial;
        #endif

        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];

        Vector3 lastPos = transform.position;
        Quaternion lastRot = transform.rotation;
        transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        
        int i = 0;
        while (i < meshFilters.Length)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;

            
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;

            
            if(!NewObject) meshFilters[i].gameObject.SetActive(false);
            
            i++;
        }
        transform.SetPositionAndRotation(lastPos, lastRot);
        op.transform.SetPositionAndRotation(lastPos, lastRot);
        
        opMeshFilter.mesh = new Mesh();
        #if UNITY_EDITOR
        opMeshFilter.sharedMesh.CombineMeshes(combine);
        #endif
        #if !UNITY_EDITOR
        opMeshFilter.mesh.CombineMeshes(combine);
        #endif
        op.gameObject.SetActive(true);
        
        return op;
    }

    public void UnBakeRays()
    {
        #if !UNITY_EDITOR
        Destroy(GetComponent<MeshRenderer>());
        Destroy(GetComponent<MeshFilter>().mesh);
        Destroy(GetComponent<MeshFilter>());
        #endif
        
        #if UNITY_EDITOR
        DestroyImmediate(GetComponent<MeshRenderer>());
        DestroyImmediate(GetComponent<MeshFilter>().sharedMesh);
        DestroyImmediate(GetComponent<MeshFilter>());
        #endif
        
        foreach(Transform child in this.transform)
        {
            child.gameObject.SetActive(true);
        }
        _baked = false;
    }

    private int CalcLayermask()
    {
        int op = 0;
        foreach(string layer in CollisionLayersInclusive)
        {
            int temp = 1 << LayerMask.NameToLayer(layer);
            op = op | temp;
        }
        return op;
    }

    void Update()
    {
        _layermask = CalcLayermask();
        #if UNITY_EDITOR
        if(EditorApplication.isPlaying) return;
        if(BakeToNewGameObject)
        {
            BakeRays(true);
            BakeToNewGameObject = false;
        }
        
        if(!_baked && !Baked) {UpdateRays(); return;}
        else if(Baked && !_baked) BakeRays(false);
        else if(!Baked && _baked) UnBakeRays();
        #endif
    }
    void FixedUpdate()
    {
        if(BakeToNewGameObject)
        {
            BakeRays(true);
            BakeToNewGameObject = false;
        }
        
        if(!_baked && !Baked) {UpdateRays(); return;}
        else if(Baked && !_baked) BakeRays(false);
        else if(!Baked && _baked) UnBakeRays();
    }
}
