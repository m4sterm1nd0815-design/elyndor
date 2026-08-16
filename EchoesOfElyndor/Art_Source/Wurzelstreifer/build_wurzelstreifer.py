"""Baut den Wurzelstreifer aus dem freigegebenen Konzeptentwurf.

Vorgabe: docs/07_Enemies/WURZELSTREIFER_CONCEPT_BRIEF.md (Status FREIGEGEBEN).
Standard: docs/Technical/BLENDER_ASSET_PIPELINE.md.

Das Skript ist die Quelle des Assets, nicht die .blend. Wer das Modell
nachbauen will, laesst dieses Skript laufen; die .blend ist sein Ergebnis.
Deshalb steht hier jede Zahl, die im Konzept steht, als benannte Konstante und
nicht als Zwischenwert in einer Modellieroperation.

Reproduzierbarkeit: fester Seed, keine Zufallszahl ohne ihn, keine Handgriffe
im Viewport. Zweimal laufen lassen ergibt dasselbe Mesh.

Aufruf (ueber die MCP-Bruecke oder aus Blender heraus):

    exec(open(r"<Pfad>/build_wurzelstreifer.py").read())
    build_all()
"""

import bmesh
import bpy
import math
import os
import random
from mathutils import Euler, Matrix, Quaternion, Vector

# ---------------------------------------------------------------------------
# Namen und Pfade
# ---------------------------------------------------------------------------

ASSET_NAME = "ELY_Enemy_Wurzelstreifer"
MESH_OBJECT_NAME = ASSET_NAME + "_Mesh"
ARMATURE_OBJECT_NAME = ASSET_NAME + "_Rig"

MATERIAL_BARK = "M_" + ASSET_NAME
MATERIAL_CRACK = "M_" + ASSET_NAME + "_Risse"

SEED = 20260816

# ---------------------------------------------------------------------------
# Masse aus dem Konzeptentwurf (Abschnitt 2, 6 und 7)
# ---------------------------------------------------------------------------

#: Schulterhoehe, Zielband 0,85-0,95 m.
SHOULDER_HEIGHT = 0.89

#: Koerperlaenge ueber alles, Zielband 1,6-1,8 m.
BODY_LENGTH = 1.75

#: Modelliert wird nach +Y, ausgeliefert wird nach -Y.
#:
#: Der Aufbau rechnet der Lesbarkeit halber in +Y: alle Zahlen in SPINE,
#: LEG_PLAN und TAIL_STRANDS steigen von hinten nach vorn. Kurz vor dem
#: Ablegen wird das Ganze um 180 Grad um Z gedreht, sodass die fertige Datei
#: nach -Y zeigt.
#:
#: Warum -Y und nicht +Y: gemessen am 16.08.2026 mit dem Standardexport
#: (axis_forward='-Z', axis_up='Y') kommt Blenders +Y in Unity als **-Z** an,
#: und -Z ist in Unity hinten. Ein nach +Y modelliertes Tier laeuft im Spiel
#: rueckwaerts. Die erste Fassung dieses Skripts hat genau das getan; die
#: Achsprobe in Unity hat es mit "Becken->Kopf = (0, 0, -0,620)" aufgedeckt.
#:
#: Die Drehung sitzt bewusst hier und nicht im Exportaufruf: der Exportstandard
#: aus BLENDER_ASSET_PIPELINE.md ist gemessen und gilt fuer alle Assets. Ihn
#: fuer ein einzelnes Modell zu veraendern hiesse, zwei Exportwege zu haben.
#:
#: Eine 180-Grad-Drehung um die Hochachse spiegelt nicht — Normalen und
#: Haendigkeit bleiben. Die Bezeichnungen "rechts" und "links" der Knochen
#: bleiben ebenfalls richtig: das Tier zeigt danach nach -Y, und rechts ist
#: vorn x oben = (0,-1,0) x (0,0,1) = -X. Was vorher bei +X lag, liegt jetzt
#: bei -X und ist weiterhin die rechte Seite.
FORWARD_BUILD = Vector((0.0, 1.0, 0.0))
FORWARD_DELIVERED = Vector((0.0, -1.0, 0.0))

#: Die Ruecken- und Kopflinie als eine durchgehende Kurve:
#: (y, Mittenhoehe, Halbbreite, Halbhoehe).
#:
#: Eine einzige Kurve statt Rumpf plus angesetztem Kopf, weil Rindenplatten,
#: Bruchlinien und Beine alle auf derselben Flaechenparametrisierung sitzen.
#: Zwei getrennte Volumen haetten an der Naht eine zweite Parametrisierung
#: gebraucht, und genau dort sitzt die Gesichtsrinde.
#:
#: Die Rueckenlinie faellt vom kraeftigen Vorderkoerper zum leichteren
#: Hinterkoerper ab; der Kopf sitzt tief zwischen den Schultern und steigt zur
#: Brauenpartie noch einmal leicht an, damit er als Kopf liest und nicht als
#: auslaufende Roehre.
SPINE = [
    (-0.700, 0.615, 0.050, 0.050),   # Rumpfende
    (-0.580, 0.634, 0.100, 0.110),
    (-0.420, 0.652, 0.132, 0.148),   # Huefte
    (-0.160, 0.664, 0.124, 0.144),   # Taille
    ( 0.140, 0.700, 0.152, 0.172),   # Brust
    ( 0.340, 0.714, 0.158, 0.176),   # Schulter, hoechster Punkt 0,890
    ( 0.460, 0.664, 0.100, 0.098),   # Hals — kurz und schmal, nicht gestreckt
    ( 0.560, 0.662, 0.112, 0.116),   # Schaedel, Braue: wird wieder breiter
    ( 0.630, 0.640, 0.102, 0.098),   # Augenlinie
    ( 0.700, 0.612, 0.076, 0.070),   # Schnauzenansatz
    ( 0.755, 0.594, 0.050, 0.045),   # Schnauze
    ( 0.800, 0.582, 0.025, 0.022),   # Schnauzenspitze
]

#: Der geteilte Wurzelfortsatz schleift dahinter aus.
Y_TAIL_TIP = -0.950

#: Aufloesung. Nicht hoeher als noetig: die Pipeline verlangt die niedrigste
#: Dreieckszahl, bei der die Silhouette noch liest.
BODY_RINGS = 34
BODY_SEGMENTS = 18

#: Laengsposition der Beine als Y-Wert auf der Rumpfachse: unter der Schulter
#: und unter der Huefte, nicht auf halbem Weg dazwischen.
Y_FRONT_LEG = 0.300
Y_REAR_LEG = -0.400


# ---------------------------------------------------------------------------
# Die Rumpfflaeche als Funktion
# ---------------------------------------------------------------------------


def _lerp(a, b, f):
    return a + (b - a) * f


def _smooth(f):
    return f * f * (3.0 - 2.0 * f)


def _spine_sample(t):
    """Mitte, Halbbreite und Halbhoehe der Achse bei t in [0..1]."""
    t = min(max(t, 0.0), 1.0)
    span = t * (len(SPINE) - 1)
    i = min(int(math.floor(span)), len(SPINE) - 2)
    f = _smooth(span - i)

    y0, z0, x0, h0 = SPINE[i]
    y1, z1, x1, h1 = SPINE[i + 1]

    return (Vector((0.0, _lerp(y0, y1, f), _lerp(z0, z1, f))),
            _lerp(x0, x1, f),
            _lerp(h0, h1, f))


def _spine_frame(t):
    """Mittelpunkt und ein Achsenkreuz, das der Kurve folgt.

    Die Ringe stehen senkrecht auf der Achse. Ohne das wuerden sie am
    absteigenden Kopf schleifen, statt ihn zu umschliessen — die Schnauze
    saehe von der Seite richtig aus und von oben zerquetscht.
    """
    eps = 1e-3
    center, half_x, half_z = _spine_sample(t)
    ahead, _, _ = _spine_sample(min(1.0, t + eps))
    behind, _, _ = _spine_sample(max(0.0, t - eps))

    tangent = ahead - behind
    if tangent.length < 1e-9:
        tangent = Vector((0.0, 1.0, 0.0))
    tangent.normalize()

    side = tangent.cross(Vector((0.0, 0.0, 1.0)))
    if side.length < 1e-9:
        side = Vector((1.0, 0.0, 0.0))
    side.normalize()
    # Die Achse liegt in der YZ-Ebene, deshalb zeigt "side" stabil nach +X.
    side = -side if side.x < 0.0 else side

    up = side.cross(tangent).normalized()
    up = -up if up.z < 0.0 else up

    return center, side, up, half_x, half_z


def _asymmetry(t, angle):
    """Die Silhouette ist links und rechts nicht spiegelgleich.

    Der Konzeptentwurf verlangt das ausdruecklich, und zwar so, dass es von
    vorn zuerst auffaellt: eine Schulter hoeher als die andere. Der Ausschlag
    bleibt klein — Asymmetrie soll auffallen, nicht deformiert wirken.
    """
    # Rechte Haelfte (cos(angle) > 0 zeigt nach +X) an der Schulter anheben.
    shoulder = math.exp(-((t - 0.455) ** 2) / 0.010)
    lift = 0.024 * shoulder * max(0.0, math.cos(angle))

    # Eine zweite, schwaechere Beule weiter hinten links, damit die Asymmetrie
    # nicht wie ein einzelner Fehler aussieht.
    hip = math.exp(-((t - 0.182) ** 2) / 0.008)
    lift += 0.013 * hip * max(0.0, -math.cos(angle))

    return lift


def _bark_relief(t, angle, rng_table):
    """Unregelmaessige Rindenoberflaeche, kein Raster.

    Die Werte kommen aus einer vorab mit festem Seed gezogenen Tabelle, damit
    zwei Laeufe dasselbe Relief ergeben.
    """
    relief = 0.0
    for freq_t, freq_a, phase_t, phase_a, amp in rng_table:
        relief += amp * math.sin(freq_t * t * math.pi * 2.0 + phase_t) \
            * math.sin(freq_a * angle + phase_a)
    return relief


def body_point(t, angle, rng_table):
    """Ein Punkt auf der Rumpfoberflaeche.

    Die Flaeche bleibt als Funktion verfuegbar, weil Rindenplatten und die
    tuerkisen Bruchlinien spaeter genau auf ihr liegen muessen. Sie
    nachtraeglich auf ein fertiges Mesh zu projizieren waere ungenauer und
    schlechter nachvollziehbar.
    """
    center, side, up, half_x, half_z = _spine_frame(t)

    relief = _bark_relief(t, angle, rng_table)
    # Am Kopf und am Rumpfende faellt das Relief aus: dort ist zu wenig
    # Flaeche, als dass es noch als Rinde und nicht als Delle laese.
    relief *= min(1.0, 5.0 * t) * min(1.0, 5.0 * (1.0 - t))

    rx = half_x * (1.0 + relief)
    rz = half_z * (1.0 + relief)

    return (center
            + side * (rx * math.cos(angle))
            + up * (rz * math.sin(angle) + _asymmetry(t, angle)))


def body_normal(t, angle, rng_table, eps=1e-3):
    """Aussennormale der Rumpfflaeche, numerisch aus zwei Tangenten."""
    p = body_point(t, angle, rng_table)
    dt = body_point(min(1.0, t + eps), angle, rng_table) - p
    da = body_point(t, angle + eps, rng_table) - p

    n = da.cross(dt)
    if n.length < 1e-9:
        center, side, up, _, _ = _spine_frame(t)
        return (side * math.cos(angle) + up * math.sin(angle)).normalized()

    n.normalize()

    center, _, _, _, _ = _spine_frame(t)
    outward = p - center
    if outward.length > 1e-9 and n.dot(outward) < 0.0:
        n = -n

    return n


def _t_at_y(y):
    """Der Achsenparameter, an dem die Achse die Hoehe y erreicht."""
    lo, hi = 0.0, 1.0
    for _ in range(40):
        mid = (lo + hi) * 0.5
        center, _, _ = _spine_sample(mid)
        if center.y < y:
            lo = mid
        else:
            hi = mid
    return (lo + hi) * 0.5


# ---------------------------------------------------------------------------
# Rumpf
# ---------------------------------------------------------------------------


def _build_body(bm, rng_table):
    """Der Rumpf als Loft aus Ringen. Reine Vierecke ausser an den Kappen."""
    rings = []

    for i in range(BODY_RINGS + 1):
        t = i / BODY_RINGS
        ring = []
        for s in range(BODY_SEGMENTS):
            angle = 2.0 * math.pi * s / BODY_SEGMENTS
            ring.append(bm.verts.new(body_point(t, angle, rng_table)))
        rings.append(ring)

    faces = []
    for i in range(BODY_RINGS):
        a, b = rings[i], rings[i + 1]
        for s in range(BODY_SEGMENTS):
            n = (s + 1) % BODY_SEGMENTS
            faces.append(bm.faces.new((a[s], a[n], b[n], b[s])))

    # Kappen: hinten und vorn je ein n-Gon, das der Triangulierer spaeter
    # aufloest. Ein Faecher aus einem Mittelpunkt waere hier nur mehr
    # Geometrie ohne Gewinn.
    faces.append(bm.faces.new(tuple(reversed(rings[0]))))
    faces.append(bm.faces.new(tuple(rings[-1])))

    return faces


# ---------------------------------------------------------------------------
# Roehren fuer Beine, Wurzelstraenge und Fortsatz
# ---------------------------------------------------------------------------


def _build_tube(bm, path, radii, segments=8, cap_start=True, cap_end=True):
    """Eine Roehre entlang eines Pfades.

    ``path`` sind Mittelpunkte, ``radii`` die zugehoerigen Radien. Die Ringe
    stehen senkrecht auf der Pfadrichtung; ohne das knicken Wurzelstraenge an
    jeder Biegung ein.
    """
    rings = []
    up_ref = Vector((0.0, 0.0, 1.0))

    for i, center in enumerate(path):
        if i == 0:
            direction = path[1] - path[0]
        elif i == len(path) - 1:
            direction = path[-1] - path[-2]
        else:
            direction = path[i + 1] - path[i - 1]

        if direction.length < 1e-9:
            direction = Vector((0.0, 0.0, -1.0))
        direction.normalize()

        ref = up_ref if abs(direction.dot(up_ref)) < 0.95 else Vector((0.0, 1.0, 0.0))
        side = direction.cross(ref)
        if side.length < 1e-9:
            side = Vector((1.0, 0.0, 0.0))
        side.normalize()
        other = direction.cross(side).normalized()

        radius = radii[i]
        ring = []
        for s in range(segments):
            angle = 2.0 * math.pi * s / segments
            offset = side * (radius * math.cos(angle)) + other * (radius * math.sin(angle))
            ring.append(bm.verts.new(center + offset))
        rings.append(ring)

    faces = []
    for i in range(len(rings) - 1):
        a, b = rings[i], rings[i + 1]
        for s in range(segments):
            n = (s + 1) % segments
            faces.append(bm.faces.new((a[s], a[n], b[n], b[s])))

    if cap_start:
        faces.append(bm.faces.new(tuple(reversed(rings[0]))))
    if cap_end:
        faces.append(bm.faces.new(tuple(rings[-1])))

    return faces


def _leg_path(hip, knee, paw):
    """Ein Bein als weicher Zug durch Huefte, Gelenk und Pfote."""
    points = []
    for f in [0.0, 0.22, 0.42, 0.58, 0.72, 0.86, 1.0]:
        if f <= 0.5:
            g = _smooth(f / 0.5)
            points.append(hip.lerp(knee, g))
        else:
            g = _smooth((f - 0.5) / 0.5)
            points.append(knee.lerp(paw, g))
    return points


#: Schlanke Beine, an den Gelenken dicker: dort sitzen die Wurzelstraenge
#: dichter, und ohne die Verdickung sieht das Bein an genau der Stelle duenn
#: aus, an der es tragen soll.
LEG_RADII = [0.062, 0.050, 0.041, 0.046, 0.035, 0.031, 0.037]


#: Ein Bein: (Kuerzel, Y auf der Achse, Seite, Pfoten-X, Knieversatz,
#: Pfotenversatz, Staerke).
#:
#: +X ist rechts: der Koerper zeigt nach +Y, oben ist +Z, also ist
#: rechts = vorn x oben = +X. Daran haengen die Bone-Namen.
#:
#: Vorderkoerper etwas kraeftiger und hoeher als der Hinterkoerper — die
#: Vorderbeine stehen weiter aussen und sind eine Spur staerker. Die kleinen
#: Abweichungen je Bein sind Absicht: vier gleiche Beine lesen sich als Kopie,
#: nicht als gewachsenes Tier.
LEG_PLAN = [
    ("VR", Y_FRONT_LEG,  1.0, 0.130,  0.052,  0.022, 1.06),
    ("VL", Y_FRONT_LEG, -1.0, 0.127,  0.046, -0.016, 1.00),
    ("HR", Y_REAR_LEG,   1.0, 0.110, -0.058, -0.020, 0.92),
    ("HL", Y_REAR_LEG,  -1.0, 0.113, -0.052,  0.024, 0.96),
]


def leg_joints(entry):
    """Huefte, Gelenk und Pfote eines Beins.

    Mesh und Rig lesen dieselbe Funktion. Haetten sie je eigene Zahlen, waere
    die erste Aenderung an einer Beinlaenge diejenige, bei der die Knochen
    neben dem Bein liegen.
    """
    _, y, side, paw_x, knee_dy, paw_dy, weight = entry

    t = _t_at_y(y)
    center, axis_side, axis_up, half_x, half_z = _spine_frame(t)

    hip = center + axis_side * (side * half_x * 0.70) - axis_up * (half_z * 0.55)
    knee = Vector((side * (paw_x + 0.016), y + knee_dy, hip.z * 0.46))
    paw = Vector((side * paw_x, y + paw_dy, 0.028))

    return hip, knee, paw, weight


def _build_legs(bm, rng_table):
    faces = []

    for entry in LEG_PLAN:
        hip, knee, paw, weight = leg_joints(entry)

        radii = [r * weight for r in LEG_RADII]
        faces.extend(_build_tube(bm, _leg_path(hip, knee, paw), radii, segments=8))

        # Pfote: ein flacher Abschluss, damit das Bein nicht als abgeschnittene
        # Roehre auf dem Boden steht.
        faces.extend(_build_tube(
            bm,
            [Vector((paw.x, paw.y + 0.004, 0.030)),
             Vector((paw.x, paw.y + 0.030, 0.004))],
            [0.042 * weight, 0.029 * weight],
            segments=8))

        faces.extend(_build_root_wraps(bm, hip, knee, paw, weight))

    return faces


def _build_root_wraps(bm, hip, knee, paw, weight):
    """Wurzelstraenge, die das Bein umwickeln.

    Zwei Straenge je Bein, gegenlaeufig gewunden und an den Gelenken dichter.
    Mehr waere Rechenlast ohne Silhouettengewinn: aus Spielentfernung liest
    sich die Umwicklung ueber die Kontur, nicht ueber die Zahl der Straenge.
    """
    faces = []
    spine = _leg_path(hip, knee, paw)

    for turns, phase, thickness in [(1.6, 0.0, 0.016), (-1.3, 2.1, 0.013)]:
        path = []
        radii = []
        steps = 12

        for i in range(steps + 1):
            f = i / steps
            index = f * (len(spine) - 1)
            lo = int(math.floor(index))
            hi = min(lo + 1, len(spine) - 1)
            center = spine[lo].lerp(spine[hi], index - lo)

            leg_radius = _lerp(LEG_RADII[lo], LEG_RADII[hi], index - lo) * weight
            angle = phase + turns * 2.0 * math.pi * f

            # Die Wicklung liegt quer zum Bein. Das Bein laeuft im Wesentlichen
            # nach unten, deshalb genuegen X und Y als Querachsen.
            path.append(center + Vector((
                math.cos(angle) * (leg_radius + thickness * 0.6),
                math.sin(angle) * (leg_radius + thickness * 0.6),
                0.0)))

            # An den Gelenken dichter: dort schwillt der Strang an.
            joint = (1.0
                     + 0.55 * math.exp(-((f - 0.50) ** 2) / 0.020)
                     + 0.35 * math.exp(-((f - 0.92) ** 2) / 0.010))
            radii.append(thickness * joint * weight)

        faces.extend(_build_tube(bm, path, radii, segments=5))

    return faces


#: Der geteilte, wurzelartige Fortsatz.
#:
#: Kein Wolfsschwanz: zwei ungleich lange Straenge, die tief haengen und
#: beinahe schleifen. Er ist das Erkennungsmerkmal von hinten, deshalb sind die
#: Straenge bewusst verschieden lang, verschieden dick und verschieden stark
#: durchgebogen — und deutlich seitlich getrennt. Zwei Straenge hintereinander
#: in derselben Ebene ergeben aus dieser Ansicht genau einen.
#:
#: (Kuerzel, Endpunkt, Startradius, Endradius, seitlicher Ausschlag, Durchhang)
TAIL_STRANDS = [
    ("A", Vector(( 0.120, Y_TAIL_TIP,        0.070)), 0.050, 0.012,  0.078, 0.21),
    ("B", Vector((-0.110, Y_TAIL_TIP + 0.21, 0.195)), 0.036, 0.009, -0.068, 0.12),
]

TAIL_BASE = Vector((0.0, SPINE[0][0] + 0.03, SPINE[0][1] - 0.01))


def tail_path(strand, steps=11):
    """Die Mittellinie eines Fortsatzstrangs. Mesh und Rig lesen dieselbe."""
    _, tip, r0, r1, sway, sag = strand
    path = []
    radii = []

    for i in range(steps + 1):
        f = i / steps
        point = TAIL_BASE.lerp(tip, f)
        # Durchhang: der Strang faellt zuerst weit, bevor er auslaeuft. Ohne
        # ihn steht der Fortsatz wie ein Stock nach hinten ab.
        point.z -= sag * math.sin(math.pi * f ** 0.75)
        point.x += sway * math.sin(math.pi * f) * 0.85
        path.append(point)
        radii.append(_lerp(r0, r1, f ** 1.6))

    return path, radii


def _build_tail(bm, rng_table):
    faces = []

    for strand in TAIL_STRANDS:
        path, radii = tail_path(strand)
        faces.extend(_build_tube(bm, path, radii, segments=6))

    return faces


# ---------------------------------------------------------------------------
# Rindenplatten
# ---------------------------------------------------------------------------

#: (t, Winkel um die Laengsachse, Laenge, Breite, Dicke, Verdrehung)
#:
#: Die Platten liegen wie verschobene Schuppen, nie in einem Raster: die
#: Winkel wandern bewusst um die Ruecklinie herum und ueber die Flanken, und
#: keine zwei aufeinanderfolgenden Platten haben dieselbe Groesse.
#: 1,571 rad ist genau oben; kleinere Werte wandern nach rechts, groessere
#: nach links.
BARK_PLATES = [
    (0.095, 1.30, 0.100, 0.078, 0.017,  0.22),
    (0.125, 1.94, 0.088, 0.068, 0.015, -0.31),
    (0.155, 1.12, 0.116, 0.088, 0.021,  0.09),
    (0.185, 2.24, 0.084, 0.064, 0.014,  0.35),
    (0.215, 1.62, 0.124, 0.094, 0.022, -0.18),
    (0.245, 0.94, 0.098, 0.074, 0.016,  0.27),
    (0.275, 2.02, 0.112, 0.084, 0.019, -0.12),
    (0.305, 1.38, 0.130, 0.098, 0.023,  0.31),
    (0.335, 2.36, 0.090, 0.068, 0.015, -0.24),
    (0.365, 1.74, 0.120, 0.090, 0.021,  0.16),
    (0.395, 1.06, 0.134, 0.102, 0.024, -0.29),
    (0.425, 1.86, 0.114, 0.086, 0.020,  0.11),
    (0.455, 1.44, 0.126, 0.096, 0.022, -0.20),
    (0.485, 2.18, 0.094, 0.072, 0.016,  0.28),
    (0.515, 1.20, 0.100, 0.076, 0.017, -0.15),
    # Gesichtsrinde. Von vorn die deutlichste Flaeche: eine geschlossene,
    # unregelmaessige Partie ueber Braue und Wange, aus der nur eine Andeutung
    # eines Auges hervorsieht. Die linke Wange bleibt bewusst offener als die
    # rechte — auch das Gesicht ist nicht spiegelgleich.
    (0.620, 1.57, 0.076, 0.098, 0.016,  0.05),
    (0.660, 0.86, 0.066, 0.062, 0.014,  0.24),
    (0.690, 2.42, 0.054, 0.050, 0.011, -0.19),
    (0.740, 1.24, 0.058, 0.052, 0.012,  0.13),
    (0.790, 1.90, 0.046, 0.040, 0.010, -0.26),
]


def _build_bark_plates(bm, rng_table):
    """Rindenplatten wie verschobene Schuppen, nie in einem Raster."""
    faces = []

    for t, angle, length, width, thickness, twist in BARK_PLATES:
        center = body_point(t, angle, rng_table)
        normal = body_normal(t, angle, rng_table)

        along = (body_point(min(1.0, t + 0.02), angle, rng_table)
                 - body_point(max(0.0, t - 0.02), angle, rng_table))
        if along.length < 1e-9:
            along = Vector((0.0, 1.0, 0.0))
        along.normalize()

        across = normal.cross(along).normalized()

        # Verdrehung in der Flaeche: die Platten liegen nicht parallel.
        rot = Matrix.Rotation(twist, 3, normal)
        along = rot @ along
        across = rot @ across

        # Die Platte sitzt in der Oberflaeche, damit keine Fuge zwischen Platte
        # und Rumpf klafft.
        base = center - normal * (thickness * 0.55)

        corners = [base
                   + along * (length * 0.5 * sy)
                   + across * (width * 0.5 * sx)
                   for sx, sy in [(-1, -1), (1, -1), (1, 1), (-1, 1)]]

        # Die Oberseite ist kleiner und leicht verschoben: eine abgeschraegte,
        # gegen die Grundflaeche versetzte Platte liest sich als gewachsene
        # Rinde, ein Quader als angeschraubtes Blech.
        shift = along * (length * 0.10) + across * (width * 0.06)
        top = [center + (c - center) * 0.70 + normal * thickness + shift
               for c in corners]

        low = [bm.verts.new(c) for c in corners]
        high = [bm.verts.new(c) for c in top]

        faces.append(bm.faces.new(tuple(high)))
        for i in range(4):
            j = (i + 1) % 4
            faces.append(bm.faces.new((low[i], low[j], high[j], high[i])))
        faces.append(bm.faces.new(tuple(reversed(low))))

    return faces


def _build_eye(bm, rng_table):
    """Die Andeutung eines Auges — genau eines, auf der rechten Seite.

    Der Konzeptentwurf verlangt ein Gesicht, das teilweise von Rinde
    geschlossen ist und aus dem "nur ein Auge oder eine Andeutung davon"
    hervorsieht. Zwei Augen waeren ein Tiergesicht; eines ist der Befund, dass
    hier etwas nicht stimmt.
    """
    t, angle = 0.700, 0.62
    center = body_point(t, angle, rng_table)
    normal = body_normal(t, angle, rng_table)

    # Eine flache, facettierte Kuppel. Aus Spielentfernung liest sich davon die
    # dunkle Rundung, nicht die Zahl der Facetten.
    ring = []
    for s in range(6):
        a = 2.0 * math.pi * s / 6
        along = Vector((0.0, 1.0, 0.0))
        across = normal.cross(along).normalized()
        up = across.cross(normal).normalized()
        ring.append(bm.verts.new(
            center - normal * 0.004
            + across * (0.019 * math.cos(a))
            + up * (0.014 * math.sin(a))))

    apex = bm.verts.new(center + normal * 0.012)

    faces = []
    for s in range(6):
        n = (s + 1) % 6
        faces.append(bm.faces.new((ring[s], ring[n], apex)))

    return faces


# ---------------------------------------------------------------------------
# Tuerkise Bruchlinien
# ---------------------------------------------------------------------------

#: (t, Winkel, Laenge in t, Drift im Winkel, Breite)
CRACK_SEEDS = [
    (0.105, 1.05, 0.10, 0.85, 0.011),
    (0.150, 2.15, 0.09, -0.70, 0.009),
    (0.195, 1.62, 0.12, 1.05, 0.012),
    (0.240, 0.92, 0.09, -0.95, 0.010),
    (0.285, 2.42, 0.10, 0.80, 0.011),
    (0.330, 1.35, 0.13, -1.15, 0.013),
    (0.380, 2.02, 0.08, 0.65, 0.009),
    (0.420, 1.08, 0.07, -0.55, 0.008),
    (0.470, 1.72, 0.07, 0.70, 0.008),
    (0.130, 4.10, 0.10, 0.90, 0.010),
    (0.225, 4.55, 0.11, -0.85, 0.011),
    (0.320, 3.95, 0.09, 0.75, 0.009),
    (0.400, 4.68, 0.10, -1.00, 0.010),
    # Am Kopf feiner: dort ist weniger Flaeche, und ein grober Riss im Gesicht
    # kippt die Figur ins Daemonische, was der Entwurf ausschliesst.
    (0.600, 1.30, 0.05, 0.50, 0.007),
    (0.665, 2.05, 0.04, -0.40, 0.006),
    (0.720, 1.15, 0.04, 0.45, 0.006),
]


def _build_cracks(bm, rng_table):
    """Die Bruchlinien als eigene, flach aufliegende Baender.

    Warum Geometrie und keine Texturmaske: ``WurzelstreiferFeedback`` schreibt
    ``_EmissionColor`` ueber einen MaterialPropertyBlock auf den Renderer. Eine
    Maske braeuchte eine Emissionstextur — und einen Texturstandard, den das
    Projekt noch nicht hat (siehe BLENDER_ASSET_PIPELINE.md, "Fehlende
    Texturen"). Ein eigener Materialslot nur fuer die Risse erreicht dasselbe
    mit den Mitteln, die heute dokumentiert sind: der Rumpfslot hat das
    Emissionskeyword aus und bleibt von der Farbe unberuehrt, der Rissslot hat
    es an. Damit leuchten nur die Linien, nie der ganze Koerper.
    """
    faces = []

    for t0, a0, dt, da, width in CRACK_SEEDS:
        steps = 7
        left = []
        right = []

        for i in range(steps + 1):
            f = i / steps
            t = t0 + dt * f
            # Der Riss laeuft nicht gerade; er zackt leicht.
            angle = a0 + da * f + 0.09 * math.sin(f * math.pi * 3.0)

            point = body_point(t, angle, rng_table)
            normal = body_normal(t, angle, rng_table)

            along = (body_point(min(1.0, t + 0.01), angle + da * 0.01, rng_table)
                     - point)
            if along.length < 1e-9:
                along = Vector((0.0, 1.0, 0.0))
            along.normalize()

            across = normal.cross(along).normalized()

            # Der Riss verjuengt sich zu beiden Enden — eine Linie mit
            # konstanter Breite liest sich als aufgemalter Streifen.
            half = width * 0.5 * max(0.25, math.sin(math.pi * f) ** 0.5)

            # 2 mm ueber der Oberflaeche: nah genug, um aufzuliegen, weit
            # genug, um nicht mit dem Rumpf zu z-kaempfen.
            lifted = point + normal * 0.002

            left.append(bm.verts.new(lifted - across * half))
            right.append(bm.verts.new(lifted + across * half))

        for i in range(steps):
            faces.append(bm.faces.new((left[i], right[i], right[i + 1], left[i + 1])))

    return faces


# ---------------------------------------------------------------------------
# Aufbau des Meshes
# ---------------------------------------------------------------------------


#: Hilfsgruppe, die aufliegende Detailschalen markiert. Sie wird nach dem
#: Haeuten wieder entfernt und verlaesst die Datei nie.
DETAIL_GROUP = "ELY_Aufliegend"
DETAIL_GROUP_INDEX = 0


def _random_table():
    rng = random.Random(SEED)
    return [(
        rng.uniform(1.5, 4.5),      # Frequenz laengs
        rng.randint(3, 7),          # Frequenz um die Achse; ganzzahlig, damit
                                    # der Ring sich schliesst
        rng.uniform(0.0, math.tau),
        rng.uniform(0.0, math.tau),
        rng.uniform(0.012, 0.030),
    ) for _ in range(4)]


def build_mesh():
    """Baut das Mesh und gibt das Objekt zurueck."""
    rng_table = _random_table()

    bm = bmesh.new()

    body = _build_body(bm, rng_table)
    legs = _build_legs(bm, rng_table)
    tail = _build_tail(bm, rng_table)
    plates = _build_bark_plates(bm, rng_table)
    eye = _build_eye(bm, rng_table)
    cracks = _build_cracks(bm, rng_table)

    # Glatt schattiert bleibt, was organisch gewachsen ist; Rindenplatten und
    # Risse bleiben facettiert. mesh_smooth_type='FACE' schreibt das als
    # Glaettungsgruppen in die FBX, und Unity liest sie mit
    # importNormals=Import.
    for face in body + legs + tail:
        face.smooth = True
    for face in plates + eye + cracks:
        face.smooth = False

    # Materialslot 1 sind die Risse, Slot 0 alles andere.
    for face in cracks:
        face.material_index = 1

    # Rindenplatten, Augenkuppel und Rissbaender sind eigene Schalen, die auf
    # der Rumpfhaut aufliegen, ohne mit ihr verbunden zu sein. Sie werden
    # markiert, damit sie ihre Gewichte spaeter von der Haut darunter
    # uebernehmen koennen statt eigene zu bekommen. Ohne das loesen sie sich
    # sichtbar vom Koerper, sobald sich der Ruecken kruemmt.
    detail = bm.verts.layers.deform.verify()
    for face in plates + eye + cracks:
        for vert in face.verts:
            vert[detail][DETAIL_GROUP_INDEX] = 1.0

    bm.normal_update()

    # Doppelte Vertices nur dort verschmelzen, wo Ringe aneinanderstossen. Der
    # Abstand ist bewusst winzig: ein groesserer Wert wuerde die 2 mm ueber der
    # Oberflaeche liegenden Rissbaender in den Rumpf ziehen und ihre
    # Materialzuordnung mit einreissen.
    bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=1e-5)
    bm.normal_update()

    mesh = bpy.data.meshes.new(MESH_OBJECT_NAME)
    bm.to_mesh(mesh)
    bm.free()

    obj = bpy.data.objects.new(MESH_OBJECT_NAME, mesh)
    bpy.context.collection.objects.link(obj)

    # Muss die erste Gruppe sein: die Markierung oben liegt auf Index 0.
    obj.vertex_groups.new(name=DETAIL_GROUP)

    # Erst jetzt pruefen. mesh.validate() wirft Vertexgewichte weg, die auf
    # eine Gruppe zeigen, die es noch nicht gibt — und vor dieser Zeile gibt
    # es die Markierungsgruppe nicht. Vorher aufgerufen loescht die Pruefung
    # genau die Markierung, die sie schuetzen soll, und meldet dabei nichts.
    mesh.validate(verbose=False)

    _assign_materials(obj)

    # Das Rig muss um denselben Betrag verschoben gebaut werden wie das Mesh.
    # Der Wert wandert deshalb am Objekt mit, statt ein zweites Mal berechnet
    # zu werden — zwei Rechnungen fuer dieselbe Zahl laufen frueher oder
    # spaeter auseinander, und dann liegen die Knochen neben dem Koerper.
    shift = _center_and_ground(obj)
    obj["ely_shift"] = tuple(shift)

    _unwrap(obj)

    return obj


def _assign_materials(obj):
    """Zwei Slots: Rinde und Risse.

    Die Blender-Materialien sind ausdruecklich nur Vorlage. Geliefert wird
    keines von beiden — der Import zieht mit materialImportMode=None gar kein
    Material aus der Datei, und Unity legt die URP-Materialien selbst an. Die
    Zahlen hier stehen trotzdem, damit nachvollziehbar bleibt, woher die
    Unity-Werte stammen.
    """
    bark = bpy.data.materials.get(MATERIAL_BARK) or bpy.data.materials.new(MATERIAL_BARK)
    bark.use_nodes = True
    bsdf = bark.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        # Dunkles, feuchtes Holz mit graubrauner Rinde.
        bsdf.inputs["Base Color"].default_value = (0.086, 0.071, 0.055, 1.0)
        bsdf.inputs["Roughness"].default_value = 0.86
        bsdf.inputs["Metallic"].default_value = 0.0

    crack = bpy.data.materials.get(MATERIAL_CRACK) or bpy.data.materials.new(MATERIAL_CRACK)
    crack.use_nodes = True
    bsdf = crack.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        # Im Ruhezustand fast dunkel; das Leuchten kommt zur Laufzeit.
        bsdf.inputs["Base Color"].default_value = (0.031, 0.094, 0.090, 1.0)
        bsdf.inputs["Roughness"].default_value = 0.62
        if "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = (0.16, 0.72, 0.68, 1.0)
        if "Emission Strength" in bsdf.inputs:
            bsdf.inputs["Emission Strength"].default_value = 0.0

    obj.data.materials.append(bark)
    obj.data.materials.append(crack)


def final_space(point, shift):
    """Vom Aufbauraum in den Auslieferungsraum.

    Erst verschieben — Pivot am Boden und laengs mittig —, dann die 180 Grad um
    die Hochachse aus FORWARD_DELIVERED. Mesh und Rig laufen beide durch diese
    eine Funktion; zwei getrennte Rechnungen fuer dieselbe Lage waeren die
    sicherste Art, Knochen neben den Koerper zu legen.
    """
    moved = point + shift
    return Vector((-moved.x, -moved.y, moved.z))


def _center_and_ground(obj):
    """Pivot am Boden, mittig, bei (0,0,0), und Blickrichtung -Y.

    Nur die Laengsachse wird zentriert und die Unterkante auf Z=0 gelegt. Die
    Querachse bleibt auf der Modellierachse stehen: sie dort auf die
    Bounding-Box zu zentrieren wuerde genau die Asymmetrie wegrechnen, die das
    Konzept verlangt.
    """
    coords = [v.co for v in obj.data.vertices]
    min_y = min(c.y for c in coords)
    max_y = max(c.y for c in coords)
    min_z = min(c.z for c in coords)

    shift = Vector((0.0, -(min_y + max_y) * 0.5, -min_z))

    for v in obj.data.vertices:
        v.co = final_space(v.co, shift)

    # Die Drehung steckt jetzt in den Meshdaten. Das Objekt selbst bleibt
    # unveraendert bei Rotation (0,0,0) und Scale (1,1,1) — genau das verlangt
    # der Standard, und genau das prueft der Validator nach.
    obj.location = (0.0, 0.0, 0.0)
    obj.rotation_euler = (0.0, 0.0, 0.0)
    obj.scale = (1.0, 1.0, 1.0)

    return shift


def viewport_override():
    """Ein Kontext, in dem Objekt- und Mesh-Operatoren zulaessig sind.

    Ueber die MCP-Bruecke laeuft dieses Skript in einem Timer-Rueckruf. Dort
    zeigt ``bpy.context`` auf irgendeinen Bereich des Fensters — bei uns auf
    das Eigenschaftenfenster — und ``bpy.ops.object.mode_set`` faellt mit
    "Context missing active object" durch, obwohl ein aktives Objekt gesetzt
    ist. Der Operator sucht es im Bereich, nicht im View Layer.
    """
    window = bpy.context.window_manager.windows[0]
    for area in window.screen.areas:
        if area.type != 'VIEW_3D':
            continue
        for region in area.regions:
            if region.type == 'WINDOW':
                return bpy.context.temp_override(
                    window=window, area=area, region=region)

    raise RuntimeError(
        "Kein 3D-Viewport gefunden. Der Aufbau braucht eine Blender-Sitzung "
        "mit Oberflaeche; im Hintergrundmodus fehlt der Bereich.")


def select_only(obj):
    """Genau ein Objekt ausgewaehlt und aktiv."""
    for other in bpy.context.view_layer.objects:
        other.select_set(False)

    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def _unwrap(obj):
    """UV0 anlegen.

    Hier wird ausnahmsweise ein Operator benutzt: fuer das Auspacken gibt es in
    bmesh keine Entsprechung. Der Kontext wird vorher eindeutig gesetzt, damit
    der Operator nicht auf einem beliebigen aktiven Objekt landet.
    """
    select_only(obj)

    with viewport_override():
        bpy.ops.object.mode_set(mode='EDIT')
        bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.uv.smart_project(
            angle_limit=math.radians(66.0), island_margin=0.006)
        bpy.ops.object.mode_set(mode='OBJECT')


# ---------------------------------------------------------------------------
# Rig
# ---------------------------------------------------------------------------

#: Bildrate der Animationen.
#:
#: 50 statt der ueblichen 24 oder 30, weil der Konzeptentwurf drei Dauern auf
#: die Hundertstelsekunde festlegt: 0,7 s Telegraph, 0,18 s Flinch, 0,8 s
#: Straucheln. Bei 50 fps sind das glatt 35, 9 und 40 Bilder. Bei 30 fps waere
#: der Flinch 5,4 Bilder lang, also entweder 0,167 s oder 0,200 s — und damit
#: eine gemessene Vorgabe, die schon beim Anlegen der Datei verfehlt wird.
FPS = 50

#: Die Knochen des Rigs: (Name, Kopf, Schwanz, Elternteil, Rollziel).
#:
#: Das Rollziel legt fest, wohin die lokale Z-Achse des Knochens zeigt, und
#: damit, was eine Drehung um die lokale X-Achse bedeutet. Ohne diese Angabe
#: waehlt Blender die Rolle selbst, und "Bein nach vorn schwingen" hiesse je
#: nach Knochen einmal X und einmal Z. Bei den Beinen zeigt Z nach vorn, damit
#: eine X-Drehung sauber vor und zurueck schwingt und eine Z-Drehung zur Seite.
SPINE_BONES = [
    # (Name, y/z des Kopfes, y/z des Schwanzes, Elternteil)
    #
    # Das Becken reicht bis ans Rumpfende, nicht nur bis zur Huefte. Reichte es
    # nur bis -0,42, waeren die naechsten Knochen fuer die letzten 28 cm Rumpf
    # die Glieder des Wurzelfortsatzes — und dann verformte ein schwingender
    # Fortsatz die Kruppe mit.
    ("Becken",   (-0.160, 0.664), (-0.620, 0.628), "Wurzel"),
    ("Ruecken",  (-0.160, 0.664), ( 0.140, 0.700), "Wurzel"),
    ("Brust",    ( 0.140, 0.700), ( 0.340, 0.714), "Ruecken"),
    ("Hals",     ( 0.340, 0.714), ( 0.460, 0.664), "Brust"),
    ("Kopf",     ( 0.460, 0.664), ( 0.700, 0.612), "Hals"),
    ("Schnauze", ( 0.700, 0.612), ( 0.800, 0.582), "Kopf"),
]

#: Rollziele. Sie zeigen bewusst in Koerperrichtungen und nicht in feste
#: Weltachsen: dadurch bedeutet eine Drehung um die lokale X-Achse an jedem
#: Knochen dasselbe, egal wohin das Tier in der Datei schaut. Die Posen weiter
#: unten sind in diesen lokalen Achsen geschrieben und bleiben damit gueltig.
UP = Vector((0.0, 0.0, 1.0))
AHEAD = FORWARD_DELIVERED


def build_armature(shift):
    """Baut das Generic-Rig des Wurzelstreifers.

    Generic und nicht Humanoid: der Konzeptentwurf legt das fest, und ein
    Vierbeiner hat in Unitys Humanoid-Muskelschema ohnehin keine Entsprechung.
    Root Motion wird nicht verwendet — die Bewegung kommt aus
    ``EnemyMovement``. Deshalb verschiebt keine Animation den Wurzelknochen
    waagerecht: sie wuerde das Modell gegen seinen eigenen Transform
    verschieben und die Fuesse rutschen lassen.
    """
    armature = bpy.data.armatures.new(ARMATURE_OBJECT_NAME)
    rig = bpy.data.objects.new(ARMATURE_OBJECT_NAME, armature)
    bpy.context.collection.objects.link(rig)

    select_only(rig)
    with viewport_override():
        bpy.ops.object.mode_set(mode='EDIT')

    edit = armature.edit_bones

    def add(name, head, tail, parent, roll_to):
        bone = edit.new(name)
        bone.head = final_space(head, shift)
        bone.tail = final_space(tail, shift)
        bone.use_connect = False
        if parent:
            bone.parent = edit[parent]
        bone.align_roll(roll_to)
        return bone

    def add_final(name, head, tail, parent, roll_to):
        """Fuer Knochen, deren Lage schon im Auslieferungsraum angegeben ist."""
        bone = edit.new(name)
        bone.head = head
        bone.tail = tail
        bone.use_connect = False
        if parent:
            bone.parent = edit[parent]
        bone.align_roll(roll_to)
        return bone

    # Der Wurzelknochen liegt im Objektursprung, also am Boden und mittig, und
    # zeigt nach vorn — im Auslieferungsraum also nach -Y. Ein Rig, dessen
    # Wurzel woanders sitzt, kommt in Unity als Hierarchie mit Versatz an, und
    # jede Positionsangabe am Prefab meint dann etwas anderes als die Angabe am
    # Modell.
    add_final("Wurzel", Vector((0.0, 0.0, 0.0)),
              Vector((0.0, -0.22, 0.0)), None, UP)

    for name, (hy, hz), (ty, tz), parent in SPINE_BONES:
        add(name, Vector((0.0, hy, hz)), Vector((0.0, ty, tz)), parent, UP)

    for entry in LEG_PLAN:
        code = entry[0]
        hip, knee, paw, _ = leg_joints(entry)
        parent = "Brust" if code.startswith("V") else "Becken"

        add("Oberbein_" + code, hip, knee, parent, AHEAD)
        add("Unterbein_" + code, knee, paw, "Oberbein_" + code, AHEAD)
        add("Pfote_" + code, paw,
            paw + Vector((0.0, 0.034, -0.024)), "Unterbein_" + code, AHEAD)

    for strand in TAIL_STRANDS:
        code = strand[0]
        path, _ = tail_path(strand)
        # Drei Glieder fuer den langen, zwei fuer den kurzen Strang: der lange
        # schleift und braucht die Biegung, der kurze haengt nur.
        pieces = 3 if code == "A" else 2
        parent = "Becken"

        for i in range(pieces):
            head = path[int(round(i * len(path) / pieces))]
            tail = path[int(round((i + 1) * len(path) / pieces))
                        if i + 1 < pieces else len(path) - 1]
            name = "Fortsatz_%s_%02d" % (code, i + 1)
            add(name, head, tail, parent, UP)
            parent = name

    with viewport_override():
        bpy.ops.object.mode_set(mode='OBJECT')

    for bone in rig.pose.bones:
        bone.rotation_mode = 'QUATERNION'

    rig.location = (0.0, 0.0, 0.0)
    rig.rotation_euler = (0.0, 0.0, 0.0)
    rig.scale = (1.0, 1.0, 1.0)

    return rig


def bind_skin(mesh_obj, rig):
    """Haeutet das Mesh an das Rig.

    Erst mit automatischen Gewichten (Bone Heat). Das Verfahren setzt eine
    weitgehend geschlossene Flaeche voraus; unser Mesh besteht aus vielen
    getrennten Schalen — Rindenplatten, Rissbaender, Wurzelstraenge. Faellt es
    durch, wird auf Huellen-Gewichte zurueckgefallen. Stillschweigend ohne
    Gewichte weiterzumachen waere der schlechteste Ausgang: das Modell kaeme in
    Unity an und bewegte sich einfach nicht.
    """
    for other in bpy.context.view_layer.objects:
        other.select_set(False)

    mesh_obj.select_set(True)
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig

    method = 'ARMATURE_AUTO'
    try:
        with viewport_override():
            bpy.ops.object.parent_set(type='ARMATURE_AUTO')
    except RuntimeError:
        method = 'ARMATURE_ENVELOPE'
        with viewport_override():
            bpy.ops.object.parent_set(type='ARMATURE_ENVELOPE')

    snapped = _snap_detail_weights(mesh_obj)

    # Die Hilfsgruppe hat ihren Zweck erfuellt. Sie muss vor der
    # Restausbesserung weg, sonst zaehlt sie dort als Gewicht und deckt genau
    # die Vertices zu, die noch keines haben — und sie hat in der
    # ausgelieferten Datei ohnehin nichts verloren.
    detail_group = mesh_obj.vertex_groups.get(DETAIL_GROUP)
    if detail_group is not None:
        mesh_obj.vertex_groups.remove(detail_group)

    repaired = _repair_unweighted(mesh_obj, rig)

    remaining = [v.index for v in mesh_obj.data.vertices
                 if not any(g.weight > 0.0 for g in v.groups)]

    return method, snapped, repaired, len(remaining)


def _snap_detail_weights(mesh_obj):
    """Gibt den aufliegenden Schalen die Gewichte der Haut unter ihnen.

    Rindenplatten, Augenkuppel und Rissbaender liegen ueber der Rumpfflaeche,
    ohne mit ihr verbunden zu sein. Bone Heat gewichtet sie deshalb
    eigenstaendig — und weil ihre Vertices ein paar Millimeter weiter aussen
    liegen als die Haut, faellt die Gewichtung anders aus. Beim ersten
    Kruemmen des Ruecken wandern Platte und Haut auseinander, und zwischen
    beiden klafft ein Loch.

    Jede Detailschale uebernimmt deshalb die Gewichte ihres naechsten
    Hautvertex. Danach bewegt sie sich exakt wie der Untergrund, auf dem sie
    sitzt.
    """
    from mathutils.kdtree import KDTree

    detail_group = mesh_obj.vertex_groups.get(DETAIL_GROUP)
    if detail_group is None:
        return 0

    vertices = mesh_obj.data.vertices
    detail = []
    skin = []

    for vertex in vertices:
        marked = any(g.group == detail_group.index and g.weight > 0.0
                     for g in vertex.groups)
        (detail if marked else skin).append(vertex)

    if not detail or not skin:
        return 0

    tree = KDTree(len(skin))
    for i, vertex in enumerate(skin):
        tree.insert(vertex.co, i)
    tree.balance()

    groups = mesh_obj.vertex_groups

    for vertex in detail:
        _, index, _ = tree.find(vertex.co)
        source = skin[index]

        for entry in list(vertex.groups):
            groups[entry.group].remove([vertex.index])

        for entry in source.groups:
            if entry.group == detail_group.index or entry.weight <= 0.0:
                continue
            groups[entry.group].add([vertex.index], entry.weight, 'REPLACE')

        detail_group.add([vertex.index], 1.0, 'REPLACE')

    return len(detail)


def _repair_unweighted(mesh_obj, rig):
    """Haengt uebrig gebliebene Vertices an den naechstgelegenen Knochen.

    Bone Heat laesst regelmaessig einzelne Vertices ohne Gewicht zurueck —
    hier die Spitzen der Rindenplatten und der Augenkuppel, die als eigene
    Schalen ueber der Oberflaeche liegen und vom Waermefluss nicht erreicht
    werden. Ein Vertex ohne Gewicht wird beim Skinning nicht mitbewegt,
    sondern bleibt im Ursprung des Objekts liegen: aus dem Gegner ragt dann
    eine Spitze zum Boden, sobald er sich bewegt. Das faellt im Ruhezustand
    nicht auf und im Kampf sofort.
    """
    unweighted = [v for v in mesh_obj.data.vertices
                  if not any(g.weight > 0.0 for g in v.groups)]

    if not unweighted:
        return 0

    segments = []
    for bone in rig.data.bones:
        group = mesh_obj.vertex_groups.get(bone.name)
        if group is not None:
            segments.append((group, bone.head_local, bone.tail_local))

    for vertex in unweighted:
        best_group = None
        best_distance = float("inf")

        for group, head, tail in segments:
            axis = tail - head
            length_squared = axis.length_squared
            if length_squared < 1e-12:
                closest = head
            else:
                f = (vertex.co - head).dot(axis) / length_squared
                closest = head + axis * min(max(f, 0.0), 1.0)

            distance = (vertex.co - closest).length
            if distance < best_distance:
                best_distance = distance
                best_group = group

        if best_group is not None:
            best_group.add([vertex.index], 1.0, 'REPLACE')

    return len(unweighted)


# ---------------------------------------------------------------------------
# Animationen
# ---------------------------------------------------------------------------

#: Die Clips des finalen Assets.
#:
#: Die zehn Zustaende von ``EnemyAnimationDriver`` plus "Trab". Namen und
#: Dauern stammen aus dem Konzeptentwurf, Abschnitt 5.
#:
#: Zum seitlichen Schritt: der Entwurf verlangt fuer ihn einen eigenen Zyklus
#: statt des geliehenen Laufs. Im Controller ist "Schritt" der Zustand, den
#: ``SelectState`` **ausschliesslich** beim Umkreisen zurueckgibt — der
#: Vorwaertslauf laeuft ueber "Lauf". "Schritt" bekommt deshalb den
#: Seitwaertszyklus, und das ist kein Umbau, sondern die Aufloesung dessen, was
#: der Zustand ohnehin tut. "Trab" wird mitgeliefert, aber von keinem Zustand
#: gewaehlt: einen Vorwaertsschritt kennt die Zustandsmaschine heute nicht.
#:
#: (Name, Dauer in Sekunden, Schleife, Anmerkung)
CLIPS = [
    ("Idle",       2.40, True,  "ruhiges Atmen, kleine Kopfbewegung"),
    ("Lauschen",   2.00, True,  "Kopf hoch, wachsam suchend"),
    ("Schritt",    1.00, True,  "seitlicher Schritt beim Umkreisen"),
    ("Trab",       1.00, True,  "Vorwaertsgang; heute von keinem Zustand gewaehlt"),
    ("Lauf",       0.60, True,  "Verfolgung"),
    ("Telegraph",  0.70, False, "Absenken der Schulter, 0,7 s aus dem Entwurf"),
    ("Sprungbiss", 0.50, False, "kurzer Biss mit sichtbarem Absprung"),
    ("Flinch",     0.18, False, "Zucken, 0,18 s aus dem Entwurf"),
    ("Stagger",    0.80, False, "schweres Straucheln, 0,8 s aus dem Entwurf"),
    ("Flucht",     0.70, True,  "Rueckzug mit gesenktem Kopf"),
    ("Niederlage", 1.40, False, "Haltung sinkt, kein Sterbekrampf"),
]

#: Posen je Clip: {Clip: [(Anteil der Dauer, {Knochen: (rx, ry, rz) in Grad})]}
#:
#: Gedreht wird fast nur um die lokale X-Achse. Durch die gesetzte Rolle heisst
#: das bei den Wirbelknochen "nicken" und bei den Beinen "vor und zurueck
#: schwingen"; eine Z-Drehung heisst am Kopf "wenden" und am Bein "abspreizen".
#:
#: Die Beine stehen paarweise diagonal: vorn rechts geht mit hinten links.
POSES = {
    "Idle": [
        (0.00, {}),
        (0.25, {"Ruecken": (1.2, 0, 0), "Brust": (1.5, 0, 0),
                "Hals": (-1.0, 0, 0), "Kopf": (1.5, 0, 2.0),
                "Fortsatz_A_01": (2.0, 0, 3.0), "Fortsatz_B_01": (1.5, 0, -2.0)}),
        (0.50, {"Ruecken": (0, 0, 0), "Kopf": (-0.8, 0, 0),
                "Fortsatz_A_02": (2.5, 0, 0)}),
        (0.75, {"Ruecken": (-0.8, 0, 0), "Brust": (-1.0, 0, 0),
                "Hals": (-1.2, 0, 0), "Kopf": (1.0, 0, -2.5),
                "Fortsatz_A_01": (-1.5, 0, -3.0), "Fortsatz_B_01": (-1.0, 0, 2.0)}),
        (1.00, {}),
    ],
    "Lauschen": [
        (0.00, {"Hals": (6.0, 0, 0), "Kopf": (-4.0, 0, 0)}),
        (0.30, {"Hals": (7.5, 0, 0), "Kopf": (-5.0, 0, 9.0),
                "Schnauze": (-2.0, 0, 4.0), "Fortsatz_A_01": (-2.0, 0, 4.0)}),
        (0.60, {"Hals": (6.5, 0, 0), "Kopf": (-3.0, 0, -8.0),
                "Schnauze": (-1.0, 0, -3.0), "Fortsatz_A_01": (-2.5, 0, -3.0)}),
        (0.85, {"Hals": (8.0, 0, 0), "Kopf": (-6.0, 4.0, 3.0)}),
        (1.00, {"Hals": (6.0, 0, 0), "Kopf": (-4.0, 0, 0)}),
    ],
    # Seitlicher Schritt: die Beine spreizen ab und setzen versetzt wieder auf,
    # der Rumpf rollt leicht mit. Vorwaerts passiert nichts — der Gegner
    # umkreist sein Ziel, er laeuft nicht darauf zu.
    "Schritt": [
        (0.00, {"Ruecken": (0, 2.5, 0), "Becken": (0, 2.0, 0)}),
        (0.25, {"Ruecken": (0, -1.0, 0), "Becken": (0, -1.0, 0),
                "Oberbein_VR": (-14, 0, 12), "Unterbein_VR": (16, 0, 0),
                "Oberbein_HL": (12, 0, -10), "Unterbein_HL": (-12, 0, 0),
                "Kopf": (0, 0, -5.0)}),
        (0.50, {"Ruecken": (0, -2.5, 0), "Becken": (0, -2.0, 0)}),
        (0.75, {"Ruecken": (0, 1.0, 0), "Becken": (0, 1.0, 0),
                "Oberbein_VL": (-14, 0, -12), "Unterbein_VL": (16, 0, 0),
                "Oberbein_HR": (12, 0, 10), "Unterbein_HR": (-12, 0, 0),
                "Kopf": (0, 0, 5.0)}),
        (1.00, {"Ruecken": (0, 2.5, 0), "Becken": (0, 2.0, 0)}),
    ],
    "Trab": [
        (0.00, {"Oberbein_VR": (-18, 0, 0), "Unterbein_VR": (10, 0, 0),
                "Oberbein_HL": (-16, 0, 0), "Unterbein_HL": (14, 0, 0),
                "Oberbein_VL": (18, 0, 0), "Unterbein_VL": (-8, 0, 0),
                "Oberbein_HR": (16, 0, 0), "Unterbein_HR": (-6, 0, 0)}),
        (0.25, {"Ruecken": (1.5, 0, 0),
                "Oberbein_VR": (0, 0, 0), "Unterbein_VR": (24, 0, 0),
                "Oberbein_HL": (0, 0, 0), "Unterbein_HL": (0, 0, 0),
                "Oberbein_VL": (0, 0, 0), "Unterbein_VL": (0, 0, 0),
                "Oberbein_HR": (0, 0, 0), "Unterbein_HR": (18, 0, 0)}),
        (0.50, {"Oberbein_VR": (18, 0, 0), "Unterbein_VR": (-8, 0, 0),
                "Oberbein_HL": (16, 0, 0), "Unterbein_HL": (-6, 0, 0),
                "Oberbein_VL": (-18, 0, 0), "Unterbein_VL": (10, 0, 0),
                "Oberbein_HR": (-16, 0, 0), "Unterbein_HR": (14, 0, 0)}),
        (0.75, {"Ruecken": (1.5, 0, 0),
                "Oberbein_VR": (0, 0, 0), "Unterbein_VR": (0, 0, 0),
                "Oberbein_HL": (0, 0, 0), "Unterbein_HL": (18, 0, 0),
                "Oberbein_VL": (0, 0, 0), "Unterbein_VL": (24, 0, 0),
                "Oberbein_HR": (0, 0, 0), "Unterbein_HR": (0, 0, 0)}),
        (1.00, {"Oberbein_VR": (-18, 0, 0), "Unterbein_VR": (10, 0, 0),
                "Oberbein_HL": (-16, 0, 0), "Unterbein_HL": (14, 0, 0),
                "Oberbein_VL": (18, 0, 0), "Unterbein_VL": (-8, 0, 0),
                "Oberbein_HR": (16, 0, 0), "Unterbein_HR": (-6, 0, 0)}),
    ],
    # Galopp: sammeln und strecken. Vorder- und Hinterbeine arbeiten paarweise,
    # nicht diagonal — das ist der Unterschied, an dem ein Lauf als Lauf liest.
    "Lauf": [
        (0.00, {"Ruecken": (-6, 0, 0), "Becken": (5, 0, 0), "Hals": (-3, 0, 0),
                "Oberbein_VR": (-32, 0, 0), "Unterbein_VR": (20, 0, 0),
                "Oberbein_VL": (-28, 0, 0), "Unterbein_VL": (24, 0, 0),
                "Oberbein_HR": (34, 0, 0), "Unterbein_HR": (-26, 0, 0),
                "Oberbein_HL": (30, 0, 0), "Unterbein_HL": (-22, 0, 0),
                "Fortsatz_A_01": (-8, 0, 0)}),
        (0.30, {"Ruecken": (4, 0, 0), "Becken": (-4, 0, 0), "Hals": (2, 0, 0),
                "Oberbein_VR": (14, 0, 0), "Unterbein_VR": (-6, 0, 0),
                "Oberbein_VL": (18, 0, 0), "Unterbein_VL": (-4, 0, 0),
                "Oberbein_HR": (-16, 0, 0), "Unterbein_HR": (30, 0, 0),
                "Oberbein_HL": (-20, 0, 0), "Unterbein_HL": (34, 0, 0),
                "Fortsatz_A_01": (6, 0, 0)}),
        (0.62, {"Ruecken": (-2, 0, 0), "Becken": (2, 0, 0),
                "Oberbein_VR": (26, 0, 0), "Unterbein_VR": (-12, 0, 0),
                "Oberbein_VL": (30, 0, 0), "Unterbein_VL": (-10, 0, 0),
                "Oberbein_HR": (-30, 0, 0), "Unterbein_HR": (18, 0, 0),
                "Oberbein_HL": (-34, 0, 0), "Unterbein_HL": (22, 0, 0)}),
        (1.00, {"Ruecken": (-6, 0, 0), "Becken": (5, 0, 0), "Hals": (-3, 0, 0),
                "Oberbein_VR": (-32, 0, 0), "Unterbein_VR": (20, 0, 0),
                "Oberbein_VL": (-28, 0, 0), "Unterbein_VL": (24, 0, 0),
                "Oberbein_HR": (34, 0, 0), "Unterbein_HR": (-26, 0, 0),
                "Oberbein_HL": (30, 0, 0), "Unterbein_HL": (-22, 0, 0),
                "Fortsatz_A_01": (-8, 0, 0)}),
    ],
    # Telegraph: die Schulter senkt sich sichtbar und laedt sich. Das Gewicht
    # geht nach hinten, der Kopf zieht zurueck. Der Entwurf verlangt, dass die
    # 0,7 s auch ohne die tuerkise Verstaerkung lesbar bleiben — deshalb ist
    # die Bewegung gross und laeuft in eine Richtung, ohne Zwischenschwingen.
    "Telegraph": [
        (0.00, {}),
        (0.35, {"Brust": (-6, 0, 0), "Ruecken": (-3, 0, 0), "Hals": (-4, 0, 0),
                "Kopf": (3, 0, 0),
                "Oberbein_VR": (10, 0, 0), "Unterbein_VR": (-16, 0, 0),
                "Oberbein_VL": (9, 0, 0), "Unterbein_VL": (-15, 0, 0)}),
        (0.75, {"Brust": (-13, 0, 0), "Ruecken": (-6, 0, 0), "Hals": (-9, 0, 0),
                "Kopf": (7, 0, 0), "Schnauze": (3, 0, 0), "Becken": (-4, 0, 0),
                "Oberbein_VR": (20, 0, 0), "Unterbein_VR": (-30, 0, 0),
                "Oberbein_VL": (18, 0, 0), "Unterbein_VL": (-28, 0, 0),
                "Oberbein_HR": (-14, 0, 0), "Unterbein_HR": (22, 0, 0),
                "Oberbein_HL": (-13, 0, 0), "Unterbein_HL": (21, 0, 0),
                "Fortsatz_A_01": (-12, 0, 0)}),
        (1.00, {"Brust": (-15, 0, 0), "Ruecken": (-7, 0, 0), "Hals": (-10, 0, 0),
                "Kopf": (8, 0, 0), "Schnauze": (4, 0, 0), "Becken": (-5, 0, 0),
                "Oberbein_VR": (22, 0, 0), "Unterbein_VR": (-33, 0, 0),
                "Oberbein_VL": (20, 0, 0), "Unterbein_VL": (-31, 0, 0),
                "Oberbein_HR": (-16, 0, 0), "Unterbein_HR": (24, 0, 0),
                "Oberbein_HL": (-15, 0, 0), "Unterbein_HL": (23, 0, 0),
                "Fortsatz_A_01": (-14, 0, 0)}),
    ],
    # Der Biss beginnt aus der geladenen Haltung des Telegraphs und faehrt nach
    # vorn heraus. Der Absprung ist sichtbar: der Rumpf streckt sich, die
    # Hinterbeine schieben, der Kopf schiesst vor.
    "Sprungbiss": [
        (0.00, {"Brust": (-15, 0, 0), "Hals": (-10, 0, 0), "Kopf": (8, 0, 0),
                "Oberbein_VR": (22, 0, 0), "Unterbein_VR": (-33, 0, 0),
                "Oberbein_VL": (20, 0, 0), "Unterbein_VL": (-31, 0, 0)}),
        (0.24, {"Brust": (9, 0, 0), "Ruecken": (6, 0, 0), "Hals": (10, 0, 0),
                "Kopf": (-14, 0, 0), "Schnauze": (-8, 0, 0),
                "Oberbein_VR": (-34, 0, 0), "Unterbein_VR": (14, 0, 0),
                "Oberbein_VL": (-32, 0, 0), "Unterbein_VL": (12, 0, 0),
                "Oberbein_HR": (26, 0, 0), "Unterbein_HR": (-30, 0, 0),
                "Oberbein_HL": (24, 0, 0), "Unterbein_HL": (-28, 0, 0),
                "Fortsatz_A_01": (14, 0, 0)}),
        (0.44, {"Brust": (4, 0, 0), "Hals": (4, 0, 0), "Kopf": (-6, 0, 0),
                "Schnauze": (-3, 0, 0),
                "Oberbein_VR": (-10, 0, 0), "Unterbein_VR": (20, 0, 0),
                "Oberbein_VL": (-8, 0, 0), "Unterbein_VL": (18, 0, 0),
                "Oberbein_HR": (8, 0, 0), "Oberbein_HL": (7, 0, 0)}),
        (1.00, {}),
    ],
    # 0,18 s Zucken. Kurz und hart: ein Flinch, der ausschwingt, liest sich als
    # Treffer, den der Gegner nicht ernst nimmt.
    "Flinch": [
        (0.00, {}),
        (0.33, {"Hals": (-7, 0, -6), "Kopf": (5, -5, -8), "Brust": (-3, 2, 0),
                "Ruecken": (-2, 2, 0)}),
        (0.66, {"Hals": (-3, 0, -2), "Kopf": (2, -2, -3), "Brust": (-1, 1, 0)}),
        (1.00, {}),
    ],
    # 0,8 s schweres Straucheln — die sichtbare Belohnung fuer einen schweren
    # Angriff. Ein Vorderbein knickt weg, der Rumpf kippt, der Kopf schwingt
    # nach, und erst danach faengt sich das Tier.
    "Stagger": [
        (0.00, {}),
        (0.18, {"Brust": (-9, 7, 0), "Ruecken": (-6, 6, 0), "Becken": (-3, 4, 0),
                "Hals": (-11, 0, -12), "Kopf": (9, -7, -14),
                "Oberbein_VR": (16, 0, 14), "Unterbein_VR": (-24, 0, 0),
                "Oberbein_VL": (-9, 0, 0),
                "Fortsatz_A_01": (-10, 0, 12)}),
        (0.45, {"Brust": (-13, -5, 0), "Ruecken": (-9, -4, 0), "Becken": (-5, -3, 0),
                "Hals": (-8, 0, 9), "Kopf": (11, 5, 11),
                "Oberbein_VR": (24, 0, 8), "Unterbein_VR": (-32, 0, 0),
                "Oberbein_VL": (-14, 0, -6), "Unterbein_VL": (10, 0, 0),
                "Oberbein_HR": (-10, 0, 0), "Oberbein_HL": (-8, 0, 0),
                "Fortsatz_A_01": (-14, 0, -10)}),
        (0.72, {"Brust": (-6, 3, 0), "Ruecken": (-4, 2, 0),
                "Hals": (-4, 0, -4), "Kopf": (5, -2, -5),
                "Oberbein_VR": (10, 0, 3), "Unterbein_VR": (-14, 0, 0),
                "Oberbein_VL": (-6, 0, 0)}),
        (1.00, {}),
    ],
    "Flucht": [
        (0.00, {"Hals": (-8, 0, 0), "Kopf": (5, 0, 0), "Ruecken": (-4, 0, 0),
                "Oberbein_VR": (-30, 0, 0), "Unterbein_VR": (18, 0, 0),
                "Oberbein_VL": (26, 0, 0), "Unterbein_VL": (-10, 0, 0),
                "Oberbein_HR": (28, 0, 0), "Unterbein_HR": (-20, 0, 0),
                "Oberbein_HL": (-26, 0, 0), "Unterbein_HL": (26, 0, 0)}),
        (0.50, {"Hals": (-9, 0, 0), "Kopf": (6, 0, 0), "Ruecken": (-2, 0, 0),
                "Oberbein_VR": (26, 0, 0), "Unterbein_VR": (-10, 0, 0),
                "Oberbein_VL": (-30, 0, 0), "Unterbein_VL": (18, 0, 0),
                "Oberbein_HR": (-26, 0, 0), "Unterbein_HR": (26, 0, 0),
                "Oberbein_HL": (28, 0, 0), "Unterbein_HL": (-20, 0, 0)}),
        (1.00, {"Hals": (-8, 0, 0), "Kopf": (5, 0, 0), "Ruecken": (-4, 0, 0),
                "Oberbein_VR": (-30, 0, 0), "Unterbein_VR": (18, 0, 0),
                "Oberbein_VL": (26, 0, 0), "Unterbein_VL": (-10, 0, 0),
                "Oberbein_HR": (28, 0, 0), "Unterbein_HR": (-20, 0, 0),
                "Oberbein_HL": (-26, 0, 0), "Unterbein_HL": (26, 0, 0)}),
    ],
    # Niederlage und Beruhigung. Die Haltung sinkt, die Beine knicken ein, der
    # Kopf legt sich zuletzt ab. Kein Sterbekrampf, kein Zucken am Ende — der
    # Entwurf verlangt ausdruecklich eine Beruhigung und kein Sterben.
    "Niederlage": [
        (0.00, {}),
        (0.20, {"Brust": (-7, 0, 0), "Hals": (-6, 0, 0), "Kopf": (4, 0, 0),
                "Oberbein_VR": (9, 0, 0), "Unterbein_VR": (-13, 0, 0),
                "Oberbein_VL": (8, 0, 0), "Unterbein_VL": (-12, 0, 0)}),
        (0.52, {"Brust": (-16, 4, 0), "Ruecken": (-12, 3, 0), "Becken": (-9, 2, 0),
                "Hals": (-14, 0, 5), "Kopf": (10, 0, 6),
                "Oberbein_VR": (30, 0, 10), "Unterbein_VR": (-46, 0, 0),
                "Oberbein_VL": (28, 0, -8), "Unterbein_VL": (-44, 0, 0),
                "Oberbein_HR": (-26, 0, 9), "Unterbein_HR": (40, 0, 0),
                "Oberbein_HL": (-24, 0, -7), "Unterbein_HL": (38, 0, 0),
                "Fortsatz_A_01": (-10, 0, 6)}),
        (0.80, {"Brust": (-21, 6, 0), "Ruecken": (-16, 5, 0), "Becken": (-12, 3, 0),
                "Hals": (-19, 0, 8), "Kopf": (16, 0, 9), "Schnauze": (6, 0, 0),
                "Oberbein_VR": (38, 0, 14), "Unterbein_VR": (-58, 0, 0),
                "Oberbein_VL": (36, 0, -11), "Unterbein_VL": (-56, 0, 0),
                "Oberbein_HR": (-33, 0, 12), "Unterbein_HR": (50, 0, 0),
                "Oberbein_HL": (-31, 0, -9), "Unterbein_HL": (48, 0, 0),
                "Fortsatz_A_01": (-14, 0, 9), "Fortsatz_B_01": (-11, 0, -7)}),
        (1.00, {"Brust": (-22, 6, 0), "Ruecken": (-17, 5, 0), "Becken": (-13, 3, 0),
                "Hals": (-20, 0, 8), "Kopf": (17, 0, 9), "Schnauze": (7, 0, 0),
                "Oberbein_VR": (39, 0, 14), "Unterbein_VR": (-59, 0, 0),
                "Oberbein_VL": (37, 0, -11), "Unterbein_VL": (-57, 0, 0),
                "Oberbein_HR": (-34, 0, 12), "Unterbein_HR": (51, 0, 0),
                "Oberbein_HL": (-32, 0, -9), "Unterbein_HL": (49, 0, 0),
                "Fortsatz_A_01": (-15, 0, 9), "Fortsatz_B_01": (-12, 0, -7)}),
    ],
}


def build_animations(rig):
    """Legt fuer jeden Clip eine Aktion an.

    Jede Aktion keyt an jedem ihrer Zeitpunkte **alle** Knochen, die in diesem
    Clip ueberhaupt vorkommen. Sonst haelt ein Knochen, der nur an einem
    Zeitpunkt gesetzt ist, seinen Wert ueber den ganzen Clip, und die Pose
    springt statt sich zu bewegen.
    """
    built = []

    rig.animation_data_create()

    for name, seconds, loop, note in CLIPS:
        poses = POSES[name]
        used = sorted({bone for _, pose in poses for bone in pose})

        action = bpy.data.actions.new(name)
        action.use_fake_user = True
        rig.animation_data.action = action

        last_frame = 1 + int(round(seconds * FPS))

        for fraction, pose in poses:
            frame = 1 + (last_frame - 1) * fraction

            for bone_name in used:
                bone = rig.pose.bones.get(bone_name)
                if bone is None:
                    raise KeyError(
                        "Clip '%s' setzt den Knochen '%s', den das Rig nicht "
                        "hat." % (name, bone_name))

                rx, ry, rz = pose.get(bone_name, (0.0, 0.0, 0.0))
                bone.rotation_quaternion = Euler(
                    (math.radians(rx), math.radians(ry), math.radians(rz)),
                    'XYZ').to_quaternion()
                bone.keyframe_insert(
                    data_path="rotation_quaternion", frame=frame)

        # Zurueck in die Ruhelage, damit die naechste Aktion nicht auf der
        # letzten Pose der vorherigen aufbaut.
        for bone in rig.pose.bones:
            bone.rotation_quaternion = Quaternion((1.0, 0.0, 0.0, 0.0))

        built.append({
            "name": name, "seconds": seconds, "last_frame": last_frame,
            "loop": loop, "bones": len(used), "note": note,
        })

    rig.animation_data.action = None
    return built


# ---------------------------------------------------------------------------
# Export
# ---------------------------------------------------------------------------

def export_fbx(filepath, bake_space_transform):
    """Exportiert nach dem Standard aus BLENDER_ASSET_PIPELINE.md.

    Zwei Abweichungen vom dort festgehaltenen Standard, beide zwingend, weil
    jener Standard fuer statische Meshes gemessen wurde:

    * ``object_types`` enthaelt ``ARMATURE``. Ohne das kaeme ein Skinned Mesh
      ohne Skelett an — also gar keines.
    * ``bake_anim=True`` mit ``bake_anim_use_all_actions``. Jede Aktion wird
      ein eigener Take und damit in Unity ein eigener Clip.

    ``bake_space_transform`` ist der offene Punkt des Pipelinedokuments
    ("NOCH ZU ENTSCHEIDEN: wie gerigte Assets mit den Achsen umgehen"). Blender
    kennzeichnet die Option als experimentell und nennt gerade Rigs als
    Problemfall, weil sie Objekt- und Armature-Raum auseinanderzieht. Deshalb
    ist sie hier ein Parameter und keine Konstante: beide Faelle werden
    exportiert und in Unity gemessen, statt geraten.
    """
    os.makedirs(os.path.dirname(filepath), exist_ok=True)

    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=False,
        object_types={'MESH', 'ARMATURE'},
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        global_scale=1.0,
        bake_space_transform=bake_space_transform,
        axis_forward='-Z',
        axis_up='Y',
        use_mesh_modifiers=True,
        mesh_smooth_type='FACE',
        use_tspace=False,
        use_custom_props=False,
        add_leaf_bones=False,
        primary_bone_axis='Y',
        secondary_bone_axis='X',
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=True,
        bake_anim_force_startend_keying=True,
        bake_anim_step=1.0,
        # Die Posen sind ohnehin wenige Stuetzstellen mit weicher
        # Interpolation. Jedes Bild als Schluesselbild zu schreiben blaeht die
        # Datei auf ein Vielfaches, ohne dass sich eine Bewegung aendert.
        # Anfang und Ende bleiben durch force_startend_keying erhalten, damit
        # die gemessenen Dauern 0,7 s, 0,18 s und 0,8 s exakt bleiben.
        bake_anim_simplify_factor=1.0,
        path_mode='AUTO',
        embed_textures=False,
    )

    return os.path.getsize(filepath)


# ---------------------------------------------------------------------------
# Gesamtlauf
# ---------------------------------------------------------------------------

def build_all(blend_path=None, fbx_paths=None):
    """Baut Mesh, Rig und Animationen und exportiert.

    ``fbx_paths`` ist ein Wortverzeichnis {Name: (Pfad, bake_space_transform)}.
    """
    bpy.context.scene.render.fps = FPS

    mesh_obj = build_mesh()
    shift = mesh_obj["ely_shift"]

    rig = build_armature(Vector(shift))
    skin_method, snapped, repaired, unweighted = bind_skin(mesh_obj, rig)
    clips = build_animations(rig)

    report = {
        "vertices": len(mesh_obj.data.vertices),
        "polygons": len(mesh_obj.data.polygons),
        "triangles": sum(len(p.vertices) - 2 for p in mesh_obj.data.polygons),
        "material_slots": len(mesh_obj.data.materials),
        "bones": len(rig.data.bones),
        "bone_names": [b.name for b in rig.data.bones],
        "skin_method": skin_method,
        "detail_vertices_snapped": snapped,
        "weights_repaired": repaired,
        "unweighted_vertices": unweighted,
        "clips": clips,
        "fps": FPS,
        "exports": {},
    }

    if blend_path:
        os.makedirs(os.path.dirname(blend_path), exist_ok=True)
        bpy.ops.wm.save_as_mainfile(filepath=blend_path)
        report["blend"] = blend_path

    for name, (path, bake) in (fbx_paths or {}).items():
        report["exports"][name] = {
            "path": path,
            "bake_space_transform": bake,
            "bytes": export_fbx(path, bake),
        }

    return report
