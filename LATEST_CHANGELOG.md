## v1.39.0 (minor)

Changes since v1.38.0:

- Refuse a class that holds itself by value (Claude)
- Read a null property as omitted, and refuse a null list element by its path (Claude)
- Read a type whose discriminator is not first, and refuse one with none (Claude)
- Refuse a null in the C++ options file instead of throwing (Claude)
- Refuse schema documents with root properties the model does not read (Claude)
- Save schemas through a temporary file so a failed write keeps the last save (Claude)
- Move the editor onto ktsu.UndoRedo 3 so the ImGui 3.75 packages build (Claude)
- Regenerate API compatibility suppressions so packing succeeds again (Claude)
- Merge remote-tracking branch 'origin/main' into dependabot/nuget/Polyfill-11.4.5 (matt-edmondson)

