Pixel Editor Window
==============================

This scripts adds a `Pixel Editor` window to unity. This window allows you to edit pixels of a texture like a very simple paintbrush app.

There is a shader/material you can assign to the window for a more enhanced editing view (transparency visualisation, grids etc). Without it, the texture is blitted in a very default way to the window.

Changes are not currently logged to undo/redo operations.

Editing pixels edits the texture live (so reflected immediately in-game) but the asset itself is not saved (to a source `PNG`/`JPEG`/`EXR` file) until you click `Save`. Similarly `revert` essentially reloads the asset contents from disk and undoes your changes.

Controls
------------------------------
- Middle mouse to drag 
- Scroll wheel to zoom
- Left mouse to paint with the "Left Colour"
- Right mouse to paint with the "Right Colour"
