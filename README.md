<div align="center">

# 🩺 Medical ICD Code Recommendation System

**Semantic Search for ICD-10 Code Prediction using FAISS & Medical NLP**

[![Python](https://img.shields.io/badge/Python-3.10%2B-blue?logo=python&logoColor=white)](https://www.python.org/)
[![FastAPI](https://img.shields.io/badge/FastAPI-0.100%2B-009688?logo=fastapi&logoColor=white)](https://fastapi.tiangolo.com/)
[![FAISS](https://img.shields.io/badge/FAISS-Meta-4B8BBE?logo=meta&logoColor=white)](https://github.com/facebookresearch/faiss)
[![.NET](https://img.shields.io/badge/.NET-4.8-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

> A clinical NLP system that maps free-text medical impressions to ICD-10 codes using dense semantic retrieval. Designed for seamless embedding into C# WinForms applications via Python.NET.

</div>

---

## ✨ Key Features

- 🔍 **Semantic Similarity Search** — Uses `pritamdeka/S-PubMedBert-MS-MARCO`, a domain-specific biomedical sentence transformer trained on PubMed and MS-MARCO datasets
- ⚡ **FAISS Vector Index** — Sub-second retrieval over tens of thousands of indexed clinical impressions
- 🏥 **Obstetric Domain Focus** — Curated obstetric ICD-10 codes covering normal and abnormal findings
- 🔗 **C#/.NET Integration** — Callable directly from WinForms via Python using subprocess without running an HTTP server
- 🌐 **Optional REST API** — FastAPI server exposing `/predict-icd` and `/recommend` endpoints for standalone use
- 🧩 **Modular Architecture** — Clean separation of indexing, retrieval service, and API layers

---

## 🏗️ Architecture

```
 ┌─────────────────────────────────────────────────────────────────┐
│                    SonocareWinForms (.NET 4.8)                  │
│         MainForm → IcdSelectionForm → EmbeddedIcdRecommender    │
└────────────────────────┬─────────────────────────────────────────┘
                         │  Process Subprocess
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│              Python ICD Recommender Service                     │
│                                                                 │
│  ┌──────────────┐    ┌──────────────────────────────────────┐   │
│  │  FastAPI App │    │     icd_recommender_service.py       │   │
│  │  (optional)  │───▶│  initialize() / get_icd_codes()    |    |
│  └──────────────┘    └──────────┬──────────────────────────┘    │
│                                 │                               │
│                    ┌────────────▼─────────────┐                 │
│                    │   SentenceTransformer    │                 │
│                    │ S-PubMedBert-MS-MARCO    │                 │
│                    └────────────┬─────────────┘                 │
│                                 │ embed query                   │
│                    ┌────────────▼─────────────┐                 │
│                    │   FAISS Index (.index)   │◀── build_index  │
│                    │   + Metadata (.pkl)      │                 │
│                    └──────────────────────────┘                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📁 Repository Structure ( Data folder isn't uploaded in the github)

```
├── sample_sonocare/
│   └── semantic_as_subprocess/   # Core Python recommendation engine
│       ├── app.py                # FastAPI server + library entry point
│       ├── icd_recommender_service.py  # FAISS search service
│       ├── build_index.py        # Offline FAISS index builder
│       ├── prepare_data.py       # Data preprocessing pipeline
│       ├── setup_recommender.py  # One-shot setup script
│       ├── requirements.txt      # Python dependencies
│       ├── canonical_icd_mapping.json  # Canonical ICD code map
│       ├── src/
│       │   └── helper.py         # Shared utilities
│       └── MultiForm_API/        # C# WinForms client application
│           └── SonocareWinForms/
│               ├── MainForm.cs
│               ├── IcdSelectionForm.cs
│               ├── Services/
│               │   ├── EmbeddedIcdRecommender.cs  # Python.NET bridge
│               │   └── EmbeddedIcdApiClient.cs    # HTTP client (optional)
│               └── SonocareWinForms.csproj
│
└── semantic_initial_phase/
    └── trial/
        └── data/                 # Raw training datasets
            ├── ABNORMAL.json
            ├── NORMAL.json
            ├── ICD_1_Jan_2025To31_Dec_205_latest_fixed.json
            ├── obstetrics_abnormal_test_results_new.csv
            └── obstetrics_normal_test_results.csv
```

---

## 🚀 Quick Start

### Prerequisites

- Python 3.10+
- pip
- .NET Framework 4.8 (for WinForms client)
- Visual Studio 2022 (optional, for C# client)

### 1. Clone the Repository

```bash
git clone https://github.com/AshGov07/Semantic_search_With_Sample_.Net.git
cd Semantic_search_With_Sample_.Net
```

### 2. Install Python Dependencies

```bash
cd sample_sonocare/semantic_as_subprocess
pip install -r requirements.txt
```

### 3. Build the FAISS Index

> ⚠️ **Required before first run.** The `.index` and `.pkl` files are not committed to git (>80MB). You must build them from your data.

```bash
# Step 1: Prepare and preprocess the raw data
python prepare_data.py

# Step 2: Build the FAISS vector index from the processed data
python build_index.py
```

This will produce:

- `icd_search.index` — FAISS flat IP index with normalized embeddings
- `icd_metadata.pkl` — Pandas DataFrame with ICD codes, descriptions, and impressions

### 4. Test the Recommender

```bash
python app.py
```

Expected output:

```
ICD Recommender Service (FAISS-based)
==================================================
[INIT] Initializing recommender...
[ICD-PY] Starting FAISS initialization...
[ICD-PY] Model loaded successfully
[ICD-PY] FAISS index loaded with 45000 vectors
[ICD-PY] FAISS recommender initialized successfully!
[INIT] ✓ Recommender ready!
```

### 5. Run as HTTP Server (Optional)

```bash
uvicorn app:app --host 0.0.0.0 --port 8000
```

API Docs available at: `http://localhost:8000/docs`

---

## 🔌 API Reference

### `POST /predict-icd`

Predict ICD codes from a medical description.

**Request Body**

```json
{
  "query": "normal fetal development with good amniotic fluid",
  "num_recommendations": 5
}
```

**Response**

```json
{
  "success": true,
  "query": "normal fetal development with good amniotic fluid",
  "count": 5,
  "icd_codes": [
    {
      "code": "Z34.1",
      "impression": "Normal singleton pregnancy, first trimester",
      "status": "Normal",
      "description": "Supervision of normal first pregnancy",
      "similarity_score": 0.9312
    }
  ]
}
```

### `POST /recommend`

Returns raw JSON results directly from the FAISS search layer.

### `GET /health`

```json
{
  "status": "healthy",
  "initialized": true,
  "index_size": 45000,
  "metadata_rows": 45000,
  "healthy": true
}
```

---

## 🔗 C# ↔ Python Integration — Subprocess Model

The Sonocare WinForms application (C#) and the ICD Recommender (Python) run in **separate processes**, communicating over standard I/O streams (`stdin` / `stdout`).

### Architecture Decision

| Approach                          | Pros                                | Cons                                   | Chosen?         |
| --------------------------------- | ----------------------------------- | -------------------------------------- | --------------- |
| PythonNET                         | Direct access                       | DLL conflicts, versioning issues       | ❌              |
| HTTP/REST API                     | Standard web integration            | Extra overhead, requires Flask/FastAPI | ⚠️ Optional   |
| **Subprocess stdin/stdout** | **Simple, isolated, no DLLs** | Slightly more IPC overhead             | ✅**YES** |
| Embedded Python                   | Best raw performance                | Complex setup, fragile versioning      | ❌              |

**Why Subprocess?**

- 🔒 **Isolation** — Python runs in a completely separate process
- 🛡️ **Robustness** — A Python crash does not affect the C# UI
- 🌐 **Portability** — Works on Windows, Linux, macOS with no changes
- ⚙️ **No DLL conflicts** — Avoids C#–Python binary compatibility issues entirely

---

### 🚀 Initialization

Initialized **once at startup** via a singleton (`EmbeddedIcdRecommender.Instance`).

```
Python process startup:      500 – 1,500 ms
Loading venv + imports:    1,000 – 2,000 ms
FAISS index load:            500 – 1,000 ms
─────────────────────────────────────────────
Total startup time:              2 – 4 seconds
```

---

### 🔄 Request Flow

```
User types impression
        │
        ▼
ReportForm (UI)          → triggers GetIcd_Click()
        │
        ▼
EmbeddedIcdApiClient     → acquires semaphore (max 5 concurrent)
        │
        ▼
EmbeddedIcdRecommender   → spawns Python subprocess, writes query to stdin
        │
        ▼
Python Subprocess        → encodes query → FAISS search → JSON to stdout
        │
        ▼
C# stdout reader         → parses JSON → IcdPredictionResponse model
        │
        ▼
IcdSelectionForm         → user selects codes → appended to report
```

---

### 📡 Subprocess Communication

```
C# Process                              Python Subprocess
──────────────────────────────────────────────────────────
1. Create ProcessStartInfo
2. Start python.exe
                                        ← Process starts
3. Write query to stdin ─────────────►
                                        3. Read query from stdin
                                        4. Encode query → 768D vector
                                        5. Run FAISS similarity search
                                        6. Retrieve metadata for top-K
4. WaitForExit (max 35 s)
                                        7. Format JSON response
                                        8. Print to stdout ──────────►
5. Read stdout (JSON)
6. Parse result
                                        ← Process exits
──────────────────────────────────────────────────────────
⏱ Typical elapsed time: 30–200 ms (after startup)
```

---

### 📁 Key Files

| File                           | Role                                                 |
| ------------------------------ | ---------------------------------------------------- |
| `ReportForm.cs`              | UI — triggers ICD lookup on button click            |
| `EmbeddedIcdApiClient.cs`    | Manages semaphore, builds request, returns response  |
| `EmbeddedIcdRecommender.cs`  | Spawns subprocess, handles stdin/stdout I/O          |
| `icd_recommender_service.py` | Python core — embedding, FAISS search, JSON output  |
| `icd_search.index`           | FAISS vector index (5,000 records × 768 dimensions) |
| `icd_metadata.pkl`           | Pandas DataFrame with ICD codes and descriptions     |

---

## 🧠 Model Details

| Property      | Value                                          |
| ------------- | ---------------------------------------------- |
| Model         | `pritamdeka/S-PubMedBert-MS-MARCO`           |
| Base          | BioBERT fine-tuned on PubMed abstracts         |
| Task          | Semantic sentence similarity (bi-encoder)      |
| Embedding Dim | 768                                            |
| Index Type    | FAISS `IndexFlatIP` (Inner Product / Cosine) |
| Normalization | L2 normalized embeddings                       |

The inner product search on L2-normalized embeddings is mathematically equivalent to cosine similarity, ensuring consistent semantic ranking across medical terminology.

---

## 📊 Dataset

| File                                             | Description                                     | Size    |
| ------------------------------------------------ | ----------------------------------------------- | ------- |
| `NORMAL.json`                                  | Normal obstetric finding cases with ICD codes   | ~15 MB  |
| `ABNORMAL.json`                                | Abnormal obstetric finding cases with ICD codes | ~4.6 MB |
| `ICD_1_Jan_2025To31_Dec_205_latest_fixed.json` | Full ICD-10 code registry (2025)                | ~26 MB  |
| `obstetrics_normal_test_results.csv`           | Normal cases evaluation set                     | ~9 MB   |
| `obstetrics_abnormal_test_results_new.csv`     | Abnormal cases evaluation set                   | ~3.3 MB |

---

## ⚙️ Configuration

| Variable                | Default                              | Description                       |
| ----------------------- | ------------------------------------ | --------------------------------- |
| `num_recommendations` | `5`                                | Number of top ICD codes to return |
| `model_name`          | `pritamdeka/S-PubMedBert-MS-MARCO` | HuggingFace model ID              |
| `index_path`          | `./icd_search.index`               | Path to FAISS index file          |
| `metadata_path`       | `./icd_metadata.pkl`               | Path to metadata pickle           |

---

## 🐛 Troubleshooting

**`Could not find icd_search.index`**

> Run `python build_index.py` to generate the index from your data files.

**`Recommender not initialized`**

> Call `initialize()` before calling `get_icd_codes()`.

**Slow first query**

> The first query loads the model from disk into memory. Subsequent queries are fast (~50ms).

**C# Python.NET `PythonException`**

> Ensure the correct Python runtime path is set in `EmbeddedIcdRecommender.cs` and that all pip packages are installed in that environment.

---

## 🤝 Contributing

1. Fork the repository
2. Create your feature branch: `git checkout -b feature/your-feature`
3. Commit your changes: `git commit -m 'feat: add your feature'`
4. Push to the branch: `git push origin feature/your-feature`
5. Open a Pull Request

---

## 📄 License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

---

<div align="center">
Built with ❤️ for clinical decision support — helping radiologists and clinicians work smarter.
</div>
