# GHOST in the Robots — project page

Static project page for **"GHOST in the Robots: Real-Time Exocentric Dual-Robot VR
Teleoperation from Onboard Cameras."** No build step — plain HTML/CSS/JS.

The page is intentionally **anonymous** for double-blind review and is structured so
de-anonymizing later is a small edit (see *De-anonymizing* below).

## Run locally

```bash
# from this folder
python3 serve.py          # default port 8000; or: python3 serve.py 9000
# then open http://localhost:8000
```

`serve.py` is a thin wrapper around Python's built-in server that adds HTTP Range
support (so the video streams/seeks properly — Safari needs this) and silences the
harmless `BrokenPipeError` tracebacks you get from `python3 -m http.server` when the
browser stops fetching the video mid-file. GitHub Pages handles all this natively in
production. Plain `python3 -m http.server 8000` still works if you don't mind the noise.

Opening `index.html` directly with `file://` also works, but the "hide Paper button
if the PDF is missing" check only runs over `http(s)://`.

## Deploy to GitHub Pages

1. Push this folder to a repo (contents at the repo root, or in `/docs`).
2. Settings → Pages → deploy from branch → `main` → `/ (root)` (or `/docs`).
3. The included `.nojekyll` file keeps Pages from reprocessing the site.

## Add your media (auto-replaces the placeholders)

Every image/video slot shows a clean labeled placeholder until the real file exists,
then swaps in automatically. Just drop files at these exact paths:

| Slot on the page              | Put the file here                          | Paper figure it maps to |
|-------------------------------|--------------------------------------------|-------------------------|
| Hero video (top of page)      | `assets/videos/overview.mp4`  ✅ added     | narrated overview video |
| Social/OG preview image       | `assets/images/teaser.png`                 | `first_image_nanobanana.png` |
| System diagram                | `assets/images/system-diagram.png`         | `GHOSTv2 … Page-4.drawio.png` |
| Depth completion before/after | `assets/images/depth-completion.png`       | `sparse_pcd.png` + `completed_pcd.png` (stack into one image) |
| Task 1 … Task 9               | `assets/images/tasks/1_final.jpg … 9_final.jpg`  ✅ added | `task_initial_end_imgs/N_final.jpg` |
| Novice results chart          | `assets/images/novice-results.png`         | `new_user_task_times_and_rates.pdf` → export PNG |
| Expert times chart            | `assets/images/expert-times.png`           | `expert_time.pdf` → export PNG |
| (optional) Tablet interface   | `assets/images/tablet-interface.png`       | `tablet_interface_gemini.png` |
| (optional) RGB-only ablation  | `assets/images/rgb-only.png`               | `GHOST-RGB-Only2.png`   |
| Paper PDF                     | `assets/pdf/paper.pdf`                      | the manuscript          |

Notes:
- Charts in the paper are PDFs — export them to PNG (e.g. at 2× for crispness) before dropping in.
- Task images are already wired to your `N_final.jpg` files with the correct
  (paper) mapping — display 3↔file 4, display 4↔file 3, display 8↔file 5, and files
  6/7/8 → displays 5/6/7. Verified visually, so no renaming needed.

## De-anonymizing (camera-ready / public version)

Open `index.html` and follow the comment block near the top. In short:
1. Replace the `#authors` block with the commented-out real-author markup right below it.
2. Re-enable the **arXiv** and **Code** buttons in the hero and add real links.
3. Update the **BibTeX** entry (`#bibtex`).

The real author list is kept only in an HTML comment in `index.html` and is **not**
rendered on the page.

## Structure

```
index.html          all content/sections
css/style.css        styles (light/dark, responsive)
js/main.js           placeholders, copy-BibTeX, nav highlight, PDF check
assets/
  images/            figures + teaser-poster.svg
  images/tasks/      task1..task9
  videos/            teaser.mp4, overview.mp4
  pdf/               paper.pdf
```
