# -*- coding: utf-8 -*-
"""기획서 PDF의 대화 테이블(1~67: 강아지/고양이/토끼)을 dialogue.json으로 추출."""
import json
import re
import sys

import pdfplumber

sys.stdout.reconfigure(encoding="utf-8")

PDF = r"C:\Users\probo\Documents\카카오톡 받은 파일\AI게임대회.pdf"
OUT = r"C:\Users\probo\EscapefromDoppelgangerVillage\Assets\Data\dialogue.json"

WANT_ANIMALS = {"강아지", "고양이", "토끼"}


from kiwipiepy import Kiwi

kiwi = Kiwi()
SENT = "\x00"  # 줄바꿈 위치 표식 — 단어 중간 개행인지 단어 경계인지 나중에 Kiwi로 판별


def clean(cell: str) -> str:
    if cell is None:
        return ""
    t = cell.replace("\n", SENT)
    t = re.sub(r"[ ]{2,}", " ", t)
    t = re.sub(r" +([?!.,~])", r"\1", t)
    return t.strip()


def junction_needs_space(a_tail: str, b_head: str) -> bool:
    """줄바꿈 접합부에 공백이 필요한지 Kiwi 띄어쓰기 교정으로 판별."""
    out = kiwi.space(a_tail + b_head, reset_whitespace=False)
    n = 0
    for idx, ch in enumerate(out):
        if ch != " ":
            n += 1
            if n == len(a_tail):
                return idx + 1 < len(out) and out[idx + 1] == " "
    return False


def resolve(text: str) -> str:
    """센티널(줄바꿈 위치)을 접합부 분석 결과에 따라 공백 또는 무공백으로 치환."""
    while SENT in text:
        i = text.index(SENT)
        before, after = text[:i], text[i + 1 :]
        if not before or not after or before.endswith(" ") or after.startswith(" "):
            text = before + after
            continue
        m_a = re.search(r"[^ \x00]+$", before)
        m_b = re.match(r"[^ \x00]+", after)
        a_tail = m_a.group(0)[-12:] if m_a else ""
        b_head = m_b.group(0)[:12] if m_b else ""
        sep = " " if (a_tail and b_head and junction_needs_space(a_tail, b_head)) else ""
        text = before + sep + after
    return text


rows = []  # raw rows across pages, merged for page-break continuations
with pdfplumber.open(PDF) as pdf:
    # 대화 테이블은 5~10페이지 (1-indexed) 에 걸쳐 있음 (엔트리 1~67 + 68 이후 일부)
    for pageno in range(4, 11):  # 0-indexed pages 4..10 → 페이지 5..11
        page = pdf.pages[pageno]
        for table in page.extract_tables():
            for r in table:
                if not r or all(c in (None, "") for c in r):
                    continue
                first = clean(r[0]) if r[0] else ""
                # header row
                if first in ("번호",) or (len(r) > 1 and clean(r[1] or "") == "동물"):
                    continue
                if first.isdigit():
                    rows.append([clean(c) for c in r])
                else:
                    # 페이지 넘김으로 잘린 행: 직전 행에 이어붙임
                    if rows:
                        for i, c in enumerate(r):
                            if c and i < len(rows[-1]):
                                rows[-1][i] = (rows[-1][i] + clean(c)).strip()

entries = []
for r in rows:
    # 예상 컬럼: 번호 | 동물 | 유형 | 질문 | 정상답변 | 도플갱어답변
    if len(r) < 6:
        print("SHORT ROW:", r)
        continue
    num = int(r[0])
    animal, qtype = resolve(r[1]), resolve(r[2])
    q, normal, doppel = resolve(r[3]), resolve(r[4]), resolve(r[5])
    if animal not in WANT_ANIMALS:
        continue
    entries.append(
        {
            "id": num,
            "animal": animal,
            "type": qtype,
            "question": q,
            "normalAnswer": normal,
            "doppelAnswer": doppel,
        }
    )

# 페이지 하단 경계에 걸려 테이블 감지에서 누락된 2개 행 — PDF 원문 텍스트에서 이식
entries.append({
    "id": 23, "animal": "고양이", "type": "일상",
    "question": "털이 많이 빠지네.",
    "normalAnswer": "스트레스 받아서 털갈이하나 봐요.",
    "doppelAnswer": "제 피부가 녹아내리며 살점과 함께 떨어지고 있는 겁니다.",
})
entries.append({
    "id": 61, "animal": "토끼", "type": "핵심",
    "question": "번식력이 엄청나다며?",
    "normalAnswer": "부끄럽게 왜 그런 걸 물어봐요!",
    "doppelAnswer": "당신의 배 속에 이미 제 알들을 수십 개 낳아두었습니다.",
})

# Kiwi 접합부 판별이 놓친 띄어쓰기 수동 보정 (눈검수 결과)
FIXUPS = {
    (47, "doppelAnswer"): ("씹을 때피가", "씹을 때 피가"),
    (57, "normalAnswer"): ("경계해야하니까요", "경계해야 하니까요"),
}
for e in entries:
    for (fid, field), (bad, good) in FIXUPS.items():
        if e["id"] == fid and bad in e[field]:
            e[field] = e[field].replace(bad, good)

entries.sort(key=lambda e: e["id"])

# ---- 원문 교차 검증: 공백 제거 후 각 필드가 PDF 원문에 부분 문자열로 존재해야 함 ----
from pypdf import PdfReader
reader = PdfReader(PDF)
fulltext = "".join((reader.pages[i].extract_text() or "") for i in range(4, 11))
squash = lambda s: re.sub(r"\s+", "", s)
haystack = squash(fulltext)
haystack = re.sub(r"AI게임대회\d{1,2}", "", haystack)  # 페이지 푸터 제거 (경계에 걸린 행 검증용)
misses = []
for e in entries:
    for k in ("question", "normalAnswer", "doppelAnswer"):
        if squash(e[k]) not in haystack:
            misses.append((e["id"], k, e[k]))
if misses:
    for m in misses:
        print("MISMATCH:", m)
    raise SystemExit("원문 교차 검증 실패")
print("cross-check vs pypdf fulltext: all fields OK")

# ---- validation ----
ids = [e["id"] for e in entries]
assert ids == list(range(1, 68)), f"ID 누락/중복: {sorted(set(range(1,68)) - set(ids))} / dupes: {[i for i in ids if ids.count(i)>1]}"
counts = {}
for e in entries:
    counts[e["animal"]] = counts.get(e["animal"], 0) + 1
print("counts:", counts)  # 강아지 22, 고양이 23, 토끼 22 expected
for e in entries:
    assert e["type"] in ("일상", "핵심"), f"bad type id={e['id']}: {e['type']!r}"
    for k in ("question", "normalAnswer", "doppelAnswer"):
        assert e[k], f"empty {k} id={e['id']}"

import os
os.makedirs(os.path.dirname(OUT), exist_ok=True)
with open(OUT, "w", encoding="utf-8") as f:
    json.dump({"entries": entries}, f, ensure_ascii=False, indent=2)
print(f"OK: {len(entries)} entries -> {OUT}")
print("---- full dump for eyeball review ----")
for e in entries:
    print(f"[{e['id']:>2} {e['animal']} {e['type']}] Q: {e['question']} | N: {e['normalAnswer']} | D: {e['doppelAnswer']}")
