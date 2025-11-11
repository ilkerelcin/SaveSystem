using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SaveLoadUI : MonoBehaviour
{
    public RawImage[] previewImages;
    public Text[] saveDateTexts;
   
    public void OnSaveButtonClicked(int slotNum)
    {
        StartCoroutine(SaveAndUpdateUI(slotNum));
        SaveSystem.Instance.Save(slotNum);

    }

    public void OnLoadButtonClicked(int slotNum)
    {
        SaveSystem.Instance.Load(slotNum);
    }

    public void OnDeleteButtonClicked(int slotNum)
    {
        SaveSystem.Instance.DeleteSave(slotNum);
        previewImages[slotNum].texture = null;
        saveDateTexts[slotNum].text = "";
    }
    private IEnumerator SaveAndUpdateUI(int slotNum)
    {
        yield return StartCoroutine(SaveSystem.Instance.Save(slotNum));

        SaveSystem.Instance.LoadScreenshotToButton(slotNum, previewImages[slotNum]);
        saveDateTexts[slotNum].text = SaveSystem.Instance.GetSaveDateTime(slotNum);
    }

}
