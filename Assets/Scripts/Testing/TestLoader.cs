using UnityEngine;
using System.IO;
using System.Collections;
using UnityEngine.UI;
using System.Linq;

public class TestLoader: MonoBehaviour {

    public BMS.BMSManager bmsManager;
    public string BMSPath;

    void Start() {
        if(bmsManager == null) return;
        StartCoroutine(LoadBMSCoroutine());
    }

    IEnumerator LoadBMSCoroutine() {
#if UNITY_EDITOR
        var file = new FileInfo(BMSPath);
        if(file.Exists) {
#else
        var dirInfo = new DirectoryInfo(Path.Combine(Application.dataPath, "../BMS"));
        if(!dirInfo.Exists) yield break;
        var fileList = dirInfo.GetFiles("*.*", SearchOption.AllDirectories).Where(s => {
            switch(s.Extension.ToLower()) {
                case ".bms": case ".bme": case ".bml": case ".pms":
                    return true;
            }
            return false;
        }).ToArray();
        while(true) {
            var file = fileList[Random.Range(0, fileList.Length)];
            if(!file.Exists) continue;
#endif
            string bmsContent;
            using(var fsRead = file.OpenText())
                bmsContent = fsRead.ReadToEnd();
            bmsManager.LoadBMS(bmsContent, file.Directory.FullName, file.Extension);
            while(!bmsManager.BMSLoaded) yield return null;

            bmsManager.ReloadBMS(BMS.BMSReloadOperation.Body | BMS.BMSReloadOperation.ResourceHeader);
            while(!bmsManager.BMSLoaded) yield return null;
            bmsManager.ReloadBMS(BMS.BMSReloadOperation.Resources);
            while(bmsManager.IsLoadingResources) yield return null;
            bmsManager.InitializeNoteScore();
            bmsManager.IsStarted = true;
#if !UNITY_EDITOR
            while(bmsManager.IsStarted) yield return null;
#endif
        }
        yield break;
    }
}
