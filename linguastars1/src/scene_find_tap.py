from __future__ import annotations
import math
import time
from dataclasses import dataclass
from typing import List, Optional, Tuple

import pygame

from src.tutor_agent import TutorAgent, RoundPlan, ObjSpec
from src.telemetry import Telemetry, RoundResult
from src.validator import Curriculum, assert_color, format_line


class SceneState:
    INTRO = "INTRO"
    PROMPT = "PROMPT"
    WAIT_TAP = "WAIT_TAP"
    FEEDBACK = "FEEDBACK"
    TRANSITION = "TRANSITION"
    REWARD = "REWARD"
    END = "END"


@dataclass
class GameObject:
    spec: ObjSpec
    rect: pygame.Rect
    base_rect: pygame.Rect
    is_target: bool = False
    glow_t: float = 0.0
    shake_t: float = 0.0

    def contains(self, pos) -> bool:
        return self.rect.collidepoint(pos)


class FindAndTapScene:
    def __init__(self, *, screen: pygame.Surface, tokens: dict, curriculum: Curriculum):
        self.screen = screen
        self.tokens = tokens
        self.curr = curriculum
        self.state = SceneState.INTRO
        self.is_done = False

        self.w, self.h = screen.get_size()
        sm = tokens["safe_margins"]
        self.safe_top = sm["top"]
        self.safe_bottom = sm["bottom"]
        self.safe_side = sm["side"]

        self.palette = tokens["palette"]
        self.outline_th = int(tokens["outline"]["thickness"])
        self.obj_size = int(tokens["object"]["size"])
        self.obj_pad = int(tokens["object"]["tap_padding"])
        self.pulse_amp = float(tokens["object"]["pulse_amp"])

        self.font = pygame.font.SysFont("arial", 34, bold=True)
        self.small_font = pygame.font.SysFont("arial", 26, bold=False)

        self.tutor = TutorAgent(curriculum)
        self.telemetry = Telemetry(scene_id=self.curr.meta.get("scene_id", "colors_find_tap_v1"))

        self.round_index = 1
        self.round_plan: Optional[RoundPlan] = None
        self.objects: List[GameObject] = []

        self.prompt_start_ms = 0
        self.last_input_ms = pygame.time.get_ticks()
        self.hints_used = 0
        self.voice_repeats = 0
        self.wrong_count = 0

        # feedback bookkeeping
        self.pending_feedback_correct: Optional[bool] = None
        self.pending_chosen_color: Optional[str] = None
        self.feedback_start_ms = 0

        # bubble text as placeholder “voice”
        self.bubble_text = ""
        self.bubble_until_ms = 0

        self._enter_intro()

    # ---------- State entry helpers ----------
    def _enter_intro(self):
        self.state = SceneState.INTRO
        self._say(self.curr.voice_lines["intro"][0], ms=2200)
        self.intro_until_ms = pygame.time.get_ticks() + 2400

    def _enter_prompt(self):
        self.state = SceneState.PROMPT
        self.round_plan = self.tutor.plan_round(self.round_index)
        assert_color(self.curr, self.round_plan.target_color)
        self.objects = self._build_objects(self.round_plan.objects, target_color=self.round_plan.target_color)

        # reset round trackers
        self.hints_used = 0
        self.voice_repeats = 0
        self.wrong_count = 0

        # "voice"
        template = self.curr.voice_lines["prompt"]["template"]
        self._say(format_line(template, color=self.round_plan.target_color), ms=1600)

        self.prompt_start_ms = pygame.time.get_ticks()
        self.last_input_ms = self.prompt_start_ms
        self.input_lock_until_ms = self.prompt_start_ms + 200  # avoid accidental taps

        # go quickly into wait
        self.state = SceneState.WAIT_TAP

    def _enter_feedback(self, is_correct: bool, chosen_color: str):
        self.state = SceneState.FEEDBACK
        self.pending_feedback_correct = is_correct
        self.pending_chosen_color = chosen_color
        self.feedback_start_ms = pygame.time.get_ticks()

        if is_correct:
            tmpl = self.curr.voice_lines["label"]["template"]
            self._say(format_line(tmpl, color=self.round_plan.target_color), ms=900)
        else:
            tmpl = self.curr.voice_lines["oops"]["template"]
            self._say(format_line(tmpl, color=self.round_plan.target_color), ms=1200)

    def _enter_transition(self):
        self.state = SceneState.TRANSITION
        self._say(self._pick_praise(), ms=600)
        self.transition_until_ms = pygame.time.get_ticks() + 650

    def _enter_reward(self):
        self.state = SceneState.REWARD
        learned = self.telemetry.learned[:]
        # Ensure 3 items for template
        colors = list(self.curr.colors.keys())
        while len(learned) < 3:
            for c in colors:
                if c not in learned:
                    learned.append(c)
                if len(learned) >= 3:
                    break
        tmpl = self.curr.voice_lines["reward"]["template"]
        self._say(format_line(tmpl, c1=learned[0], c2=learned[1], c3=learned[2]), ms=2600)
        self.reward_start_ms = pygame.time.get_ticks()
        self.reward_until_ms = self.reward_start_ms + 3200

    def _enter_end(self):
        self.state = SceneState.END
        self.is_done = True

    # ---------- Public API ----------
    def handle_event(self, event: pygame.event.Event):
        if self.state != SceneState.WAIT_TAP:
            return
        if event.type == pygame.MOUSEBUTTONDOWN and event.button == 1:
            now = pygame.time.get_ticks()
            if now < getattr(self, "input_lock_until_ms", 0):
                return

            pos = pygame.mouse.get_pos()
            self.last_input_ms = now
            for obj in self.objects:
                if obj.contains(pos):
                    chosen_color = obj.spec.color_id
                    is_correct = (chosen_color == self.round_plan.target_color)
                    if is_correct:
                        obj.glow_t = 0.8
                    else:
                        obj.shake_t = 0.5
                        self.wrong_count += 1

                    rt = max(0, now - self.prompt_start_ms)
                    self.tutor.observe_tap(
                        target_color=self.round_plan.target_color,
                        chosen_color=chosen_color,
                        is_correct=is_correct,
                        reaction_time_ms=rt,
                        hints_used=self.hints_used,
                        wrong_count=self.wrong_count,
                    )
                    self._enter_feedback(is_correct=is_correct, chosen_color=chosen_color)
                    break

    def update(self, dt: float):
        now = pygame.time.get_ticks()

        # Update object animations
        for obj in self.objects:
            if obj.glow_t > 0:
                obj.glow_t = max(0.0, obj.glow_t - dt)
            if obj.shake_t > 0:
                obj.shake_t = max(0.0, obj.shake_t - dt)

        if self.state == SceneState.INTRO:
            if now >= self.intro_until_ms:
                self._enter_prompt()

        elif self.state == SceneState.WAIT_TAP:
            self._maybe_hint(now)
            self._apply_pulse(now)

        elif self.state == SceneState.FEEDBACK:
            # keep feedback short
            elapsed = now - self.feedback_start_ms
            if self.pending_feedback_correct:
                if elapsed >= 750:
                    # log round result
                    rr = RoundResult(
                        round_index=self.round_index,
                        target_color=self.round_plan.target_color,
                        chosen_color=self.pending_chosen_color,
                        is_correct=True,
                        reaction_time_ms=max(0, self.feedback_start_ms - self.prompt_start_ms),
                        hints_used=self.hints_used,
                        wrong_count=self.wrong_count,
                    )
                    self.telemetry.add_round(rr)
                    self._enter_transition()
            else:
                if elapsed >= 900:
                    # back to waiting, with potentially earlier hinting if repeated wrong
                    self.state = SceneState.WAIT_TAP
                    # If multiple wrongs, trigger quicker hint
                    if self.wrong_count >= 2 and self.round_plan:
                        self.hints_used = max(self.hints_used, 1)
                    self.last_input_ms = now  # give a short grace

        elif self.state == SceneState.TRANSITION:
            if now >= self.transition_until_ms:
                self.round_index += 1
                if self.round_index <= 3:
                    self._enter_prompt()
                else:
                    self._enter_reward()

        elif self.state == SceneState.REWARD:
            if now >= self.reward_until_ms:
                self._enter_end()

    def draw(self):
        self._draw_background()
        self._draw_top_ui()
        self._draw_objects()
        self._draw_guide()
        if self.state == SceneState.REWARD:
            self._draw_confetti()

    def on_exit(self):
        # Print final summary
        conf = self.tutor.confidence_score()
        summary = self.telemetry.build_summary(confidence_score=conf)
        from src.telemetry import Telemetry
        print("=== SceneSummary ===")
        print(Telemetry.to_json(summary))

    # ---------- Drawing helpers ----------
    def _draw_background(self):
        self.screen.fill(self._hex(self.palette["bg"]))

        # Subtle panel regions
        panel = pygame.Surface((self.w - 2*self.safe_side, 160), pygame.SRCALPHA)
        panel.fill((*self._hex(self.palette["ui_panel"]), int(self.tokens["ui"]["panel_alpha"])))
        self.screen.blit(panel, (self.safe_side, self.safe_top))

    def _draw_top_ui(self):
        # Speech bubble (placeholder for audio)
        x = self.safe_side
        y = self.safe_top + 18
        w = self.w - 2*self.safe_side
        h = 124
        self._rounded_rect((x, y, w, h), fill=self._hex(self.palette["ui_panel"]), radius=int(self.tokens["ui"]["corner_radius"]), alpha=245)

        text = self._current_bubble_text()
        if text:
            surf = self.small_font.render(text, True, self._hex(self.palette["outline"]))
            self.screen.blit(surf, (x + 22, y + 40))

        # Round dots (progress)
        dot_y = y + h + 18
        dot_x = x + 12
        for i in range(1, 4):
            r = 10
            cx = dot_x + (i-1)*34
            cy = dot_y
            col = self._hex(self.palette["outline"]) if i < self.round_index else (40,40,40)
            if i == self.round_index and self.state not in (SceneState.REWARD, SceneState.END):
                col = self._hex(self.palette["accent"])
            pygame.draw.circle(self.screen, col, (cx, cy), r)

    def _draw_objects(self):
        for obj in self.objects:
            self._draw_object(obj)

    def _draw_guide(self):
        # “Room and a Half”-ish: thick outline, simple geometry, quirky expression
        guide_zone_top = 980
        gx = self.safe_side + 110
        gy = min(self.h - self.safe_bottom - 10, guide_zone_top + 90)

        t = pygame.time.get_ticks() / 1000.0
        bob = int(self.tokens["guide"]["idle_bob_px"] * math.sin(t * self.tokens["guide"]["anim_speed"]))
        body_r = int(self.tokens["guide"]["body_size"] * 0.45)

        # body (circle)
        body_center = (gx, gy + bob)
        body_col = self._hex(self.palette["yellow"])
        self._circle_with_outline(body_center, body_r, body_col)

        # antenna (cutesy-creepy ant vibe)
        ax1 = (body_center[0]-18, body_center[1]-body_r-10)
        ax2 = (body_center[0]-34, body_center[1]-body_r-46)
        ax3 = (body_center[0]+18, body_center[1]-body_r-10)
        ax4 = (body_center[0]+34, body_center[1]-body_r-46)
        pygame.draw.line(self.screen, self._hex(self.palette["outline"]), ax1, ax2, self.outline_th//2)
        pygame.draw.line(self.screen, self._hex(self.palette["outline"]), ax3, ax4, self.outline_th//2)
        pygame.draw.circle(self.screen, self._hex(self.palette["accent"]), ax2, 10)
        pygame.draw.circle(self.screen, self._hex(self.palette["accent"]), ax4, 10)
        pygame.draw.circle(self.screen, self._hex(self.palette["outline"]), ax2, 10, self.outline_th//3)
        pygame.draw.circle(self.screen, self._hex(self.palette["outline"]), ax4, 10, self.outline_th//3)

        # eyes
        eye_y = body_center[1] - 14
        for dx in (-18, 18):
            pygame.draw.circle(self.screen, (255,255,255), (body_center[0]+dx, eye_y), 18)
            pygame.draw.circle(self.screen, self._hex(self.palette["outline"]), (body_center[0]+dx, eye_y), 18, self.outline_th//3)
            pupil = 6 if self.state != SceneState.FEEDBACK else 4
            pygame.draw.circle(self.screen, self._hex(self.palette["outline"]), (body_center[0]+dx, eye_y), pupil)

        # mouth depends on last feedback
        mx, my = body_center[0], body_center[1]+22
        if self.state == SceneState.FEEDBACK and self.pending_feedback_correct is False:
            pygame.draw.arc(self.screen, self._hex(self.palette["outline"]), (mx-22, my-6, 44, 26), math.pi, 2*math.pi, self.outline_th//2)
        else:
            pygame.draw.arc(self.screen, self._hex(self.palette["outline"]), (mx-22, my-16, 44, 26), 0, math.pi, self.outline_th//2)

        # Guide prompt “pointer” glow toward target when hinting
        if self.state == SceneState.WAIT_TAP and self._is_hint_active():
            target = self._get_target_object()
            if target:
                pygame.draw.line(self.screen, self._hex(self.palette["accent"]), (gx+40, gy+bob), target.rect.center, 6)

    def _draw_confetti(self):
        # simple confetti dots
        t = pygame.time.get_ticks()
        random.seed(1234)
        for i in range(60):
            x = (i*97 + t//3) % self.w
            y = (i*193 + t//2) % (self.h//2)
            col = random.choice([self._hex(self.palette["red"]), self._hex(self.palette["blue"]), self._hex(self.palette["yellow"]), self._hex(self.palette["accent"])])
            pygame.draw.circle(self.screen, col, (x, y), 6)

    # ---------- Object building & drawing ----------
    def _build_objects(self, specs: List[ObjSpec], *, target_color: str) -> List[GameObject]:
        slots = self._slot_positions()
        out = []
        for s in specs:
            cx, cy = slots[s.pos_slot]
            rect = pygame.Rect(0, 0, self.obj_size, self.obj_size)
            rect.center = (cx, cy)
            base = rect.copy()
            out.append(GameObject(spec=s, rect=rect, base_rect=base, is_target=(s.color_id == target_color)))
        return out

    def _draw_object(self, obj: GameObject):
        color = self._hex(self.curr.colors[obj.spec.color_id]["hex"])
        center = obj.rect.center
        size = obj.rect.width

        # shadow
        shadow_off = 10
        shadow_rect = obj.rect.move(0, shadow_off)
        pygame.draw.ellipse(self.screen, (0,0,0,60), shadow_rect)

        # draw shape with thick outline
        if obj.spec.shape_id == "ball":
            self._circle_with_outline(center, size//2, color)
        elif obj.spec.shape_id == "cube":
            self._rounded_rect(obj.rect, fill=color, radius=22)
            pygame.draw.rect(self.screen, self._hex(self.palette["outline"]), obj.rect, self.outline_th, border_radius=22)
        else:  # star
            self._star(center, size//2, color)

        # glow on correct
        if obj.glow_t > 0:
            a = int(140 * (obj.glow_t / 0.8))
            glow = pygame.Surface((obj.rect.width+30, obj.rect.height+30), pygame.SRCALPHA)
            pygame.draw.ellipse(glow, (*color, a), glow.get_rect())
            self.screen.blit(glow, glow.get_rect(center=center))

        # shake on wrong
        if obj.shake_t > 0:
            t = pygame.time.get_ticks() / 1000.0
            amp = int(self.tokens["anim"]["shake_px"])
            dx = int(amp * math.sin(t * self.tokens["anim"]["shake_hz"]))
            obj.rect.centerx = obj.base_rect.centerx + dx
        else:
            obj.rect.center = obj.base_rect.center

        # Extra tap padding (visual cue)
        pad_rect = obj.rect.inflate(self.obj_pad*2, self.obj_pad*2)
        pygame.draw.rect(self.screen, (0,0,0,18), pad_rect, 2, border_radius=28)

    # ---------- Hinting ----------
    def _maybe_hint(self, now_ms: int):
        if not self.round_plan:
            return
        idle = now_ms - self.last_input_ms
        if idle >= self.round_plan.hint_policy.idle_ms:
            if self.voice_repeats < self.round_plan.hint_policy.max_voice_repeats:
                self.voice_repeats += 1
                self.hints_used += 1
                tmpl = self.curr.voice_lines["hint_repeat"]["template"]
                self._say(format_line(tmpl, color=self.round_plan.target_color), ms=1100)

    def _apply_pulse(self, now_ms: int):
        if not self.round_plan:
            return
        idle = now_ms - self.last_input_ms
        active = idle >= self.round_plan.hint_policy.idle_ms
        for obj in self.objects:
            if obj.is_target and active:
                t = now_ms / 1000.0
                s = 1.0 + self.pulse_amp * (0.5 + 0.5*math.sin(t*5.0))
                new_size = int(self.obj_size * s)
                obj.rect.width = new_size
                obj.rect.height = new_size
                obj.rect.center = obj.base_rect.center
            else:
                obj.rect.size = (self.obj_size, self.obj_size)
                obj.rect.center = obj.base_rect.center

    def _is_hint_active(self) -> bool:
        if not self.round_plan:
            return False
        now = pygame.time.get_ticks()
        return (now - self.last_input_ms) >= self.round_plan.hint_policy.idle_ms

    def _get_target_object(self) -> Optional[GameObject]:
        for obj in self.objects:
            if obj.is_target:
                return obj
        return None

    # ---------- Bubble / “voice” ----------
    def _say(self, text: str, *, ms: int = 1200):
        self.bubble_text = text
        self.bubble_until_ms = pygame.time.get_ticks() + ms

    def _current_bubble_text(self) -> str:
        now = pygame.time.get_ticks()
        if now <= self.bubble_until_ms:
            return self.bubble_text
        return ""

    def _pick_praise(self) -> str:
        import random
        return random.choice(self.curr.voice_lines["praise_short"])

    # ---------- Geometry ----------
    def _slot_positions(self) -> dict:
        # Mid zone: y 360..920-ish
        mid_top = 330
        mid_bottom = 920
        left_x = self.safe_side + 190
        right_x = self.w - self.safe_side - 190
        bottom_x = self.w // 2

        top_y = mid_top + 180
        bottom_y = mid_bottom - 110

        return {
            "left": (left_x, top_y),
            "right": (right_x, top_y),
            "bottom": (bottom_x, bottom_y),
        }

    # ---------- Drawing primitives ----------
    def _hex(self, s: str) -> Tuple[int,int,int]:
        s = s.lstrip("#")
        return (int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16))

    def _rounded_rect(self, rect_like, *, fill, radius: int, alpha: int = 255):
        if isinstance(rect_like, pygame.Rect):
            rect = rect_like
        else:
            rect = pygame.Rect(rect_like)
        surf = pygame.Surface(rect.size, pygame.SRCALPHA)
        pygame.draw.rect(surf, (*fill, alpha), surf.get_rect(), border_radius=radius)
        self.screen.blit(surf, rect.topleft)

    def _circle_with_outline(self, center, radius, fill):
        pygame.draw.circle(self.screen, fill, center, radius)
        pygame.draw.circle(self.screen, self._hex(self.palette["outline"]), center, radius, self.outline_th)

    def _star(self, center, radius, fill):
        cx, cy = center
        pts = []
        for i in range(10):
            ang = i * math.pi / 5.0 - math.pi/2
            r = radius if i % 2 == 0 else radius * 0.45
            pts.append((cx + r*math.cos(ang), cy + r*math.sin(ang)))
        pygame.draw.polygon(self.screen, fill, pts)
        pygame.draw.polygon(self.screen, self._hex(self.palette["outline"]), pts, self.outline_th)
