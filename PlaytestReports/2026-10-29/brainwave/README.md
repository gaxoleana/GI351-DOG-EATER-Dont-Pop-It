# Alien brainwave — implementation validation

- Alien telegraph: 0.4 s body tremor and green Angry; no Warning picture.
- Active duration: random 1–2.5 real seconds excluding Pause. Alternating red/blue phases, minimum 0.4 s each. Red requires one press per phase; blue uses the existing release grace. Existing event failure penalty applies.
- Cooldown: random 12–24 real seconds excluding Pause, measured after casting ends; waits for existing events/attacks to finish.
- Portrait uses Alien sprite; transparent red/blue screen tint follows the active rule. No instructional text added.
- Burst laser retains red Angry; normal laser retains its shorter warning.

Unity MCP controlled runtime checks (temporary setup at 21 km, physics disabled for isolation):
- Successful 2.5 s cycle observed Red, Blue, Red, Blue, Red. Red completion was supplied through temporary runtime counters to exercise all transitions, not a human skill assessment.
- Alien portrait present, Warning picture absent, input unlocked and timeScale restored to 1 afterward.
- Green Angry texture: 61,348 green-dominant pixels and zero red-dominant pixels.
- Obstacle interruption gate active; interruption ended brainwave and restored input, timeScale 1, gravity 1. Portrait removed by the next frame.
- Final visual adjustment adds transparent screen tint and moves the portrait above the player. Final scripts compile, Console contains zero errors, diff whitespace check passes.
- Play Mode stopped; temporary test state was discarded. No full 30 km replay or human difficulty score after this addition.
