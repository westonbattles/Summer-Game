***
_ADDING ITEMS:_  

\
**Creating the Asset**

Create a folder in `Assets/#Game/Items` using the item name (`#Game/Items/LuckyBullet/` for example).  

In this folder, create an ItemDefinition using `Create->Items->Item`

_World Model_
- When we eventually make 3d models for these items (when they are in world or being displayed in logbook), the model prefab will also live in this folder. And it will be assigned to our item in the inpsector `World Model Prefab` property (the null item model gets used when model prefab property is empty)

_Id_
- Each item definition we make should have an id value we set up with it, which is defined as one of the properties
- Naming convention imo should just be `snake_case` of whatever the asset name is (LuckyBullet -> lucky_bullet)
- Important so if we need an itemdefition quickly for a specific item, we can just run ItemCatalog.GetItemFromId(string id)

\
**Creating the Behaviour**  
_PLEASE READ THRU SCRIPT IN `Assets/#Game/Items/Example`_

basically, behaviours have two overridable functions, one to apply modifiers (gets runs once on inventory recalculation), and one to create a ItemRuntim instance (which you need to define yourself if the item should have custom behaviour at runtime that isn't just applying a modifier or changing a value once. for example, items that heal you over time. or do other things over time)

the example script shows how to create the itemruntime class and use it properly. please lmk if you have any questions about it. the base class gives us a variable `Count` we can use to get the amount of the item we have in our inventory
***
_Spawning Items_  
  
`ItemSpawner.Instance.Spawn(ItemDefinition definition, Vector3 position, Vector3 initialVelocity)`

items shoot out like in ror2 as a lil ball and turn into items on collision with floor, (like a pokeball lowk)
- parameters are obvious i hope
***
_Item Catalog_
- The Item Catalog is an Asset that lives in the root of `Asset/#Game/Items`. I coded a tiny editor script for it that essentially uses the big refresh button to automatically update the catalog's `definitions` array to include every `ItemDefinition` asset in the /Items folder. You do not need and can not modify this list in the inspector. The point of this asset is to have a truth table list for every item asset we have, if we want to prevent a player from getting one that should be handled programitaccly elswhere
- ### **_IMPORTANT!!!!!_**
  - Connected via Serialization to ItemSpawner instance. to access use `ItemSpawner.Instance.ItemCatalog`
  - `ItemDefinition GetItemFromId(string id)` returns the itemdefinition for the given id value, null if id isn't valid
  - `ItemDefinition GetRandomItem()` returns a random item
    - My next task will be to hook up blancing this with rarities, and then have a GetRandomDuoItem() function as well
  - THIS WILL BE THE PRIMARY WAY TO SPAWN ITEMS (using them to get the defintion to pass into Itemspawner spawn function)
***

_Item Inventory_

- im lowkey too lazy to write paragraphs anymore so im doing bullet points
- has a list of every item in our players current inventory  
- ticks every one of those items if they use itemruntime 
- `HasItem() function and others to help with stuff`
- `AddItem() if we need, but using ItemSpawner should be preferred`








a
