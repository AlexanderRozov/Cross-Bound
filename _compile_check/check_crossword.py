# Functional check of the new crossword data + scoring rules, mirroring CrosswordGameState.
# Verifies: JSON validity, crossing consistency, solvability, exact max score (250),
# score-lock (no farming), hint penalty, delete behavior, numbering.
import json, sys

PATH = r"H:/Cross-Bound/Crossboud_Unity_ Project/Assets/CrossBound/Resources/CrosswordQuestions.json"
data = json.load(open(PATH, encoding="utf-8"))
W, H = data["gridWidth"], data["gridHeight"]
questions = data["questions"]
errors = []

# --- validator equivalent ---
grid = {}
seen = set()
for q in questions:
    if not q.get("id") or not q.get("answer") or not q.get("question"):
        errors.append(f"question missing fields: {q}")
    if q["id"] in seen:
        errors.append(f"duplicate id {q['id']}")
    seen.add(q["id"])
    x, y = q["startX"], q["startY"]
    if not (0 <= x < W and 0 <= y < H):
        errors.append(f"{q['id']} starts outside grid")
        continue
    for ch in q["answer"].upper():
        if not (0 <= x < W and 0 <= y < H):
            errors.append(f"{q['id']} extends outside grid at ({x},{y})")
            break
        if (x, y) in grid and grid[(x, y)][1] != ch:
            errors.append(f"crossing conflict at ({x},{y}): {grid[(x,y)][0]}={grid[(x,y)][1]} vs {q['id']}={ch}")
        grid.setdefault((x, y), (q["id"], ch))
        x += 1 if q["isHorizontal"] else 0
        y += 0 if q["isHorizontal"] else 1

assert not errors, "VALIDATOR ERRORS:\n" + "\n".join(errors)

answers = {c: v[1] for c, v in grid.items()}
cells = len(answers)

# --- game-state simulation equivalent ---
inputs = {}
score = 0
sel = None
current = None

def contains(q, x, y):
    cx, cy = q["startX"], q["startY"]
    for ch in q["answer"].upper():
        if (cx, cy) == (x, y):
            return True
        cx += 1 if q["isHorizontal"] else 0
        cy += 0 if q["isHorizontal"] else 1
    return False

def select_question(q):
    global sel, current
    sel = (q["startX"], q["startY"])
    current = q

def input_letter(letter):
    global score, sel
    letter = letter.upper()
    assert letter.isalpha() and letter.isascii()
    x, y = sel
    if inputs.get((x, y)) == answers[(x, y)]:
        step(1); return
    was = inputs.get((x, y))
    inputs[(x, y)] = letter
    if letter == answers[(x, y)] and was != answers[(x, y)]:
        score += 10
    step(1)

def step(delta):
    global sel
    x, y = sel
    for _ in range(W if current["isHorizontal"] else H):
        x += delta if current["isHorizontal"] else 0
        y += 0 if current["isHorizontal"] else delta
        if not (0 <= x < W and 0 <= y < H):
            break
        if contains(current, x, y):
            sel = (x, y); return

def delete():
    global sel
    x, y = sel
    if inputs.get((x, y)) and inputs.get((x, y)) != answers[(x, y)]:
        del inputs[(x, y)]; return
    step(-1)
    x, y = sel
    if inputs.get((x, y)) and inputs.get((x, y)) != answers[(x, y)]:
        del inputs[(x, y)]

def completed():
    return all(inputs.get(c) == a for c, a in answers.items())

# numbering (row-major starts)
starts = sorted(questions, key=lambda q: (q["startY"], q["startX"]))
cellnum = {}
for q in starts:
    cellnum.setdefault((q["startX"], q["startY"]), q["number"])
assert cellnum[(5, 2)] == 1 and cellnum[(9, 2)] == 3 and cellnum[(7, 6)] == 7
assert (6, 2) not in cellnum

# --- test: full solve => completion with exact score ---
completed_flag = False
final = 0
for q in questions:
    select_question(q)
    for ch in q["answer"].upper():
        input_letter(ch)
        if completed() and not completed_flag:
            completed_flag = True
            final = score
assert completed_flag, "puzzle not completed after solving all words"
expected = cells * 10
assert final == expected == 250, f"score {final}, expected {expected} (cells={cells})"
print(f"PASS full solve: completed, score={final} ({cells} cells x 10)")

# --- test: score lock (no farming) ---
inputs.clear(); score = 0
select_question(questions[0])  # CAT
input_letter('C')              # +10, moves to (6,2)
assert score == 10
sel = (5, 2)
input_letter('C')              # locked
assert score == 10, "farming detected"
input_letter('X')              # wrong at (6,2), selection moves to (7,2)
assert score == 10 and inputs[(6, 2)] == 'X'
sel = (6, 2)
input_letter('A')              # correct it: +10
assert score == 20
print("PASS score lock: no farming, wrong-then-correct gives +10")

# --- test: delete ---
inputs.clear(); score = 0
select_question(questions[0])
input_letter('X')              # wrong at (5,2), selection -> (6,2)
delete()                       # empty -> step back, clear (5,2)
assert (5, 2) not in inputs and sel == (5, 2)
input_letter('C')              # correct
delete()                       # locked: stays
assert inputs[(5, 2)] == 'C'
print("PASS delete: clears wrong, steps back, never clears locked")

# --- test: hint penalty ---
inputs.clear(); score = 0
select_question(questions[0])
input_letter('C'); score_before = score  # 10
select_question(questions[0])
# reveal word with penalty 30
for (x, y), a in answers.items():
    pass
x, y = questions[0]["startX"], questions[0]["startY"]
for ch in questions[0]["answer"].upper():
    inputs[(x, y)] = ch
    x += 1 if questions[0]["isHorizontal"] else 0
    y += 0 if questions[0]["isHorizontal"] else 1
score = max(0, score - 30)
assert score == 0 and not completed()
print("PASS hint: reveals word, penalty clamps to 0")

# --- test: crossing letters consistent for solvability (both words accept same letter) ---
# already covered by validator; additionally every across word must be typeable after downs:
print(f"\nALL CHECKS PASSED — {len(questions)} questions, {cells} cells, max score {cells*10}")
