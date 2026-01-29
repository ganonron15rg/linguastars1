import json
import pygame
from pathlib import Path

from src.scene_find_tap import FindAndTapScene
from src.style import load_tokens
from src.validator import load_curriculum, Curriculum


def run_app() -> int:
    pygame.init()
    pygame.mixer.quit()  # keep v1 stable on systems with audio issues

    tokens = load_tokens(Path("style/tokens.json"))
    curriculum: Curriculum = load_curriculum(Path("curriculum/colors_v1.json"))

    w = tokens["screen"]["width"]
    h = tokens["screen"]["height"]
    fps = tokens["screen"]["fps"]

    # Mobile-like portrait window
    screen = pygame.display.set_mode((w, h))
    pygame.display.set_caption("LinguaStars v1 — Colors Find & Tap")

    clock = pygame.time.Clock()
    scene = FindAndTapScene(screen=screen, tokens=tokens, curriculum=curriculum)

    running = True
    while running:
        dt = clock.tick(fps) / 1000.0  # seconds
        for event in pygame.event.get():
            if event.type == pygame.QUIT:
                running = False
            else:
                scene.handle_event(event)

        scene.update(dt)
        scene.draw()
        pygame.display.flip()

        if scene.is_done:
            running = False

    scene.on_exit()
    pygame.quit()
    return 0
