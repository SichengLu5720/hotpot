# TASK-002 v9 — glyph / safe-area evidence

Input HC-01-v9 Accepted. Parent confirmed bounded output after HC-01-v9-r1 UI ownership split. Target HC-02-v9. Rollback HC-03-v8 platform baseline; old zero-font constraint is not reused. Human readability judgment Pending.

## Font choice and size intent

- Existing `Unity/Assets/HotpotSort/Resources/Hotpot/TASK001/v7/r001/fonts/readable.otf` and adjacent `NotoSansCJKsc-LICENSE.txt` supply the verifiable SIL OFL source. No invented license, downloaded font or OS-only game-font dependency.
- Modern Noto Sans CJK for titles, body, buttons, digits and status text. No calligraphy title in the new candidate. The initial typography/readability board predates PM skin split and is superseded by typography-modern.png; do not hand the older title style to implementation.
- At 360px evidence width: body14.7px, notes12px minimum, example timer21.3px, display title32px. Use these as readable content priorities; do not shrink long text automatically below minimum to fit. Wrap or increase card height within safe area.
- Keep state sentences concise and explicit. No unverified quota, cooldown or success values shown in the font samples. Open-data-domain nicknames remain system-font content owned by TASK-008, not restricted to the game's static Chinese subset.
- Proposed subset input: exact production UI strings across all pages, errors, rewards, privacy/unavailable states plus punctuation, Arabic digits, MM:SS separators, Latin technical fallback strings actually shipped. Include dynamic numerals, ellipsis, slash and full-width punctuation. Final subset must be made from complete source string inventory after state copy freezes, retain license, and run glyph coverage against the produced font. This preview does not claim a complete subset or measured package budget.

## Safe-area geometry

- Preserve the420×900 board with one shared uniform mapping for visuals and hit input. Scale inside usable rectangle, including notch/status/gesture insets and platform menu exclusion. Do not independently reposition food or change292 crop semantics to make a phone screenshot look fuller.
- safe-area.png uses an illustrative1080×1920 canvas, top108/bottom72 reserved areas and centered978×1740 content. These are demonstration values, not a claimed phone model or fixed production margins.
- Menu outline, green safe rectangle and explanatory labels are technical overlays, not product UI. No important text/control under menu or home indicator. A/B cards and shell will replace the older frame later without changing this geometry constraint.
- Need runtime WeChat safe-area/menu-rectangle evidence for portrait aspect ratios, actual font import/rasterization, text fallback and open-data-domain nickname rendering. Desktop GDI glyph output is not a WeChat font-compatibility certification.

## Scope and status

No Unity/code/resource/font subset/build/package mutation. Source artwork untouched. No ImageGen call: deterministically rendered text and layout evidence reuse existing approved raster assets. AppID, advertisement unit and cloud environment IDs are not read, guessed or embedded; ad/cloud IDs remain external integration dependencies.

Expected Change: modern readable text plus real safe-area fit. No Difference: logical board mapping and approved food/container art. Uncertain: final skin fit, exported subset coverage/size, actual device rasterization and platform layouts. Pending style is owned by the separate A/B skin workstream. Unaffected work may continue.
