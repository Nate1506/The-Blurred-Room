using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class MultiplyRotationXWithMinusOne : MonoBehaviour
{
    [MenuItem("Edit/X-")]
  static void CreateWizard()
    {
        //ScriptableWizard.DisplayWizard("Minus X", typeof(MultiplyRotationXWithMinusOne), "MultiplyMinus");
        List<GameObject> mySelection = new List<GameObject>(Selection.gameObjects);
        foreach (GameObject prop in mySelection)
        {
            
            prop.transform.localScale = new Vector3(prop.transform.localScale.x * -1f, prop.transform.localScale.y, prop.transform.localScale.z);


        }
    }
    
     /* void OnWizardCreate()
    {
        List<GameObject> mySelection = new List<GameObject>(Selection.gameObjects);
        foreach (GameObject prop in mySelection)
        {
            
            prop.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            

        }

    } */
}
