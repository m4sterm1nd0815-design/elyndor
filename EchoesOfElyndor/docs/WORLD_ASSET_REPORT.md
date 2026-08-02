# World Asset Report

## Wiederverwendete Projektbestände

- Natur Pack: Basisvegetation, Felsen und Bodenpflanzen.
- Ultimate Stylized Nature: regionale Baum-, Busch- und Felsvarianten.
- Textured Stylized Trees: Birken, Laub- und Totholzvarianten.
- Modular Temple und Ruinenbestände: Landmarken und Siedlungsspuren.
- Quaternius Farm Buildings: geeignete CC0-Kandidaten für Sonnenfelder.
- Vorhandene URP-Prototypmaterialien, Terrain-Layer, Wind- und Wassershader.

Der neue additive Visual-Layer verwendet ausschließlich bereits importierte
Materialien und Unity-Primitiven. Es wurden keine externen Assets geladen und
keine Lizenzen ergänzt.

## Nicht verwendet

Unitypackage-Archive und 7z-Inhalte aus `_AssetInbox` werden nicht automatisch
importiert. Herkunft, Paketversion und Weitergaberecht sind dort nicht für
jedes Archiv ausreichend belegt. Assets mit unklarer Lizenz bleiben außerhalb
der produktiven `Assets/`-Struktur.

## Offener Bedarf

- regionstaugliche modulare Stege und Moorhütten,
- abgestimmte Ruinenmodule mit LODs,
- einheitliche Obstbäume und Feldrandvegetation,
- regionale Ambient-Loops mit klarer kommerzieller Lizenz,
- Hero-Landmarken für spätere Kapitelregionen.

## Finsterwald Root Gate (Meshy import)

- Source candidate: Finsterwald_RootGate_Meshy.fbx (1,197,020 bytes).
- Final path: Assets/_Elyndor/Art/Environment/Finsterwald/RootGate/.
- Unity import: one mesh, 29,107 vertices, one submesh and one renderer.
- Materials: one assigned material slot; no missing material references.
- Orientation: upright at zero rotation; scene yaw is applied by the world builder.
- Pivot: centered vertically in the source model. PlaceModel grounds the rendered
  bounds on the terrain, so the source pivot does not cause floating placement.
- Prefab: Finsterwald_RootGate.prefab.
- Integration: replaces the procedural Root Gate placeholder in the generated
  Finsterwald visual layer. Final appearance, collision clearance and camera
  occlusion still require the documented Play Mode walkthrough.
- Gate-0 material correction: the FBX contained only an unusable internal
  fallback material and no productive external texture references. Embedded
  material extraction is therefore intentionally disabled.
- Production appearance: explicit URP/Lit material
  `Materials/Finsterwald_RootGate_Stylized.mat` with a deterministic 512 px
  brown, weathered-grey and moss-green albedo. The prefab renderer references
  this material directly; geometry, collider, scale and placement are unchanged.
- `MANUELL OFFEN`: final in-scene visual approval under representative
  Finsterwald lighting after this correction is integrated.
