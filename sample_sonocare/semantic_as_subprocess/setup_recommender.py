#!/usr/bin/env python3
"""
Setup Script for ICD Recommender
Run this to set up the FAISS-based recommender for your first time.

Usage:
    python setup_recommender.py
    
Then follow the prompts.
"""

import os
import sys
import subprocess
from pathlib import Path

def print_banner(text):
    """Print a formatted banner"""
    print("\n" + "=" * 70)
    print(text.center(70))
    print("=" * 70 + "\n")

def check_python_version():
    """Check Python version"""
    if sys.version_info < (3, 8):
        print("❌ ERROR: Python 3.8+ required")
        print(f"   You have Python {sys.version}")
        return False
    print(f"✓ Python {sys.version.split()[0]} detected")
    return True

def check_pip():
    """Check if pip is available"""
    try:
        subprocess.run(['pip', '--version'], capture_output=True, check=True)
        print("✓ pip is available")
        return True
    except:
        print("❌ ERROR: pip not found")
        return False

def install_requirements():
    """Install required packages"""
    print("\n[1] Installing Python dependencies...")
    print("    (This may take a few minutes on first run)")
    
    try:
        result = subprocess.run(
            [sys.executable, '-m', 'pip', 'install', '-r', 'requirements.txt'],
            capture_output=True,
            text=True,
            timeout=300
        )
        
        if result.returncode != 0:
            print("❌ Failed to install requirements")
            print(result.stderr)
            return False
        
        print("✓ Dependencies installed successfully")
        return True
    except subprocess.TimeoutExpired:
        print("❌ Installation timed out")
        return False
    except Exception as e:
        print(f"❌ Installation error: {e}")
        return False

def get_data_file():
    """Get ICD data file path from user"""
    print("\n[2] Preparing your ICD data...")
    print("    You need an ICD data JSON file to proceed")
    
    while True:
        file_path = input("\nEnter path to your ICD JSON file (or 'skip' to skip): ").strip()
        
        if file_path.lower() == 'skip':
            print("⏭️  Skipping data preparation")
            print("   You can run this later: python prepare_data.py your_file.json")
            return None
        
        # Remove quotes if present
        file_path = file_path.strip('"\'')
        
        if os.path.exists(file_path):
            print(f"✓ Found: {file_path}")
            return file_path
        else:
            print(f"❌ File not found: {file_path}")
            print("   Please provide a valid path")

def prepare_data(data_file):
    """Prepare ICD data"""
    print(f"\n   Processing {os.path.basename(data_file)}...")
    
    try:
        result = subprocess.run(
            [sys.executable, 'prepare_data.py', data_file],
            capture_output=True,
            text=True,
            timeout=120
        )
        
        if result.returncode != 0:
            print(f"❌ Data preparation failed:")
            print(result.stderr)
            return False
        
        if os.path.exists('processed_obstetrics_data.csv'):
            print("✓ Data prepared successfully")
            return True
        else:
            print("❌ Data file was not created")
            return False
    except subprocess.TimeoutExpired:
        print("❌ Data preparation timed out")
        return False
    except Exception as e:
        print(f"❌ Error: {e}")
        return False

def build_index():
    """Build FAISS index"""
    print("\n[3] Building FAISS Index...")
    print("    Loading embeddings and creating search index...")
    print("    (This may take 1-2 minutes for the first time)")
    
    if not os.path.exists('processed_obstetrics_data.csv'):
        print("⏭️  Skipping index build (no processed data)")
        print("   You can run this later: python build_index.py")
        return False
    
    try:
        result = subprocess.run(
            [sys.executable, 'build_index.py'],
            capture_output=True,
            text=True,
            timeout=600
        )
        
        if result.returncode != 0:
            print(f"❌ Index build failed:")
            print(result.stderr)
            return False
        
        if os.path.exists('icd_search.index') and os.path.exists('icd_metadata.pkl'):
            print("✓ FAISS index built successfully")
            return True
        else:
            print("❌ Index files were not created")
            return False
    except subprocess.TimeoutExpired:
        print("❌ Index build timed out (dataset too large?)")
        return False
    except Exception as e:
        print(f"❌ Error: {e}")
        return False

def test_recommender():
    """Test the recommender"""
    print("\n[4] Testing Recommender...")
    
    try:
        result = subprocess.run(
            [sys.executable, 'example_usage.py'],
            capture_output=True,
            text=True,
            timeout=60
        )
        
        if result.returncode != 0:
            print(f"⚠️  Test had issues:")
            print(result.stderr[:500])
            return False
        
        print("✓ Recommender test successful")
        return True
    except Exception as e:
        print(f"⚠️  Could not run test: {e}")
        return False

def show_next_steps():
    """Show next steps"""
    print_banner("Setup Complete! 🎉")
    
    print("""
📋 Next Steps:

1. For Python usage:
   ─────────────────
   from icd_recommender_service import initialize, get_icd_codes
   
   initialize()
   result = get_icd_codes("your medical impression")

2. For C# Integration:
   ──────────────────
   See: CSHARP_INTEGRATION_GUIDE.md

3. To run as HTTP server (optional):
   ──────────────────────────────────
   pip install fastapi uvicorn
   uvicorn app:app --host 0.0.0.0 --port 8000
   
   Then visit: http://localhost:8000/docs

4. For more information:
   ─────────────────────
   • FAISS_IMPLEMENTATION.md - Complete documentation
   • MIGRATION_GUIDE.md - What changed from ChromaDB
   • example_usage.py - Code examples

📊 Files Created:
   • processed_obstetrics_data.csv - Your processed data
   • icd_search.index - FAISS search index
   • icd_metadata.pkl - Metadata for codes
   
🔗 Important Links:
   • FAISS: https://github.com/facebookresearch/faiss
   • Sentence-Transformers: https://www.sbert.net/

Need help? Check the documentation files or the docstrings in the code.
    """)

def main():
    """Main setup flow"""
    print_banner("ICD Recommender Setup (FAISS-based)")
    
    print("""
This script will help you set up the ICD recommender system:

1. Install Python dependencies
2. Prepare your ICD data (if you have it)
3. Build the FAISS search index
4. Test the setup

Let's get started!
    """)
    
    # Step 0: Check prerequisites
    print("[0] Checking Prerequisites...")
    if not check_python_version():
        return False
    if not check_pip():
        return False
    
    # Step 1: Install requirements
    if not install_requirements():
        return False
    
    # Step 2: Prepare data (optional)
    data_file = get_data_file()
    if data_file:
        if not prepare_data(data_file):
            return False
    
    # Step 3: Build index
    build_index()
    
    # Step 4: Test (optional)
    test_recommender()
    
    # Show completion
    show_next_steps()
    return True

if __name__ == "__main__":
    try:
        success = main()
        sys.exit(0 if success else 1)
    except KeyboardInterrupt:
        print("\n\n⚠️  Setup interrupted by user")
        sys.exit(1)
    except Exception as e:
        print(f"\n\n❌ Unexpected error: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
