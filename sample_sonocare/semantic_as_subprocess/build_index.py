#!/usr/bin/env python3
"""
Build FAISS index from processed ICD data.
Run this after prepare_data.py

Usage:
    python build_index.py
"""

import pandas as pd
import numpy as np
import faiss
from sentence_transformers import SentenceTransformer
import os

# Check for processed data
if not os.path.exists('processed_obstetrics_data.csv'):
    print("ERROR: processed_obstetrics_data.csv not found")
    print("Please run 'python prepare_data.py' first")
    exit(1)

# Load processed data
print("Loading processed data...")
df = pd.read_csv('processed_obstetrics_data.csv')
print(f"Loaded {len(df)} rows")

# Check search_text column exists
if 'search_text' not in df.columns:
    print("ERROR: 'search_text' column not found in CSV")
    print(f"Available columns: {df.columns.tolist()}")
    exit(1)

print("\nSample search_text values:")
print(df['search_text'].head(10).tolist())

# Initialize embedding model
model_name = 'pritamdeka/S-PubMedBert-MS-MARCO'
print(f"\nLoading embedding model: {model_name}...")
print("(This may take a minute on first run...)")
model = SentenceTransformer(model_name)

# Generate embeddings
print("\nGenerating embeddings for all records...")
embeddings = model.encode(
    df['search_text'].tolist(),
    show_progress_bar=True,
    convert_to_numpy=True
)
print(f"Embeddings shape: {embeddings.shape}")

# Build FAISS index
print("\nBuilding FAISS index...")
dimension = embeddings.shape[1]
index = faiss.IndexFlatL2(dimension)  # L2 distance (lower = more similar)
index.add(embeddings.astype(np.float32))
print(f"FAISS index built with {index.ntotal} vectors")

# Save index and metadata
print("\nSaving index and metadata...")
faiss.write_index(index, 'icd_search.index')
print("✓ Index saved to: icd_search.index")

df.to_pickle('icd_metadata.pkl')
print("✓ Metadata saved to: icd_metadata.pkl")

print("\n✓ Done! Ready to use with icd_recommender_service.py")
