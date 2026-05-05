<div style="display: flex; gap: 10px; flex-wrap: wrap; margin-bottom: 16px;">
  <img src="https://img.shields.io/codefactor/grade/github/se3ya/LC-ConfigurableQuota?style=flat&logo=codefactor&logoColor=white&color=83E6FB&cacheSeconds=1200" alt="CodeFactor Grade">
  <img src="https://img.shields.io/thunderstore/dt/seechela/ConfigurableQuota?style=flat&logo=thunderstore&logoColor=white&color=83E6FB&cacheSeconds=1200" alt="Thunderstore Downloads">
  <img src="https://img.shields.io/github/v/release/se3ya/LC-ConfigurableQuota?style=flat&logo=github&logoColor=white&color=83E6FB&cacheSeconds=1200" alt="GitHub Release Version">
  </div>
<br><br>

---
# Configurable Quota
### **Allows users to configure every aspect of the quota the way they want.**
---

## Features

- Configure starting credits, starting quota and deadline
- Configure curved quota growth with sharpness and randomizer
- Configure growth dampening which slows down quota increases the longer you play
- Configure player count scaling with threshold, cap and per player multiplier
- Configure rollover which transfers any extra fulfilment to the next quota
- Configure randomized deadline
- Configure credit penalties when crew members die
- Configure quota penalties when crew members die
- Configure randomly lost scrap items on a full crew wipe
- Configure reduced scrap value on full crew wipe
- Configure randomly lost purchased equipment on a full crew wipe
- Disable quota entirely
- Configure new quota animation speed
- Show actual penalty values on fine UI

---

## Configuration Options

### **0. Basic**

- **Starting Credits** - Credits you start with on a new game
- **Starting Quota** - First quota value on a new game
- **Days To Deadline** - Days per quota cycle. Ignored when `RandomizeDeadline` is on
- **Randomize Deadline** - Pick a random deadline length each cycle using the min/max below
- **Deadline Min** - Minimum days when random deadline is enabled
- **Deadline Max** - Maximum days when random deadline is enabled

### **1. Leveling**

- **Base Increase** - Base amount the quota goes up each cycle
- **Curve Sharpness** - Controls how fast the quota scales. Higher = slower growth
- **Randomizer Multiplier** - Adds variation to quota increases. 0 = no randomness, 1 = vanilla variance
- **Final Level** - Quota value where curved growth switches to flat. Set -1 to disable
- **Final Increase** - Flat increase used after hitting `FinalLevel`
- **Quota Cap** - Maximum quota value. Set -1 for no limit
- **Enable Growth Dampening** - Gradually reduces quota growth the longer you play
- **Dampening Start At** - How many fulfilled quotas before dampening kicks in
- **Dampening Sharpness** - How aggressively growth is reduced. Lower = stronger dampening

### **2. Player Scaling**

- **Enable Player Multiplier** - Scale quota increases based on how many players are connected
- **Player Threshold** - Player count where scaling starts
- **Player Cap** - Maximum players counted for scaling
- **Multiplier Per Player** - Extra multiplier per player above the threshold. Example: 0.25 = +25% per player

### **3. Optional**

- **Disable Quota** - Disables the quota
- **Rollover Amount** - Percentage of excess fulfillment that carries over to the next cycle. 0 = none

### **4. Penalties Credits**

- **Enabled** - Reduce credits when crew members die
- **On Gordion** - Apply credit penalties even when visiting Gordion
- **Percent Per Player** - Credits lost per dead player. Ignored if `Dynamic` is on
- **Dynamic** - Use team death ratio instead of per-player count
- **Percent Cap** - Maximum percentage of credits that can be lost
- **Percent Threshold** - Ignore penalties below this percentage
- **Recovery Bonus** - Reduce penalty if you recover bodies

### **5. Penalties Quota**

- **Enabled** - Increase the current quota when crew members die
- **On Gordion** - Apply quota penalties even when visiting Gordion
- **Percent Per Player** - Quota increase per dead player. Ignored if `Dynamic` is on
- **Dynamic** - Use team death ratio instead of per-player count
- **Percent Cap** - Maximum percentage the quota can increase
- **Percent Threshold** - Ignore penalties below this percentage
- **Recovery Bonus** - Reduce penalty if you recover bodies

### **6. Loss Scrap**

- **Enabled** - Randomly lose collected scrap items when the entire crew is wiped
- **Items Safe Chance** - Chance for each item to be protected from loss
- **Lose Each Scrap Chance** - Chance to lose an unprotected item
- **Max Lost Scrap Items** - Maximum scrap items that can be lost per round

### **7. Loss Value**

- **Enabled** - Reduce scrap value of all ship items on full crew wipe
- **Percent** - How much of scrap value to remove. Example: 0.25 = items lose 25% of their value, stacks on repeated wipes

### **8. Loss Equipment**

- **Enabled** - Randomly lose purchased equipment when the entire crew is wiped
- **Lose Each Equipment Chance** - Chance for each equipment item to be lost
- **Max Lost Equipment Items** - Maximum equipment items that can be lost per round

### **9. UI**

- **Quota Animation Speed** - Speed of the new quota pop-up animation. Higher = faster

---

## FAQ

### **Q: Can I use this with other quota mods?**

Not recommended.

---

## Credits

- Developed by **[seeya](https://thunderstore.io/c/lethal-company/p/seechela/)**
- Inspired mostly from QuotaOverhaul, AfineQuota and ChocoQuota

---

## License

Distributed under the GPL v3 License.

---

### 💖 Support

If you enjoy my work, consider [supporting](https://www.buymeacoffee.com/see_ya) me. Donations are optional but greatly appreciated.

---
