from __future__ import annotations
from dataclasses import dataclass
from typing import List, Dict, Optional
import random

from src.validator import Curriculum, assert_color, assert_shape


@dataclass(frozen=True)
class ObjSpec:
    shape_id: str
    color_id: str
    pos_slot: str  # left | right | bottom


@dataclass(frozen=True)
class HintPolicy:
    idle_ms: int
    max_voice_repeats: int


@dataclass(frozen=True)
class FeedbackStyle:
    tone: str   # warm | cheerful
    speed: str  # slow | normal


@dataclass(frozen=True)
class RoundPlan:
    target_color: str
    objects: List[ObjSpec]
    hint_policy: HintPolicy
    feedback_style: FeedbackStyle


class TutorAgent:
    """
    A small, deterministic-ish tutor agent (v1).
    It adapts within a short window memory (no “hallucinations”).
    """
    def __init__(self, curriculum: Curriculum):
        self.curr = curriculum
        self.window: List[dict] = []  # last N events
        self.max_window = 20
        self.last_target: Optional[str] = None
        self.repeat_budget: Dict[str, int] = {cid: 0 for cid in self.curr.colors.keys()}

    def observe_tap(self, *, target_color: str, chosen_color: str, is_correct: bool, reaction_time_ms: int, hints_used: int, wrong_count: int):
        self.window.append({
            "target": target_color,
            "chosen": chosen_color,
            "correct": is_correct,
            "rt": reaction_time_ms,
            "hints": hints_used,
            "wrong": wrong_count,
        })
        if len(self.window) > self.max_window:
            self.window.pop(0)
        if not is_correct:
            self.repeat_budget[target_color] += 1
        else:
            self.repeat_budget[target_color] = max(0, self.repeat_budget[target_color] - 1)

    def confidence_score(self) -> int:
        # Simple confidence heuristic
        score = 70
        for e in self.window[-10:]:
            if not e["correct"]:
                score -= 10
            if e["rt"] > 6000:
                score -= 10
            if e["rt"] < 2000 and e["correct"]:
                score += 5
        return max(0, min(100, score))

    def plan_round(self, round_index: int) -> RoundPlan:
        colors = list(self.curr.colors.keys())

        # If child is struggling with a color recently, repeat it (but vary shape/position)
        struggle_color = self._detect_struggle_color()
        if struggle_color:
            target = struggle_color
        else:
            # choose new target, avoid repeating same as last if possible
            target = random.choice(colors)
            if self.last_target and target == self.last_target and len(colors) > 1:
                target = random.choice([c for c in colors if c != self.last_target])

        self.last_target = target
        assert_color(self.curr, target)

        # Choose 2 distractors (different colors)
        distractors = [c for c in colors if c != target]
        random.shuffle(distractors)
        distractors = distractors[:2]

        # Choose shapes and slots
        shapes = list(self.curr.shapes)
        random.shuffle(shapes)
        chosen_shapes = shapes[:3] if len(shapes) >= 3 else (shapes * 3)[:3]

        slots = ["left", "right", "bottom"]
        random.shuffle(slots)

        obj_colors = [target, distractors[0], distractors[1]]
        random.shuffle(obj_colors)

        objects = []
        for i in range(3):
            shape_id = chosen_shapes[i]
            color_id = obj_colors[i]
            pos_slot = slots[i]
            assert_shape(self.curr, shape_id)
            assert_color(self.curr, color_id)
            objects.append(ObjSpec(shape_id=shape_id, color_id=color_id, pos_slot=pos_slot))

        # Hint policy adapts if confidence low
        base_idle = int(self.curr.hint_defaults.get("idle_ms", 4500))
        conf = self.confidence_score()
        idle_ms = base_idle if conf >= 50 else max(2500, base_idle - 1500)

        hint = HintPolicy(idle_ms=idle_ms, max_voice_repeats=int(self.curr.hint_defaults.get("max_voice_repeats", 1)))
        style = FeedbackStyle(tone="warm" if conf < 60 else "cheerful", speed="slow" if conf < 60 else "normal")

        return RoundPlan(target_color=target, objects=objects, hint_policy=hint, feedback_style=style)

    def _detect_struggle_color(self) -> Optional[str]:
        # Look at last 6 events; if a color has 2+ wrongs, flag it
        recent = self.window[-6:]
        wrong_counts: Dict[str, int] = {}
        for e in recent:
            if not e["correct"]:
                wrong_counts[e["target"]] = wrong_counts.get(e["target"], 0) + 1
        if not wrong_counts:
            return None
        worst = max(wrong_counts.items(), key=lambda kv: kv[1])
        if worst[1] >= 2:
            return worst[0]
        return None
