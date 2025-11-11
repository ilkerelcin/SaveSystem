// To add Newtonsoft JSON support:
// Add the following line to the "dependencies" section of your Packages/manifest.json file:
// "com.unity.nuget.newtonsoft-json": "3.2.1",  

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SaveEntry
{
    public string key;
    public object data;
}

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance;
    public Canvas pauseCanvas;


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public IEnumerator Save(int slotNum)
    {

        string saveDateTime = System.DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        var saveables = FindObjectsOfType<MonoBehaviour>(true); 
        Dictionary<string, object> state = new Dictionary<string, object>();
        foreach (var saveable in saveables)
        {
            if (saveable is ISaveable saveableObj)
            {
                string id = saveableObj.GetUniqueIdentifier();
                state[id] = saveableObj.CaptureState();
            }
        }

        string json = JsonConvert.SerializeObject(state);
        StartCoroutine(CaptureScreenshot(slotNum));

        PlayerPrefs.SetString($"SaveSlot{slotNum}", json);
        PlayerPrefs.SetString($"SaveSlot{slotNum}_DateTime", saveDateTime); 
        PlayerPrefs.Save();

        yield return StartCoroutine(CaptureScreenshot(slotNum));
    }

    public void Load(int slotNum)
    {
        if (!PlayerPrefs.HasKey($"SaveSlot{slotNum}"))
        {
            return;
        }

        string json = PlayerPrefs.GetString($"SaveSlot{slotNum}");

        var state = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

        var saveables = FindObjectsOfType<MonoBehaviour>(true);

        foreach (var saveable in saveables)
        {
            if (saveable is ISaveable saveableObj)
            {
                string id = saveableObj.GetUniqueIdentifier();

                if (state.ContainsKey(id))
                {
                    saveableObj.RestoreState(state[id]);
                }
            }
        }

    }

    public void DeleteSave(int slotNum)
    {
        if (PlayerPrefs.HasKey($"SaveSlot{slotNum}"))
        {
            PlayerPrefs.DeleteKey($"SaveSlot{slotNum}");
        }
        else
        {
        }
    }

    private static string GetFilePath(int? slot = null, string saveName = null)
    {
        if (!string.IsNullOrEmpty(saveName))
        {
            return Application.persistentDataPath + "/" + saveName + ".dat";
        }else if (slot.HasValue)
        {
            return Application.persistentDataPath + "/save_slot" + slot.Value + ".dat";
        }
        else
        {
            return Application.persistentDataPath + "/checkpoint.dat";
        }
    }
    
    public static void SaveGame(int? slot = null, string saveName = null)
    {
        var saveables = GameObject.FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>();
        Dictionary<string, object> stateDict = new Dictionary<string, object>();
        foreach (var saveable in saveables)
        {
            stateDict[saveable.GetUniqueIdentifier()] = saveable.CaptureState();
        }
        
        SerializationWrapper wrapper = new SerializationWrapper(stateDict);
        
        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        string json = JsonConvert.SerializeObject(wrapper, Formatting.Indented, settings);
        byte[] binaryData = System.Text.Encoding.UTF8.GetBytes(json);
        string filePath = GetFilePath(slot, saveName);
        System.IO.File.WriteAllBytes(filePath, binaryData);
    }    
    
    public static void LoadGame(int? slot = null, string saveName = null)
    {
        string filePath = GetFilePath(slot, saveName);

        if (!System.IO.File.Exists(filePath)) return;

        byte[] binData = System.IO.File.ReadAllBytes(filePath);
        string json = Encoding.UTF8.GetString(binData);

        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
        
        SerializationWrapper wrapper = JsonConvert.DeserializeObject<SerializationWrapper>(json, settings);
        
        if (wrapper == null || wrapper.jsonData == null)
        {
            return;
        }
        var saveables = GameObject.FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>();

        foreach (var saveable in saveables)
        {
            string id = saveable.GetUniqueIdentifier();
            var matchedEntry = wrapper.jsonData.FirstOrDefault(entry => entry.key == id);

            if (matchedEntry != null && matchedEntry.data != null)
            {
                saveable.RestoreState(matchedEntry.data);
            }
        }
    }

    public static List<string> ListAllSaveFiles(bool fullPath = false)
    {
        string directoryPath = Application.persistentDataPath;
        if (!System.IO.Directory.Exists(directoryPath))
        {
            return new List<string>();
        }

        var files = System.IO.Directory.GetFiles(directoryPath, "*.dat");

        if (!files.Any())
        {
            return new List<string>();
        }
        
        var sortedFiles = files.OrderByDescending(f => System.IO.File.GetLastWriteTime(f));
        
        List<string> saveFileNames = new List<string>();
        
        foreach (var file in sortedFiles)
        {
            if (fullPath)
            {
                saveFileNames.Add(file);
            }
            else
            {
                saveFileNames.Add(Path.GetFileNameWithoutExtension(file)); 
            }
        }
        
        return saveFileNames;
    }
    
    [System.Serializable]
    private class SerializationWrapper
    {
        
        public List<SaveEntry> jsonData = new List<SaveEntry>();

        public SerializationWrapper()
        {
        }
        
        public SerializationWrapper(Dictionary<string, object> dictionary)
        {
            foreach (var pair in dictionary)
            {
                SaveEntry entry = new SaveEntry
                {
                    key = pair.Key,
                    data = pair.Value
                };
                jsonData.Add(entry);
            }
        }
    }
    public IEnumerator CaptureScreenshot(int slot)
    {
        if (pauseCanvas != null)
            pauseCanvas.enabled = false;  

        yield return new WaitForEndOfFrame();

        Texture2D screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenshot.Apply();

        byte[] bytes = screenshot.EncodeToPNG();
        File.WriteAllBytes(GetScreenshotPath(slot), bytes);
        
        Destroy(screenshot);

        yield return null;

        if (pauseCanvas != null)
            pauseCanvas.enabled = true;   
    }
    private string GetScreenshotPath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, $"save_slot_{slot}_screenshot.png");
    }
    public void LoadScreenshotToButton(int slot, RawImage image)
    {
        string path = GetScreenshotPath(slot);
        if (File.Exists(path))
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(bytes);
            image.texture = texture;
        }
        else
        {
            image.texture = null;
        }
    }
    public string GetSaveDateTime(int slotNum)
    {
        if (PlayerPrefs.HasKey($"SaveSlot{slotNum}_DateTime"))
        {
            return PlayerPrefs.GetString($"SaveSlot{slotNum}_DateTime");
        }
        else
        {
            return "!";
        }
    }


}
