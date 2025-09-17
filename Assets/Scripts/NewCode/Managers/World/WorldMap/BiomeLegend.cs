using UnityEngine;
using UnityEngine.UI;
using Game.World.Map.Biome;

namespace Game.UI
{
    public class BiomeLegend : MonoBehaviour
    {
        [Header("Resources path")]
        [SerializeField] private string biomesPath = "ScriptAssets/Biomes";

        [Header("UI")]
        [SerializeField] private RectTransform container; // объект с VerticalLayoutGroup
        [SerializeField] private Font font;
        [SerializeField] private int fontSize = 14;
        [SerializeField] private Vector2 itemSize = new Vector2(220, 22);

        private void Start()
        {
            if (container == null) container = GetComponent<RectTransform>();
            var items = Resources.LoadAll<BiomeData>(biomesPath);
            System.Array.Sort(items, (a, b) => a.Type.CompareTo(b.Type));

            foreach (var bd in items)
            {
                var row = new GameObject($"Legend_{bd.Type}", typeof(RectTransform));
                row.transform.SetParent(container, false);
                var rtRow = (RectTransform)row.transform;
                rtRow.sizeDelta = itemSize;

                // цвет
                var cGO = new GameObject("Color", typeof(RectTransform), typeof(Image));
                cGO.transform.SetParent(row.transform, false);
                var rtC = (RectTransform)cGO.transform;
                rtC.anchorMin = new Vector2(0, 0); rtC.anchorMax = new Vector2(0, 1);
                rtC.pivot = new Vector2(0, 0.5f);
                rtC.sizeDelta = new Vector2(itemSize.y, 0);
                cGO.GetComponent<Image>().color = bd.ColorMap.a > 0 ? bd.ColorMap : Color.magenta;

                // текст
                var tGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
                tGO.transform.SetParent(row.transform, false);
                var rtT = (RectTransform)tGO.transform;
                rtT.anchorMin = new Vector2(0, 0); rtT.anchorMax = new Vector2(1, 1);
                rtT.offsetMin = new Vector2(itemSize.y + 6, 0);
                rtT.offsetMax = Vector2.zero;
                var txt = tGO.GetComponent<Text>();
                txt.text = bd.Type.ToString();
                txt.alignment = TextAnchor.MiddleLeft;
                txt.fontSize = fontSize;
                txt.color = Color.white;
                if (font) txt.font = font;
            }
        }
    }
}