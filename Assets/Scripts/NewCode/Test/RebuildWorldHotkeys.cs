using UnityEngine;

namespace Game.Dev
{
    /// √ор€чие клавиши перегенерации мира:
    /// R Ч новый случайный seed, пересборка
    /// T Ч пересборка с тем же seed
    /// [+]/[-] на num/main Ч изменить половинный размер (±32) и пересобрать
    public class RebuildWorldHotkeys : MonoBehaviour
    {
        [SerializeField] private bool listenInEditorOnly = false;
        [SerializeField] private int sizeStep = 32;

        private void Update()
        {
#if UNITY_EDITOR
            if (listenInEditorOnly == false || Application.isEditor)
#else
            if (true)
#endif
            {
                var wm = GlobalCore.Instance?.WorldManager;
                if (wm == null) return;

                if (Input.GetKeyDown(KeyCode.R))
                {
                    int newSeed = Random.Range(int.MinValue, int.MaxValue);
                    wm.RebuildWorld(newSeed: newSeed);
                    Debug.Log($"[Hotkeys] Rebuild with NEW seed: {newSeed}");
                }
                else if (Input.GetKeyDown(KeyCode.T))
                {
                    wm.RebuildWorld();
                    Debug.Log("[Hotkeys] Rebuild with SAME seed");
                }
                else if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
                {
                    var half = wm.SizeMap;
                    wm.RebuildWorld(newHalfSize: new Vector2Int(half.x + sizeStep, half.y + sizeStep));
                    Debug.Log($"[Hotkeys] Size +{sizeStep}: {wm.SizeMap}");
                }
                else if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
                {
                    var half = wm.SizeMap;
                    wm.RebuildWorld(newHalfSize: new Vector2Int(Mathf.Max(1, half.x - sizeStep), Mathf.Max(1, half.y - sizeStep)));
                    Debug.Log($"[Hotkeys] Size -{sizeStep}: {wm.SizeMap}");
                }
            }
        }
    }
}