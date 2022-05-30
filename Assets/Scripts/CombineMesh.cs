using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class CombineMesh : MonoBehaviour
{
    [SerializeField] bool autoCombine;
    [SerializeField] bool createCollider;
    [SerializeField] Transform colliderParent;
    void Start() {
        if(!autoCombine) return;
        CombineMeshes();
    }
    public void CombineMeshes(){
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];

        int i = 0;
        while (i < meshFilters.Length)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            meshFilters[i].gameObject.SetActive(false);

            
            if(createCollider){
                BoxCollider otherCollider = meshFilters[i].gameObject.GetComponent<BoxCollider>();

                if(otherCollider != null){
                    GameObject newGameObj = new GameObject("Collider");
                    BoxCollider collider = newGameObj.AddComponent<BoxCollider>();
                    collider.transform.parent = colliderParent;
                    collider.transform.position = meshFilters[i].transform.position;
                    collider.transform.localScale = new Vector3(1f,1f,1f);
                    collider.transform.rotation = meshFilters[i].transform.rotation;
                    collider.size = otherCollider.size;
                    collider.center = otherCollider.center;
                }
            }


            i++;
        }

        transform.GetComponent<MeshFilter>().mesh = new Mesh();
        transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);
        transform.gameObject.SetActive(true);

    }
}
