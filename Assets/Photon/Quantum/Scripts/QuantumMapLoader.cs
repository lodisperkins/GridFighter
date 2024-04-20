using Photon.Analyzer;
using Quantum;
using UnityEngine;

public class QuantumMapLoader : MonoBehaviour {
  [StaticField]
  private static QuantumMapLoader _instance;
  
  [StaticField]
  private static bool _isApplicationQuitting;

  public static QuantumMapLoader Instance {
    get {
      if (_isApplicationQuitting) {
        return null;
      }

      if (_instance == null) {
        _instance = GameObject.FindObjectOfType<QuantumMapLoader>();
      }

      if (_instance == null) {
        _instance = new GameObject("QuantumMapLoader").AddComponent<QuantumMapLoader>();
      }

      return _instance;
    }
  }

  public void Awake() {
    DontDestroyOnLoad(this);
  }

  public void OnApplicationQuit() {
    _isApplicationQuitting = true;
  }

  [StaticFieldResetMethod]
  public static void ResetStatics() {
    _instance = null;
    _isApplicationQuitting = false;
  }
}