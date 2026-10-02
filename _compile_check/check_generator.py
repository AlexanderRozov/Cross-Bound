# Simulation of the upgraded crossword generator pipeline against the real cross.txt base.
# Mirrors CrosswordDictionarySource + CrosswordAnalysis + CrosswordBacktracker logic 1:1.
import random, re, time

raw = open(r"H:/Cross-Bound/Crossboud_Unity_ Project/Assets/CrossBound/words/cross.txt", "rb").read().decode("cp1251", errors="replace")
ALPHABET = set("АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ")

entries, seen = [], set()
for line in raw.splitlines():
    sep = line.find(" - ")
    if sep <= 0:
        continue
    w = line[:sep].strip().upper()
    d = line[sep+3:].strip()
    if not w or not d or len(w) < 3 or len(w) > 11:
        continue
    if any(c not in ALPHABET for c in w) or w in seen:
        continue
    seen.add(w)
    entries.append((w, d))

print(f"pool: {len(entries)} unique words (len 3..11)")

def sample(pool, n, seed):
    r = random.Random(seed)
    pool = pool[:]
    for i in range(min(n, len(pool))):
        j = r.randrange(i, len(pool))
        pool[i], pool[j] = pool[j], pool[i]
    return pool[:n]

def order_words(pool):
    freq = {}
    for w, _ in pool:
        for c in set(w):
            freq[c] = freq.get(c, 0) + 1
    return sorted(pool, key=lambda e: (-len(e[0]) * sum(freq.get(c, 0) for c in set(e[0])), -len(e[0]), e[0]))

WIDTH, HEIGHT, WORD_COUNT, MAX_ATTEMPTS = 15, 15, 12, 15000

def generate(ordered):
    def search(placements, cells, attempts):
        if len(placements) >= WORD_COUNT: return True, attempts
        attempts += 1
        if attempts >= MAX_ATTEMPTS: return False, attempts
        used = {p[0][0] for p in placements}
        cands = []
        seen_c = set()
        for word, px, py, pd in placements:
            dx, dy = (1, 0) if pd == 0 else (0, 1)
            for i, ch in enumerate(word):
                cx, cy = px + dx*i, py + dy*i
                nd = 1 - pd
                for w2, _ in ordered:
                    if w2 in used: continue
                    for j, c2 in enumerate(w2):
                        if c2 != ch: continue
                        x = cx - (j if nd == 0 else 0)
                        y = cy - (j if nd == 1 else 0)
                        key = (w2, x, y, nd)
                        if key not in seen_c:
                            seen_c.add(key); cands.append((w2, x, y, nd))
        random.shuffle(cands)
        for c in cands:
            if not try_place(c, placements, cells): continue
            ok, attempts = search(placements, cells, attempts)
            if ok: return True, attempts
            remove(c, placements, cells)
        return False, attempts

    def try_place(c, placements, cells):
        w, x, y, d = c
        dx, dy = (1, 0) if d == 0 else (0, 1)
        if x < 0 or y < 0 or x + dx*(len(w)-1) >= WIDTH or y + dy*(len(w)-1) >= HEIGHT: return False
        if (x-dx, y-dy) in cells or (x+dx*len(w), y+dy*len(w)) in cells: return False
        inter = 0
        for i, ch in enumerate(w):
            cx, cy = x + dx*i, y + dy*i
            if (cx, cy) in cells:
                if cells[(cx, cy)] != ch: return False
                inter += 1; continue
            if d == 0 and ((cx, cy-1) in cells or (cx, cy+1) in cells): return False
            if d == 1 and ((cx-1, cy) in cells or (cx+1, cy) in cells): return False
        if placements and inter == 0: return False
        placements.append(c)
        for i, ch in enumerate(w):
            cells[(x+dx*i, y+dy*i)] = ch
        return True

    def remove(c, placements, cells):
        placements.pop()
        cells.clear()
        for w, x, y, d in placements:
            dx, dy = (1, 0) if d == 0 else (0, 1)
            for i, ch in enumerate(w):
                cells[(x+dx*i, y+dy*i)] = ch

    for seed_w, _ in [e for e in ordered if len(e[0]) <= WIDTH][:25]:
        placements, cells = [], {}
        x = (WIDTH - len(seed_w)) // 2
        if try_place((seed_w, x, HEIGHT // 2, 0), placements, cells):
            ok, _ = search(placements, cells, 0)
            if ok: return placements
    return None

for seed in (42, 7):
    t0 = time.time()
    pool = sample(entries, 600, seed)
    ordered = order_words(pool)
    res = generate(ordered)
    dt = time.time() - t0
    if res:
        # numbering + crossing validation
        starts, nums, n = {}, {}, 1
        for w, x, y, d in sorted(res, key=lambda c: (c[2], c[1], c[3])):
            if (x, y) not in nums:
                nums[(x, y)] = n; n += 1
        grid = {}
        ok = True
        for w, x, y, d in res:
            dx, dy = (1, 0) if d == 0 else (0, 1)
            for i, ch in enumerate(w):
                k = (x+dx*i, y+dy*i)
                if k in grid and grid[k] != ch: ok = False
                grid[k] = ch
        lens = [len(c[0]) for c in res]
        print(f"seed {seed}: OK in {dt:.1f}s — {len(res)} words, {len(grid)} cells, "
              f"avg len {sum(lens)/len(lens):.1f}, crossings valid: {ok}")
    else:
        print(f"seed {seed}: FAILED in {dt:.1f}s")
