using System.Collections.Generic;
using UnityEngine;

public class PoolManagerSpecial : MonoBehaviour
{
    //public static PoolManagerSpecial Instance { get; private set; }
    //private void Awake()
    //{
    //    if (Instance == null)
    //    {
    //        Instance = this;
    //    }
    //    else
    //    {
    //        Destroy(gameObject);
    //    }
    //}
    //public static int PoolcountSpecial = 4000;
    //public static Queue<GameObject> Specialpool = new Queue<GameObject>();
    //public Dictionary<Vector2Int, GameObject> activeSpecial = new Dictionary<Vector2Int, GameObject>();
    //public GameObject GetSpecial(Vector3 position, Vector2Int key)
    //{
    //    if (GlobalCore.Instance.MainGameObjects.MainSpecial == null)
    //    {
    //        Debug.LogError(" specialPrefab не установлен в SquarePool!");
    //        return null;
    //    }
    //    if (GlobalCore.Instance.WorldManager.GlobalMap.ContainsKey(key))
    //        for (int i = 0; i < GlobalCore.Instance.WorldManager.Bioms.Count; i++)
    //            for (int j = 1; j <= GlobalCore.Instance.WorldManager.Bioms[(BiomsType)i].Count; j++)
    //                if (GlobalCore.Instance.WorldManager.Bioms[(BiomsType)i][j].Objects.TryGetValue(key, out ObjectManager.ObjectOnMap ValueL))
    //                {
    //                    GameObject special;
    //                    if (Specialpool.Count > 0)
    //                    {
    //                        special = Specialpool.Dequeue(); // Берём объект из пула
    //                    }
    //                    else
    //                    {
    //                        special = Instantiate(GlobalCore.Instance.MainGameObjects.MainSpecial); // Если пул пуст, создаём новый объект
    //                    }

    //                    if (special == null)
    //                    {
    //                        Debug.LogError(" Ошибка: special оказался null после создания!");
    //                        return null;
    //                    }
    //                    ObjectsInfoData InfoValue = GlobalCore.Instance.ObjectManager.baseObjects[(Objects)ValueL.type];
    //                    //Global.MainGameObject.Controller.GetComponent<ObjectBase>().CreateObjInfo(ValueL.type);
    //                    special.GetComponent<SpriteRenderer>().sprite = InfoValue.sprite;
    //                    special.transform.name = InfoValue.NameObject;
    //                    Vector2Int size = ValueL.size;
    //                    special.transform.localScale = new Vector3(size.x * 0.7f, size.y * 0.7f, special.transform.localScale.z);

    //                    // Центрирование относительно занимаемых клеток
    //                    Vector3 offset = new Vector3(
    //                        (size.x - 1) * 0.75f * 0.7f,
    //                        (size.y - 1) * 0.75f * 0.7f,
    //                        0f
    //                    );
    //                    if (InfoValue.HighObject)
    //                    {
    //                        float visualOffset = (ValueL.size.y) * 0.5f;
    //                        special.transform.localPosition = position + offset - new Vector3(0, visualOffset, 0f);
    //                    }
    //                    else
    //                        special.transform.localPosition = position + offset;
    //                    //Vector3 oldScale = special.transform.localScale;
    //                    //special.transform.localScale = new Vector3(size.x * 0.7f, size.y * 0.7f, special.transform.localScale.z);
    //                    //special.transform.localPosition = position + new Vector3(0, 0, -1);
    //                    //float scale = size.x * size.y;
    //                    //Vector3 positionOffset = new Vector3(size.x / scale, size.y / scale, -1f);
    //                    //special.transform.localPosition = position +positionOffset;
    //                    //Vector3 offset = (special.transform.localScale - oldScale) / 2;
    //                    //special.transform.localPosition = special.transform.localPosition - offset;
    //                    special.transform.SetParent(GlobalCore.Instance.MainGameObjects.WorldRoot.transform);
    //                    special.SetActive(true);
    //                    // Назначаем Sorting Layer и порядок отрисовки по Y
    //                    var renderer = special.GetComponent<SpriteRenderer>();
    //                    if (renderer != null)
    //                    {
    //                        renderer.sortingLayerName = "Objects";
    //                        renderer.sortingOrder = -(int)special.transform.localPosition.y; // глубина
    //                    }
    //                    ValueL = new(ValueL.id, ValueL.type, ValueL.position, ValueL.size, special, ValueL.occupiedCells, ValueL.CurrectHitBox);
    //                    GlobalCore.Instance.WorldManager.Bioms[(BiomsType)i][j].Objects[key] = ValueL;

    //                    return special;

    //                }
    //    if (GlobalCore.Instance.WorldManager.GlobalMap.ContainsKey(key))
    //        for (int i = 0; i < GlobalCore.Instance.WorldManager.Bioms.Count; i++)
    //            if (GlobalCore.Instance.WorldManager.Bioms[(BiomsType)i].TryGetValue(GlobalCore.Instance.WorldManager.GlobalMap[key].id, out WorldManager.BiomObjonMap ValueF))
    //                for (int n = 1; n <= ValueF.AllyNPC.Count; n++)
    //                    if (ValueF.AllyNPC[n].position == key)
    //                    {
    //                        GameObject special;
    //                        if (Specialpool.Count > 0)
    //                        {
    //                            special = Specialpool.Dequeue(); // Берём объект из пула
    //                        }
    //                        else
    //                        {
    //                            special = Instantiate(GlobalCore.Instance.MainGameObjects.MainSpecial); // Если пул пуст, создаём новый объект
    //                        }

    //                        if (special == null)
    //                        {
    //                            Debug.LogError(" Ошибка: special оказался null после создания!");
    //                            return null;
    //                        }
    //                        //if (ValueF.AllyNPC[n].Quests.Count > 0)
    //                        //    special.GetComponent<SpriteRenderer>().color = new(0.1f, 0f, 0.4f);
    //                        // else
    //                        var InfoNPC = GlobalCore.Instance.NPCManager.NPCDatabase[ValueF.AllyNPC[n].type];
    //                        special.GetComponent<SpriteRenderer>().color = InfoNPC.outfitColor;
    //                        special.transform.name = GlobalCore.Instance.NPCManager.NamesNPC[(int)ValueF.AllyNPC[n].type];
    //                        special.transform.localPosition = position + new Vector3(0, 0, -1);
    //                        special.transform.SetParent(GlobalCore.Instance.MainGameObjects.WorldRoot.transform);
    //                        special.SetActive(true);
    //                        // Назначаем Sorting Layer и порядок отрисовки по Y
    //                        var renderer = special.GetComponent<SpriteRenderer>();
    //                        if (renderer != null)
    //                        {
    //                            renderer.sortingLayerName = "Objects";
    //                            renderer.sortingOrder = -(int)special.transform.localPosition.y; // глубина
    //                        }
    //                        NPCManager.AllyNPC x = ValueF.AllyNPC[n];
    //                        x.CurrObj = special;
    //                        GlobalCore.Instance.WorldManager.Bioms[(BiomsType)i][GlobalCore.Instance.WorldManager.GlobalMap[key].id].AllyNPC[n] = x;
    //                        return special;

    //                    }
    //    //if (Global.ObjectsonMap.TryGetValue(key, out Global.MapSquare BaseObj))

    //    //{
    //    //    GameObject special;
    //    //    if (Global.Specialpool.Count > 0)
    //    //    {
    //    //        special = Global.Specialpool.Dequeue(); // Берём объект из пула
    //    //    }
    //    //    else
    //    //    {
    //    //        special = Instantiate(Global.MainGameObject.SpecialObj); // Если пул пуст, создаём новый объект
    //    //    }

    //    //    if (special == null)
    //    //    {
    //    //        Debug.LogError(" Ошибка: special оказался null после создания!");
    //    //        return null;
    //    //    }
    //    //    special.transform.localPosition = position + new Vector3(0, 0, -1);
    //    //    special.GetComponent<SpriteRenderer>().color = Color.white;
    //    //    special.transform.SetParent(Global.MainGameObject.Canvas.transform);
    //    //    special.SetActive(true);
    //    //    return special;
    //    //}
    //    return null;
    //}
    //public void GenerateSpecialPool()
    //{
    //    //Global.MainGameObject.EyePlayer = Instantiate(Global.MainGameObject.Player);
    //    //Global.MainGameObject.EyePlayer.transform.localScale = new Vector3(0.5f, 0.5f);

    //    if (GlobalCore.Instance.MainGameObjects.MainSpecial == null)
    //    {
    //        Debug.LogError(" squarePrefab не установлен в SquarePool! Добавь его в инспекторе.");
    //        return;
    //    }
    //    // Заполняем пул квадратами
    //    for (int i = 0; i < PoolcountSpecial; i++)
    //    {
    //        GameObject special = Instantiate(GlobalCore.Instance.MainGameObjects.MainSpecial);
    //        special.SetActive(false);
    //        special.transform.name = "sleepSpecial";
    //        special.transform.SetParent(GlobalCore.Instance.MainGameObjects.WorldRoot.transform);
    //        Specialpool.Enqueue(special);
    //    }
    //}
    //public void ReturnSpecial(GameObject special)
    //{
    //    if (special == null)
    //    {
    //        Debug.LogError(" Попытка вернуть null-объект в пул!");
    //        return;
    //    }
    //    for (int i = 0; i < GlobalCore.Instance.WorldManager.Bioms.Count; i++)
    //        for (int j = 1; j <= GlobalCore.Instance.WorldManager.Bioms[(BiomsType)i].Count; j++)
    //        {
    //            bool check = false;
    //            for (int k = 1; k <= GlobalCore.Instance.WorldManager.Bioms[(BiomsType)i][j].AllyNPC.Count; k++)
    //                if (GlobalCore.Instance.WorldManager.Bioms[(BiomsType)i][j].AllyNPC[k].CurrObj == special)
    //                {
    //                    NPCManager.AllyNPC x = GlobalCore.Instance.WorldManager.Bioms[(BiomsType)i][j].AllyNPC[k];
    //                    x.CurrObj = null;
    //                    check = true;
    //                    GlobalCore.Instance.WorldManager.Bioms[(BiomsType)i][j].AllyNPC[k] = x;
    //                }
    //            if (check == false)
    //                for (int k = 1; k <= GlobalCore.Instance.WorldManager.Bioms[(BiomsType)i][j].Objects.Count; k++)
    //                    if (GlobalCore.Instance.WorldManager.Bioms[(BiomsType)i][j].Objects.ContainsKey(new(Mathf.RoundToInt(special.transform.localPosition.x), Mathf.RoundToInt(special.transform.localPosition.y))))
    //                        if (GlobalCore.Instance.WorldManager.Bioms[(BiomsType)i][j].Objects[new(Mathf.RoundToInt(special.transform.localPosition.x), Mathf.RoundToInt(special.transform.localPosition.y))].currObj == special)
    //                        {
    //                            ObjectManager.ObjectOnMap x = GlobalCore.Instance.WorldManager.Bioms[(BiomsType)i][j].Objects[new(Mathf.RoundToInt(special.transform.localPosition.x), Mathf.RoundToInt(special.transform.localPosition.y))];
    //                            x.currObj = null;
    //                            GlobalCore.Instance.WorldManager.Bioms[(BiomsType)i][j].Objects[new(Mathf.RoundToInt(special.transform.localPosition.x), Mathf.RoundToInt(special.transform.localPosition.y))] = x;
    //                        }
    //        }

    //    special.transform.name = "sleepSpecial";
    //    special.SetActive(false);
    //    Specialpool.Enqueue(special);
    //}
}
