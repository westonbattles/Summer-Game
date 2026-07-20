CombatSystem script lives on each entity that uses it

Damaging targets now works like:  
_old_: `target.TakeDamage(damageInfo);`  
_new_: `brassKnuckles.Player.Combat.TryDamage(target, damageInfo);` or ``

***
Two main functions:

<br>

_TryDealDamage_  
\- main damaging function


_TryDealDamageWithoutEffects_  
\- doesn't use item notification system to tell all items that a damage as occured, so it won't apply effects (like fire or wtv)  
\- this is just so like effect damage can't/won't add resursively add more effects haha


***
_unused for now_  
_TryDealItemDamage_  
\- it's just commented out rn, but if ever need custom functionality for item damage (which we prob won't ever need, all of it can be handled in the item ticking stuff), we can bring this in
