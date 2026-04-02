# SPEC-B-005: Skill Tree / Skill System Audit

> **Priority:** P0 (user bug report)
> **Symptom:** Skill tree icons not displaying. General skill system check needed.

---

## Diagnostic Points

### 1. Missing Icon Image in SkillRowUI
- **File:** `Assets/Scripts/UI/SkillTreeUI.cs:171-230`
- `SkillRowUI` has no `Image` field for skill icons — only text (name, level, cost, desc) and backgroundImage for state coloring.
- **A-006 assets exist:** `skilltree_bg_melee/ranged/magic`, `skilltree_node_locked/available/learned`, `skilltree_connector_*` (9 sprites total).
- **Fix:** Add `[SerializeField] Image iconImage` to `SkillRowUI`, load skill icon from `SkillDef`, assign in `Setup()`.

### 2. SkillDef Data Check
- Verify `SkillDef` has an `icon` or `sprite` field referencing the icon sprite name.
- If not, add field and populate in `skills.json`.

### 3. Skill Tree Background / Node Sprites
- Tree column backgrounds (`meleeColumn`, `rangedColumn`, `magicColumn`) — check if they use `skilltree_bg_*` sprites.
- Node sprites (locked/available/learned) — can be used as `backgroundImage` in `SkillRowUI.UpdateState()` instead of solid colors.

### 4. Skill System General Check
- **SkillTreeUI open/close:** Toggle key (K or Tab?) → `panel.SetActive` works?
- **Skill learning:** `OnLearnSkill` callback → `SkillSystem.LearnSkill` chain.
- **Skill equipping:** `OnEquipSkill` callback → equip bar updates.
- **Skill data loading:** `skills.json` parsed correctly by `DataManager`.

### 5. Skill Bar (HUD) Icons
- V-003 (RESERVE) describes "skill bar icon display" — also affected.
- Check if HUD skill slots show icons or empty black squares.

## Fix Direction
1. Add icon Image to `SkillRowUI`, wire to `SkillDef.icon` sprite path.
2. Load from `Resources/Sprites/` or skill-specific path.
3. Optionally replace solid color backgrounds with A-006 node sprites.
4. Verify full skill learn → equip → use chain.

## Verification
- Skill tree panel opens with icons per skill row.
- Learn / equip flow works end-to-end.
- Skill bar in HUD shows equipped skill icons.
