# TODO | ISSUES

## Priority

---

- !ISSUE: Mouse hover highlighting is selecting multiple icons.
- !ISSUE: If a Quick Action is picked; the Ring doesn't re-calculate spacing around.
- FIXED: v3.0.1.0 - !ISSUE: Icon spacing is off when lot's of icons are present.

## QoL

- Improve Item lookup logic (Store a static list?)
- Figure out better math for the Checkbox positioning. (Currently working)
- Auto populate `GearSets` when in GearSet menu.
  - https://github.com/aers/FFXIVClientStructs/blob/62ea2008fc5a1a88733653925dcea946d512eb1c/FFXIVClientStructs/FFXIV/Client/UI/Misc/RaptureGearsetModule.cs#L34

## Icon Hovering/Highlighting

- See about getting the font used for Item Count and using that for Tooltips/etc.
  - Possibly see about writing the Item name in the middle of the circle when hovered.
- Auto spacing/Adjust scaling of Icon when mousing over the Icon we could subtract a flat value or % from the angle value.
