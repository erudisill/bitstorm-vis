using UnityEngine;
using System.Collections;

public class UnisonAPI : MonoBehaviour {

    public int BinId;

    public void MoveInventory() {
        StartCoroutine(DoMoveInventory());
    }

    private IEnumerator DoMoveInventory() {
        // Create a Web Form
        WWWForm form = new WWWForm();
        form.AddField("VIN", "SCBFH7ZA1GC054247");
        form.AddField("BinId", BinId);

        // Upload to a cgi script
        WWW w = new WWW("http://unison-dev.cphandheld.com/api/Inventory/NadaMoveInventory", form);
        yield return w;
        if (!string.IsNullOrEmpty(w.error)) {
            print(w.error);
        }
        else {
            print("Moved to bin " + BinId);
        }        
    }
}
