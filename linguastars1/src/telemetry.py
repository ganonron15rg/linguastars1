from __future__ import annotations
from dataclasses import dataclass, asdict
from typing import List, Optional
import time
import json


@dataclass
class RoundResult:
    round_index: int
    target_color: str
    chosen_color: Optional[str]
    is_correct: bool
    reaction_time_ms: int
    hints_used: int
    wrong_count: int


@dataclass
class SceneSummary:
    scene_id: str
    duration_ms: int
    learned: List[str]
    confidence_score: int
    rounds: List[RoundResult]


class Telemetry:
    def __init__(self, scene_id: str):
        self.scene_id = scene_id
        self.t0 = time.time()
        self.rounds: List[RoundResult] = []
        self.learned: List[str] = []

    def add_round(self, rr: RoundResult):
        self.rounds.append(rr)
        if rr.is_correct and rr.target_color not in self.learned:
            self.learned.append(rr.target_color)

    def build_summary(self, confidence_score: int) -> SceneSummary:
        dur = int((time.time() - self.t0) * 1000)
        return SceneSummary(
            scene_id=self.scene_id,
            duration_ms=dur,
            learned=self.learned[:],
            confidence_score=confidence_score,
            rounds=self.rounds[:],
        )

    @staticmethod
    def to_json(summary: SceneSummary) -> str:
        d = asdict(summary)
        d["rounds"] = [asdict(r) for r in summary.rounds]
        return json.dumps(d, indent=2)
