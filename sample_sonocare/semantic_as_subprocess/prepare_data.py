#!/usr/bin/env python3
"""
Prepare ICD data for FAISS indexing.
Run this script when you have a new ICD data JSON file.

Usage:
    python prepare_data.py <path_to_icd_data.json>
Or:
    python prepare_data.py
    (Will prompt for file path)
"""

import json
import pandas as pd
import sys

# Get file path from command line or prompt user
if len(sys.argv) > 1:
    file_path = sys.argv[1]
else:
    file_path = input("Enter path to ICD JSON data file: ").strip()

if not file_path:
    print("ERROR: No file path provided")
    sys.exit(1)

# Remove quotes if present
file_path = file_path.strip('"\'')

print(f"Loading JSON file from: {file_path}")
try:
    with open(file_path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    if isinstance(data, list):
        df = pd.DataFrame(data)
    else:
        df = pd.DataFrame([data])
except FileNotFoundError:
    print(f"ERROR: File not found: {file_path}")
    sys.exit(1)
except json.JSONDecodeError as e:
    print(f"ERROR: Invalid JSON format: {e}")
    print("Trying JSON Lines format...")
    try:
        records = []
        with open(file_path, 'r', encoding='utf-8') as f:
            for line in f:
                line = line.strip()
                if line:
                    records.append(json.loads(line))
        df = pd.DataFrame(records)
    except Exception as e2:
        print(f"ERROR: Could not parse file: {e2}")
        sys.exit(1)

print(f"Loaded {len(df)} rows, columns: {df.columns.tolist()}")

# Check for required columns
required_cols = ['Impression', 'IcdCode']
missing_cols = [col for col in required_cols if col not in df.columns]
if missing_cols:
    print(f"WARNING: Missing expected columns: {missing_cols}")
    print(f"Available columns: {df.columns.tolist()}")

# Initial exploration
print("\n--- Data overview ---")
print("Missing values:")
print(df.isnull().sum())
print("\nFirst few rows:")
print(df.head(3))

# Preprocessing
print("\n--- Processing data ---")

# Remove rows with missing Impression
df_clean = df[df['Impression'].notna()].copy()
print(f"Rows after removing missing impressions: {len(df_clean)}")

# Function to split comma-separated ICD codes
def split_codes(codes):
    if pd.isna(codes):
        return set()
    return {c.strip() for c in str(codes).split(',')}

df_clean['IcdCode_set'] = df_clean['IcdCode'].apply(split_codes)

# Group by Impression and InvestigationStatus
grouped = df_clean.groupby(['Impression', 'InvestigationStatus']).agg({
    'IcdCode_set': lambda x: set().union(*x),
    'IcdDescription': lambda x: ', '.join(str(v).strip() for v in x.unique()[:5] if pd.notna(v))
}).reset_index()

# Convert sets to strings
grouped['IcdCode_list'] = grouped['IcdCode_set'].apply(lambda s: sorted(s))
grouped['IcdCode_str'] = grouped['IcdCode_list'].apply(lambda l: ', '.join(l))

# Create search text
grouped['search_text'] = grouped['Impression'] + ' ' + grouped['InvestigationStatus'].fillna('')
grouped['search_text'] = grouped['search_text'].str.strip()

# Cleanup
grouped = grouped.drop(columns=['IcdCode_set', 'IcdCode_list'])

# Save
print(f"\nNumber of unique (Impression, InvestigationStatus) pairs: {len(grouped)}")
print("\nSample rows after grouping:")
print(grouped[['Impression', 'InvestigationStatus', 'IcdCode_str']].head(10))

output_file = 'processed_obstetrics_data.csv'
grouped.to_csv(output_file, index=False)
print(f"\n✓ Processed data saved to '{output_file}'")
print(f"Ready for indexing with: python build_index.py")
