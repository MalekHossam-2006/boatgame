using UnityEngine;

public class ChangeColor : MonoBehaviour
{
    void OnCollisionEnter(Collision collision){
        GetComponent<MeshRenderer>().material.color= Color.red;
        Debug.Log("You Hit Me");
    }
}
