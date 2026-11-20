# Pip the taskbar fox

Pip is a little red fox for Windows 11. I chose a fox because it explores quietly, notices small details, and tries a different route when it gets stuck. Pip brings that curiosity to the taskbar, one suspicious link or misplaced bug at a time.

## Run

Double-click **Pip.exe** or run this from the project folder in PowerShell.

```powershell
.\Pip.exe
```

Pip uses the .NET Framework included with Windows 11. The executable includes all of the artwork and runs offline.

- Grab Pip with the left mouse button. Flick and release to toss him.
- Leave the keyboard and mouse alone for a minute and Pip curls up. Move the mouse or press a key to wake him.
- Copy a link, number, code snippet, or a passage of 180 characters or more for four different reactions. Pip classifies the text locally and keeps the copied content out of his speech bubbles.
- Click or right-click Pip in the system tray for **Feed**, **Put to sleep**, and **Exit**. Open Windows' hidden icons arrow if Pip is in the overflow. **Wake Pip** appears while he's asleep. A tap on the fox also wakes him.

To rebuild from the source and bundled sprites, run `powershell -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1`. The build uses the Windows C# compiler and requires no downloads.

## Artwork

Codex's built-in GPT Image tool (`image_gen`) produced two transparent atlases. The 13 extracted PNGs live in `assets/sprites/`, and `assets/pip.ico` uses the same fox.

<details>
<summary>Motion atlas prompt</summary>

> Create one professional game sprite ATLAS on a genuinely transparent alpha canvas. This is for Pip, an original tiny Windows desktop pet. EXACTLY NINE fully isolated full-body sprites of the SAME young red fox in a precisely spaced 3 columns by 3 rows sheet. Square canvas, equal invisible cells, generous empty gutters. Put the center of each sprite within its cell, leave at least 12% side padding in every cell (especially the tail), never let the sprites touch or overlap. NO drawn grid, no labels, no numbers, no text, no scenery, no ground, no shadows outside the silhouette, no checkerboard; actual transparent alpha.
>
> Character model: a small clever red fox kit with a very big curved russet brush tail with an ivory tip, copper-orange coat (#dc713e), cream muzzle/bib and cheeks, espresso brown stockings, big triangular dark-rimmed ears, shiny ink-black eyes, one tiny sea-glass teal neckerchief tied with a gold disk on the side. Fox proportions are cartoony, a large expressive head, low round compact body, four short paws. Always the exact same details and fur colors. Premium hand-inked game sticker art with bold clean dark aubergine contours, a few warm cel-shaded planes, subtle painted edge highlights, strong simple readable silhouette even at 96 pixels. No white sticker border. Side or 3/4 view, all poses face RIGHT (tail on left except when curled asleep). The character should occupy a similar scale in all cells.
>
> Row 1 left to right: (1) standing still on all four paws, head proudly up, bright curious smile, bushy tail behind; (2) right-facing WALK CONTACT pose, front near paw reaches forward and far hind paw reaches back, level spine; (3) same right-facing WALK OPPOSITE CONTACT pose, near hind paw reaches forward and far front paw reaches forward, other legs opposed, lively but NOT jumping.
> Row 2 left to right: (4) sitting relaxed on haunches with front paws together, bright open eyes and upright ears; (5) dangling in the air as if a gentle hand has picked up the scruff, no hand drawn, all four paws loosely dangle, wide surprised eyes, tail hangs down; (6) airborne, jubilant skydiving fox, paws stretch outward, tail streams behind, expressive surprised joy.
> Row 3 left to right: (7) asleep, curled into a compact cinnamon bun, eyes shut, chin on paws, cream tipped tail wrapped around its body, no Z lettering; (8) sitting upright on haunches holding a small round golden biscuit in both front paws and taking a happy bite, biscuit only as part of sprite; (9) low grounded landing squash, belly and paws low to ground, tail out, little determined expression, no effects.
>
> This is an actual usable sprite atlas. No duplicates, no extras. Use clean alpha around every silhouette and keep consistent visual scale and design across the nine positions.

</details>

<details>
<summary>Clipboard atlas prompt</summary>

> Draw a NEW companion animation atlas for the exact Pip fox in the provided pose sheet. The input is the character model and style reference only. Output four NEW acting poses of that very same character, exactly one fox per tile in a 2 columns by 2 rows square sprite atlas. Genuine transparent-alpha background. Equal invisible tile sizes with very generous gutters. Every fox and its prop are fully visible and isolated with at least 12% padding on all sides. No grid lines, frame, ground plane, cast shadow, lettering, captions, watermarks, disconnected marks or decorative flourishes.
>
> Preserve the character from the reference precisely: same wide shiny black eyes and short cream muzzle, black-brown paws, warm copper-orange fur outlined in rich dark plum, same very large curled ivory-tipped brush tail, same cream ear centers, same dark sage-teal neckerchief with small golden circular clasp on the front. Match the bold, subtly textured cel-shaded hand-inked illustration and warm autumn palette. Fox's large face is clearly visible at each pose. Draw all four at the same scale and at a consistent floor line within each tile, with face turned RIGHT or 3/4 right. These sprites must read clearly at 110 px wide.
>
> Top left, curious about a link: with big raised ears, Pip sits leaning forward and lets a tiny sea-glass green two-link chain dangle from one lifted front paw, nose very close, eyebrows raised in adorable expectation. Chain must touch the paw.
> Top right, counting: Pip sits on his hindquarters and cradles exactly three golden acorns against his cream bib with his two paws, comically intense, one eyebrow high and one low. Acorns touch paws.
> Bottom left, debugging: Pip lies low on his belly, tail held high behind, wielding a tiny wooden-handled round magnifying glass with one front paw, squinting intently through it at a ladybug on his other paw. Face readable and lifted slightly, no marks detached from silhouette.
> Bottom right, overwhelmed by a long passage: a long unprinted warm ivory paper scroll coils loosely around the seated fox's belly like a blanket. The tail, eyes, both ears and teal neckerchief remain visible. His expression is comically tired, cheek resting softly on the paper like a pillow. Roll should touch fox and stay within the tile, without text or symbols.
>
> Keep the graphic production quality and the exact same character design and colors as the reference sheet.

</details>
