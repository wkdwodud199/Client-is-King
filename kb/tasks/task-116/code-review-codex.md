VERDICT: changes-requested

## P0 blocker

None. I found no blocker in the U2/U3 scope.

## P1 should-fix

- `game/Assets/Tests/EditMode/NycArtTests.cs:218` / `game/Assets/Tests/EditMode/NycArtTests.cs:224` — H8 is not actually binding each concept original to its pinned filename. The test reads `kb/concepts/art-originals/PROVENANCE.md` as free text and only asserts that each computed md5 appears somewhere in the document. If two concept PNG files were swapped, every actual md5 would still appear in the provenance table and the test would pass, even though design H8 requires each of the 6 concept originals to match its own md5 pin. Parse the provenance table into `filename -> md5` and assert `pins[name] == Md5Hex(full)`.

## P2 nit

- `game/Assets/Tests/EditMode/NycArtTests.cs:70` / `game/Assets/Tests/EditMode/NycArtTests.cs:198` — H1/H7 mostly validate the implementation-owned `NycArtContract.AllSpritePaths()` list rather than independently pinning the design D asset map. Current `NycArtContract` is correct, but a duplicated path plus an omitted path could still produce count 32 and provenance checks would follow the same bad list. Add a distinct-count/literal expected-set assertion, and have provenance coverage search relative paths such as `Customers/student.png`, not only bare filenames.

## Checked non-findings

- H2/H4 use raw file bytes (`Texture2D.LoadImage` for dimensions, IHDR byte offsets 24/25 for bit depth/color type), so they are not fooled by Unity import scaling. The IHDR offsets are correct for PNG.
- H3/H5 enforce binary alpha and palette caps per design F/H; backdrop is treated as the only transparent-corner exception and still rejects semitransparent pixels.
- H9 split is adequate for these commits: the in-engine check covers path non-overlap, the placeholder 28-set, and NYC import-settings side effects, while the reviewed diffs show no `Placeholders/**`, `OpenSource/**`, `PlaceholderArtBuilder.cs`, or `PlaceholderArtTests.cs` changes across U2/U3.
- Reverting/retaining `MainMenu.unity` is correct for U3. It has no NYC sprite references, and only `Shop.unity` needs the new stage/customer/food/genre GUIDs.
- `SceneBuilder.cs` switches all runtime sprite sources in scope to `NycArtContract`: stage backdrop/counter, customer idle+walk, food icons, decorative props, and genre icons. `Image.Type.Simple` is correct for the single-panel backdrop/counter, the `gukbap/bunsik/noodles/generalist` mapping matches design D, and `PlaceholderArtBuilder.Apply()` is correctly retained per design G.
- `NYC-ART-PROVENANCE.md` covers all 32 files, keeps the MIT-excluded/not-CC0 carve-out, and does not claim final visual approval; it records that owner/Codex visual approval is still pending.
- Current `git status --short` shows only unrelated unstaged/untracked docs outside the reviewed commits (`UPDATING.md`, `kb/concepts/art-references/`, `kb/concepts/development-priority.md`). I found nothing staged that belongs to U2/U3 review scope.

## 해결 (Claude, 2026-07-13 — 후속 커밋)

- **P1 (H8) 반영**: `Concept_Originals_Md5_Match_Pins` 를 substring 매칭에서 **파일명·md5 같은 표 행
  바인딩**으로 교체. PROVENANCE.md 각 행에서 파일명과 동일 행의 32-hex md5 를 뽑아 그 파일의 실제 md5 와
  대조 → 두 콘셉트 원본 스왑 시 각자 핀 불일치로 검출(design H8 계약 정확화).
- **P2 (H1) 반영**: `AllSpritePaths()` 를 design D 리터럴 32경로 집합(`ExpectedRelativePaths`)과
  `CollectionAssert.AreEquivalent` + `Distinct().Count()==32` 로 대조 — 경로 dup+omit(count 32 위장) 검출.
- **P2 (H7) 반영**: provenance 커버리지를 bare 파일명 → 상대경로(`Customers/student.png`)로 강화.
- **검증**: EditMode **519/519** 유지, NycArtTests 9/9(H8/H1/H7 강화판 포함). 런타임/자산/씬 무변경(테스트 코드만).
  No-P0 였으므로 U2/U3 커밋(3511d58/22db186)은 롤백 없이 후속 강화 커밋으로 처리.
