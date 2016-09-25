using UnityEngine;
using BMS;

class ExtraCameraHandler: MonoBehaviour {
    [SerializeField]
    BMSManager bmsManager;

    void Awake() {
        bmsManager.OnGameStarted += GameStarted;    
    }

    void OnDestroy() {
        if(bmsManager)
            bmsManager.OnGameStarted -= GameStarted;
    }

    void GameStarted() {
        gameObject.SetActive(bmsManager.BGAEnabled);
    }
}
