<div style="display: flex; gap: 10px; flex-wrap: wrap; margin-bottom: 16px;">
  <img src="https://img.shields.io/codefactor/grade/github/se3ya/LC-ConfigurableQuota?style=flat&logo=codefactor&logoColor=white&color=83E6FB&cacheSeconds=1200" alt="CodeFactor Grade">
  <img src="https://img.shields.io/thunderstore/dt/seechela/ConfigurableQuota?style=flat&logo=thunderstore&logoColor=white&color=83E6FB&cacheSeconds=1200" alt="Thunderstore Downloads">
  <img src="https://img.shields.io/github/v/release/se3ya/LC-ConfigurableQuota?style=flat&logo=github&logoColor=white&color=83E6FB&cacheSeconds=1200" alt="GitHub Release Version">
  </div>
<br><br>

---
# ConfigurableQuota
### **Allows users to configure every aspect of the quota the way they want.**
---

## Features

- Configure starting credits, starting quota and deadline
- Curved quota growth with adjustable sharpness and randomizer
- Growth dampening - slows down quota increases the longer you play
- Player-count scaling with threshold, cap, and per-player multiplier
- Rollover - carry excess fulfillment into the next cycle
- Randomized deadline length each cycle
- Disable the quota system entirely
- Credit penalties when crew members die
- Quota penalties when crew members die
- Randomly lose collected scrap items on full crew wipe
- Permanently reduce scrap value on full crew wipe
- Randomly lose purchased equipment on full crew wipe
- Adjustable new quota animation speed

---

## Configuration Options

### **0. Basic**

- **StartingCredits** - Credits you start with on a new game
- **StartingQuota** - First quota value on a new game
- **DaysToDeadline** - Days per quota cycle. Ignored when `RandomizeDeadline` is on
- **RandomizeDeadline** - Pick a random deadline length each cycle using the min/max below
- **DeadlineMin** - Minimum days when random deadline is enabled
- **DeadlineMax** - Maximum days when random deadline is enabled

### **1. Leveling**

- **BaseIncrease** - Base amount the quota goes up each cycle
- **CurveSharpness** - Controls how fast the quota scales. Higher = slower growth
- **RandomizerMultiplier** - Adds variation to quota increases. 0 = no randomness, 1 = vanilla variance
- **FinalLevel** - Quota value where curved growth switches to flat. Set -1 to disable
- **FinalIncrease** - Flat increase used after hitting `FinalLevel`
- **QuotaCap** - Maximum quota value. Set -1 for no limit
- **EnableGrowthDampening** - Gradually reduces quota growth the longer you play
- **DampeningStartAt** - How many fulfilled quotas before dampening kicks in
- **DampeningSharpness** - How aggressively growth is reduced. Lower = stronger dampening

### **2. PlayerScaling**

- **EnablePlayerMultiplier** - Scale quota increases based on how many players are connected
- **PlayerThreshold** - Player count where scaling starts
- **PlayerCap** - Maximum players counted for scaling
- **MultPerPlayer** - Extra multiplier per player above the threshold. Example: 0.25 = +25% per player

### **3. Optional**

- **DisableQuota** - Disables the quota
- **RolloverAmount** - Percentage of excess fulfillment that carries over to the next cycle. 0 = none

### **4. Penalties.Credits**

- **Enabled** - Reduce credits when crew members die
- **OnGordion** - Apply credit penalties even when visiting Gordion
- **PercentPerPlayer** - Credits lost per dead player. Ignored if `Dynamic` is on
- **Dynamic** - Use team death ratio instead of per-player count
- **PercentCap** - Maximum percentage of credits that can be lost
- **PercentThreshold** - Ignore penalties below this percentage
- **RecoveryBonus** - Reduce penalty if you recover bodies

### **5. Penalties.Quota**

- **Enabled** - Increase the current quota when crew members die
- **OnGordion** - Apply quota penalties even when visiting Gordion
- **PercentPerPlayer** - Quota increase per dead player. Ignored if `Dynamic` is on
- **Dynamic** - Use team death ratio instead of per-player count
- **PercentCap** - Maximum percentage the quota can increase
- **PercentThreshold** - Ignore penalties below this percentage
- **RecoveryBonus** - Reduce penalty if you recover bodies

### **6. Loss.Scrap**

- **Enabled** - Randomly lose collected scrap items when the entire crew is wiped
- **ItemsSafeChance** - Chance for each item to be protected from loss
- **LoseEachScrapChance** - Chance to lose an unprotected item
- **MaxLostScrapItems** - Maximum scrap items that can be lost per round

### **7. Loss.Value**

- **Enabled** - Permanently reduce scrap value of all ship items on full crew wipe. Persists to the next day
- **Percent** - How much value to remove. Example: 0.25 = items lose 25% of their value, stacks on repeated wipes

### **8. Loss.Equipment**

- **Enabled** - Randomly lose purchased equipment when the entire crew is wiped
- **LoseEachEquipmentChance** - Chance for each equipment item to be lost
- **MaxLostEquipmentItems** - Maximum equipment items that can be lost per round

### **9. UI**

- **QuotaAnimationSpeed** - Speed of the new quota pop-up animation. Higher = faster

---

## FAQ

### **Q: Can I use this with other quota mods?**

Not recommended.

---

## Credits

- Developed by **[seeya](https://thunderstore.io/c/lethal-company/p/seechela/)**

---

## License

Distributed under the GPL v3 License.

---

### 💖 Support

If you enjoy my work, consider [supporting](https://www.buymeacoffee.com/see_ya) me. Donations are optional but greatly appreciated.

---
