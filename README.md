
# SaveService v1 (A1)

## Что это
Минимальная система сейвов для альфы: хранит **только делты по чанкам** (harvested/destroyed/placed) и базовые снапшоты.
Формат — JSON на диске. Словари сериализуются как списки.

## Как подключить
1. Положите `SaveService` на стартовую сцену (`DontDestroyOnLoad` включен).
2. На старте игры вызовите:
   ```csharp
   SaveService.Instance.Init(worldSeed, chunkWidth, chunkHeight);
   SaveService.Instance.LoadNow(); // если хотите автозагрузку
   ```
3. В спавнере объектов при создании урожая (BerryBush и т.п.) проверьте:
   ```csharp
   if (SaveService.Instance.ShouldHideHarvestable(data.type, cell))
       go.SetActive(false); // уже собран ранее
   ```
   или, если нужно детальнее — получите `ChunkKey` и `persistentId` и проверьте напрямую.
4. В месте **успешного сбора** (Interact) добавьте:
   ```csharp
   var key = SaveService.Instance.KeyFromCell(Cell);
   var pid = SaveService.Instance.ComputePersistentId(Data.type, Cell);
   SaveService.Instance.MarkHarvested(key, pid);
   SaveService.Instance.SaveNow(); // по желанию — автосейв
   ```

## Модель
- `ChunkKey {x,y}` — ключ чанка.
- `ChunkDelta` — списки: `harvestedIds`, `destroyedIds`, `placed`.
- `SaveData` — снапшоты + `worldDeltas` (список пар ключ/дельта).

## Ограничения альфы
- Нет шифрования/сжатия.
- Нет миграций формата (версионирование можно добавить полем `formatVersion`).
- Инвентарь/квесты/репутация — заглушки под вашу реализацию (сделаем в А2/В1).
