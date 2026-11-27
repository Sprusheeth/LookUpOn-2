# Research package

This directory contains an eight-page technical prototype study dated 7 November 2026, its editable source, online reference ledger, and selected local evidence. It is a project report, not a claim of acceptance at a conference or journal. Actual authors/affiliation and any venue-specific formatting can be added when preparing a submission.

## Contents

- `LookUpOn-Research-Paper.pdf`: formatted report with an architecture diagram, three tables, numbered citations, and clickable source links.
- `LookUpOn-Research-Paper.md`: editable manuscript and Mermaid diagram source.
- `references.bib`: eight BibTeX entries corresponding to manuscript references [1]-[8].
- `sources.json`: verified primary/institutional URLs, access dates, supported claims, and evidence boundaries.
- `evidence/manifest.json`: selected implementation/report hashes and environment/scope information.
- `evidence/validation.txt`: recorded Unity validator result.
- `evidence/b787309b-peer-a.json` and `b787309b-peer-b.json`: recorded smoke results.
- `evidence/78b4b1a9-results.json`: selected actual owner-loss outcomes, including failed-save status.
- `evidence/inventory-summary.json`: derived primary/backup counts from both pre-failure observers, with original artifact paths.
- `evidence/node-tests.txt`: dashboard/model test output captured during report preparation.

The original large inventory files remain under `Builds/BackupPolicy`; this package preserves their hashes and a summary. The manifest binds selected files at preparation time, not future edits. It is not a signed provenance statement or independent storage proof.

## Rebuild the PDF

The game/admin do not need the Python packages below. They are only for generating the paper and rendering its QA pages. In a Python environment of your choice:

```powershell
python -m pip install -r .\Research\requirements.txt
python .\Tools\Build-ResearchPaper.py
```

Run from the LookUpOn project root. The builder reads the Markdown, recreates the PDF, verifies basic extracted content and eight reference entries, then renders all pages and a contact sheet under `Builds/ResearchQA`. Review every page after a substantive manuscript/layout change. The renderer implements the Markdown subset used by this paper and draws the architecture figure directly; update its figure if changing the system diagram.

The recorded PDF was visually reviewed on every page. Rebuilding does not rerun Unity/game trials, update empirical results, or regenerate the evidence manifest automatically. If changing the study, collect new evidence using [the testing guide](../docs/TESTING.md), revise the claims/tables, and refresh hashes deliberately.

## Evidence boundary

The paper separates recorded Unity/game tests from browser-model checks and tests rerun during documentation preparation. The selected lifecycle trials were local processes on one laptop and reported denied AppData writes. They establish live availability behavior, not remote disk durability. External references provide background; they do not supply LookUpOn measurements. The PCG institutional record was used for its taxonomy summary, not restricted full text.
