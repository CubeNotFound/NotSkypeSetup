# Skype 7.0.0.102 UI inspection

The supplied unpacked executable contains Delphi/VCL binary form resources rather than ordinary Win32 `RT_DIALOG` templates.

## Main form (`TSSMAINFORM`)

Key values read directly from the binary DFM resource:

- Class: `TssMainForm`
- Client size: **720 × 490**
- Font: **Tahoma**, height `-11` at **96 DPI**
- Scaling: disabled (`Scaled = False`)
- Header host: `(0,0,720,120)`
- Main page host: `(20,156,680,270)`
- Bottom button strip: `(0,446,720,44)`
- Main page columns: left `312`, separator host `48`, right `312`
- Progress bar: `(128,152,150,17)` relative to the page host, style `pbstMarquee`

The form contains these original tab pages:

- `tsMain`
- `tsPlugin`
- `tsProgressBar`
- `tsFinish`
- `tsProblems`
- `tsYandex`
- `tsBing`
- `tsWLM`

## Original embedded PNG resources used

- `10101`: 102 × 46 Skype wordmark
- `10201`: 720 × 112 header/cloud artwork
- `10301`: 48 × 71 information icon and reflection
- `10401`: 48 × 71 error icon and reflection
- `10501`: 48 × 71 warning icon and reflection
- `10701`: 320 × 280 Click-to-Call illustration
- `10801`: 326 × 255 Windows Live Messenger migration illustration
- `20101`: 326 × 255 Bing offer artwork
- `20102`: 325 × 195 alternate Bing offer artwork

The project embeds the extracted PNG bytes directly; these images were not redrawn.


## Screenshot-matched corrections

The 7.0 skin now follows the supplied original screenshots rather than using the
initial generic DFM-to-WinForms approximation:

- 720 x 490 client area.
- Main content origin at (24, 163).
- 337 px divider and 361 px right-column origin.
- 7.0 headings use the fixed Skype blue RGB(0, 175, 240).
- 59/62 px grey footer using RGB(235, 235, 235), with RGB(218, 218, 218) separator.
- Installing page uses the original full-width 672 x 32 marquee at (24, 260).
- Exit dialog uses the separate 7.0 grey chrome and an icon/reflection crop from
  the supplied original 7.0 dialog screenshot.
- Accent colours are selected internally by SetupUiVersion and are no longer
  configurable in installer.config.xml.

## Fine typography pass

The 7.0 branch now follows the original Delphi/VCL form font more closely:

- Tahoma, 8.25 pt for normal text and native controls.
- Tahoma, 10 pt bold for blue page headings.
- Tahoma, 8.25 pt bold for the exit-dialog heading.
- The startup checkbox remains visible when More Options is collapsed, as in the original 7.0 installer.
- More Options now controls only the install path, Browse button, and desktop-icon option.
- The 7.0 exit-dialog mark is positioned independently from the 7.41 mark.

The 7.41 page builders, fonts, assets, and geometry were not changed by this pass.
