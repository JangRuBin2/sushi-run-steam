"""Reusable SushiRun measurement helpers (probe-side). Unity frame: metres, +z along the race course."""
from autopilot.kit import *
import math

# Race (RotatingBeltLevel) sections by z, upper bound exclusive
RACE_SECTIONS = [(16, "start"), (28, "plate1"), (41, "plate2_chopsticks"), (48.6, "cp1_pad"),
                 (64.6, "belt_2chopsticks"), (70.6, "cp2_pad"), (96, "small_plates"), (103.1, "cp3_pad"),
                 (114, "last_chopsticks"), (999, "finish_pad")]
RACE_CP_Z = [42.6, 64.6, 97.1]   # checkpoint pad entry z (cp1..cp3)
RACE_FINISH_Z = 121.0


def race_section(z):
    for lim, name in RACE_SECTIONS:
        if z < lim:
            return name
    return "past"


def race_census(game, seconds=100, build=True, step=0.1):
    """Watch every WanderBot run the race for `seconds` of game time (no player input).
    Per bot: skill, furthest z/section, checkpoint pads reached, finishes (+times), falls by section and cause
    (knocked = went Down within 2 s before the fall, else walked/jumped off). Returns a dict."""
    if build:
        game.press("Digit1")
        run(wait(game, 0.3))
    bots = list(game.actors("WanderBot"))
    skill = {b: game.get(b + "|WanderBot", "Skill") for b in bots}
    st = {b: {"skill": round(skill[b], 2), "maxz": 0.0, "cps": 0, "finishes": 0, "finish_t": [], "falls": [],
              "_desp": False, "_down_t": None, "_down_z": None, "_lastz": 0.0, "_start": None, "_wasfinish": False}
          for b in bots}
    clock = Clock(game)
    while clock.elapsed() < seconds:
        t = clock.elapsed()
        r = game.batch([lambda b=b: (game.get(b, "position"), game.get(b + "|SushiCharacter", "Mode"),
                                     game.get(b + "|SushiCharacter", "IsDespawned")) for b in bots])
        for b, v in zip(bots, r):
            if not isinstance(v, tuple) or not isinstance(v[0], dict):
                continue
            pos, mode, desp = v[0], v[1], v[2] is True
            s = st[b]
            if s["_start"] is None:
                s["_start"] = t
            if mode == "Down" and not desp:
                if s["_down_t"] is None or t - s["_down_t"] > 2.5:
                    s["_down_t"], s["_down_z"] = t, pos["z"]
            if desp and not s["_desp"]:
                knocked = s["_down_t"] is not None and t - s["_down_t"] < 2.5
                if s["_lastz"] < RACE_FINISH_Z:
                    s["falls"].append({"section": race_section(s["_down_z"] if knocked else s["_lastz"]),
                                       "cause": "knocked" if knocked else "off_edge", "t": round(t, 1)})
            if not desp:
                z = pos["z"]
                s["maxz"] = max(s["maxz"], z)
                s["cps"] = max(s["cps"], sum(1 for c in RACE_CP_Z if s["maxz"] >= c))
                if z >= RACE_FINISH_Z and s["_lastz"] < RACE_FINISH_Z and not s["_wasfinish"]:
                    s["finishes"] += 1
                    s["finish_t"].append(round(t - s["_start"], 1))
                    s["_wasfinish"] = True
                if z < 20:
                    if s["_wasfinish"]:
                        s["_start"] = t   # back at the start after finishing: next lap
                    s["_wasfinish"] = False
                s["_lastz"] = z
            s["_desp"] = desp
        run(wait(game, step))
    out = {}
    for b, s in st.items():
        name = b.rsplit("/", 1)[-1]
        by = {}
        for f in s["falls"]:
            k = f["section"] + ":" + f["cause"]
            by[k] = by.get(k, 0) + 1
        out[name] = {"skill": s["skill"], "furthest": race_section(s["maxz"]), "maxz": round(s["maxz"], 1),
                     "checkpoints": s["cps"], "finishes": s["finishes"], "finish_times": s["finish_t"],
                     "falls": len(s["falls"]), "falls_by": by}
    return out


def print_census(out, seconds):
    tot_f = sum(v["finishes"] for v in out.values())
    tot_falls = sum(v["falls"] for v in out.values())
    print(f"census {seconds}s: finishes {tot_f}, falls {tot_falls}")
    for k, v in sorted(out.items(), key=lambda kv: -kv[1]["skill"]):
        print(k, v)
