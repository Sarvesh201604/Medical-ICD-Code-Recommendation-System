#!/usr/bin/env python3
"""
ICD Recommender Service - FAISS-based implementation
Callable from C# via Python.NET without needing a FastAPI server
Uses FAISS for semantic search instead of ChromaDB
"""

import json
import os
import sys
from typing import List, Dict
from pathlib import Path
import pandas as pd
import numpy as np
import faiss
from sentence_transformers import SentenceTransformer
import warnings

# Suppress all warnings
warnings.filterwarnings('ignore')

# Initialize on module load
_index = None
_metadata_df = None
_model = None
_initialized = False


def _log(message: str):
    """Print to stderr to keep stdout clean for JSON output"""
    print(f"[ICD-PY] {message}", file=sys.stderr, flush=True)


def _find_data_file(filename: str) -> str:
    """Find data file in multiple possible locations"""
    possible_paths = [
        Path(__file__).parent / filename,
        Path.cwd() / filename,
        Path(__file__).parent.parent / filename,
        Path.cwd().parent / filename,
    ]
    
    for path in possible_paths:
        if path.exists():
            return str(path)
    
    return None


def initialize():
    """Initialize the recommender system with FAISS - call once at app startup"""
    global _index, _metadata_df, _model, _initialized
    
    if _initialized:
        print("[ICD-PY] Already initialized", flush=True)
        return True
    
    try:
        print("[ICD-PY] Starting FAISS initialization...", flush=True)
        print(f"[ICD-PY] Current directory: {os.getcwd()}", flush=True)
        print(f"[ICD-PY] Script location: {__file__}", flush=True)
        print(f"[ICD-PY] Python version: {sys.version}", flush=True)
        
        # Load embedding model
        print("[ICD-PY] Loading sentence-transformers model...", flush=True)
        model_name = 'pritamdeka/S-PubMedBert-MS-MARCO'
        _model = SentenceTransformer(model_name)
        print("[ICD-PY] Model loaded successfully", flush=True)
        
        # Find FAISS index
        print("[ICD-PY] Looking for FAISS index...", flush=True)
        index_path = _find_data_file("icd_search.index")
        if not index_path:
            print("[ICD-PY] ERROR: Could not find icd_search.index", flush=True)
            return False
        
        print(f"[ICD-PY] Found index at: {index_path}", flush=True)
        _index = faiss.read_index(index_path)
        print(f"[ICD-PY] FAISS index loaded with {_index.ntotal} vectors", flush=True)
        
        # Find and load metadata
        print("[ICD-PY] Looking for metadata pickle...", flush=True)
        metadata_path = _find_data_file("icd_metadata.pkl")
        if not metadata_path:
            # Try loading from CSV instead
            print("[ICD-PY] Pickle not found, trying CSV...", flush=True)
            csv_path = _find_data_file("processed_obstetrics_data.csv")
            if not csv_path:
                print("[ICD-PY] ERROR: Could not find metadata pickle or CSV", flush=True)
                return False
            print(f"[ICD-PY] Loading metadata from CSV: {csv_path}", flush=True)
            _metadata_df = pd.read_csv(csv_path)
        else:
            print(f"[ICD-PY] Loading metadata from pickle: {metadata_path}", flush=True)
            _metadata_df = pd.read_pickle(metadata_path)
        
        print(f"[ICD-PY] Metadata loaded: {len(_metadata_df)} rows", flush=True)
        print(f"[ICD-PY] Columns: {_metadata_df.columns.tolist()}", flush=True)
        
        print("[ICD-PY] FAISS recommender initialized successfully!", flush=True)
        _initialized = True
        return True
    except Exception as e:
        print(f"[ICD-PY] ERROR during initialization: {type(e).__name__}: {e}", flush=True)
        import traceback
        traceback.print_exc()
        return False


def get_icd_codes(query: str, num_recommendations: int = 5) -> str:
    """
    Get ICD code recommendations for a medical query.
    
    Args:
        query: Medical impression/description
        num_recommendations: Number of top results to return (default: 5)
    
    Returns:
        JSON string with recommended codes and details
    """
    import time
    start_time = time.time()
    print(f"[ICD-PY] get_icd_codes START: '{query[:60]}...'", flush=True)
    
    if not _initialized:
        print(f"[ICD-PY] Not initialized, calling initialize()...", flush=True)
        if not initialize():
            return json.dumps({"error": "Recommender not initialized", "codes": [], "success": False})
    
    try:
        # Embed the query
        print(f"[ICD-PY] Encoding query...", flush=True)
        query_embedding = _model.encode([query], show_progress_bar=False, convert_to_numpy=True)
        
        # Search FAISS index
        print(f"[ICD-PY] Searching FAISS index...", flush=True)
        distances, indices = _index.search(query_embedding.astype(np.float32), num_recommendations)
        
        # Extract results
        print(f"[ICD-PY] Extracting results from metadata...", flush=True)
        codes_list = []
        
        for idx, distance in zip(indices[0], distances[0]):
            if idx < len(_metadata_df):
                row = _metadata_df.iloc[idx]
                
                # Extract fields based on available columns
                icd_code = row.get('IcdCode_str', '')
                impression = row.get('Impression', '')
                investigation_status = row.get('InvestigationStatus', '')
                description = row.get('IcdDescription', '')
                
                codes_list.append({
                    "code": str(icd_code),
                    "impression": str(impression),
                    "status": str(investigation_status),
                    "description": str(description),
                    "similarity_score": float(distance)
                })
        
        elapsed = time.time() - start_time
        print(f"[ICD-PY] SUCCESS! Got {len(codes_list)} codes in {elapsed:.2f}s", flush=True)
        
        return json.dumps({
            "success": True,
            "codes": codes_list,
            "count": len(codes_list),
            "query": query,
            "elapsed_ms": elapsed * 1000
        })
    except Exception as e:
        elapsed = time.time() - start_time
        print(f"[ICD-PY] ERROR after {elapsed:.2f}s: {type(e).__name__}: {e}", flush=True)
        import traceback
        traceback.print_exc()
        return json.dumps({
            "success": False,
            "error": str(e),
            "codes": [],
            "query": query
        })


def get_health_status() -> str:
    """Get health status of the recommender system"""
    if not _initialized:
        return json.dumps({
            "status": "not_initialized",
            "message": "Call initialize() first",
            "healthy": False
        })
    
    try:
        return json.dumps({
            "status": "healthy",
            "initialized": _initialized,
            "index_size": _index.ntotal if _index else 0,
            "metadata_rows": len(_metadata_df) if _metadata_df is not None else 0,
            "message": "ICD recommender service is ready",
            "healthy": True
        })
    except Exception as e:
        return json.dumps({
            "status": "error",
            "healthy": False,
            "message": str(e)
        })

