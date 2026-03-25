import json
from pathlib import Path

from langchain_community.document_loaders import PyPDFLoader, DirectoryLoader
from langchain_text_splitters import RecursiveCharacterTextSplitter
from langchain_huggingface import HuggingFaceEmbeddings
from langchain_core.documents import Document


def _to_text(value):
    if value is None:
        return ""
    if isinstance(value, list):
        return "; ".join(str(item).strip() for item in value if str(item).strip())
    return str(value).strip()


def _normalize_records(payload):
    if isinstance(payload, list):
        return payload

    if isinstance(payload, dict):
        for key in ["records", "data", "items", "patients", "entries"]:
            value = payload.get(key)
            if isinstance(value, list):
                return value
        return [payload]

    return []


def _get_value(record, target_key):
    target_key = str(target_key).lower()
    for key, value in record.items():
        if str(key).lower() == target_key:
            return value
    return None


def _first_non_empty(record, candidate_keys):
    for key in candidate_keys:
        value = _to_text(_get_value(record, key))
        if value:
            return value
    return ""


def _extract_icd_codes(record):
    direct_keys = [
        "icd_code",
        "icd_codes",
        "icd10",
        "icd_10",
        "code",
        "codes",
        "diagnosis_code",
        "diagnosis_codes",
    ]

    values = []
    for key in direct_keys:
        text = _to_text(_get_value(record, key))
        if text:
            values.extend([part.strip() for part in text.replace(";", ",").split(",") if part.strip()])

    if not values:
        for key, value in record.items():
            key_lower = str(key).lower()
            if "icd" in key_lower and "code" in key_lower and "desc" not in key_lower and "description" not in key_lower:
                text = _to_text(value)
                if text:
                    values.extend([part.strip() for part in text.replace(";", ",").split(",") if part.strip()])

    # Keep insertion order while removing duplicates.
    deduped = []
    seen = set()
    for code in values:
        if code not in seen:
            deduped.append(code)
            seen.add(code)

    return deduped


#Extract Data From the PDF File
def load_pdf_file(data):
    loader= DirectoryLoader(data,
                            glob="*.pdf",
                            loader_cls=PyPDFLoader)

    documents=loader.load()

    return documents


def load_json_icd_documents(data):
    data_path = Path(data)
    json_files = sorted(data_path.glob("*.json"))
    documents = []

    for json_file in json_files:
        with open(json_file, "r", encoding="utf-8") as file:
            payload = json.load(file)

        records = _normalize_records(payload)

        for index, record in enumerate(records):
            if not isinstance(record, dict):
                content = _to_text(record)
                if not content:
                    continue
                documents.append(
                    Document(
                        page_content=content,
                        metadata={"source": json_file.name, "record_id": str(index)},
                    )
                )
                continue

            impression = ""
            impression = _first_non_empty(
                record,
                [
                    "impression",
                    "impressions",
                    "clinical_impression",
                    "summary",
                    "diagnosis_text",
                    "description",
                    "note",
                ],
            )
            scan_type = _first_non_empty(record, ["scantype", "scan_type", "modality"])
            indication = _first_non_empty(record, ["indication", "history", "clinical_indication"])
            status = _first_non_empty(record, ["investigationstatus", "status"])
            icd_description = _first_non_empty(
                record,
                ["icddescription", "icd_description", "diagnosis_description", "description"],
            )

            icd_codes = _extract_icd_codes(record)

            if not impression and not icd_codes and not icd_description:
                fallback = _to_text(record)
                if not fallback:
                    continue
                documents.append(
                    Document(
                        page_content=fallback,
                        metadata={"source": json_file.name, "record_id": str(index)},
                    )
                )
                continue

            content_lines = []
            if scan_type:
                content_lines.append(f"Scan Type: {scan_type}")
            if status:
                content_lines.append(f"Status: {status}")
            if indication:
                content_lines.append(f"Indication: {indication}")
            content_lines.append(f"Impression: {impression if impression else 'Unknown'}")
            content_lines.append(f"ICD Codes: {', '.join(icd_codes) if icd_codes else 'Unknown'}")
            if icd_description:
                content_lines.append(f"ICD Description: {icd_description}")

            page_content = "\n".join(content_lines)
            record_id = _first_non_empty(record, ["id", "patientid", "record_id"]) or str(index)

            documents.append(
                Document(
                    page_content=page_content,
                    metadata={
                        "source": json_file.name,
                        "record_id": str(record_id),
                        "icd_codes": ", ".join(icd_codes),
                        "icd_description": icd_description,
                        "scan_type": scan_type,
                        "status": status,
                    },
                )
            )

    return documents



#Split the Data into Text Chunks
def text_split(extracted_data):
    text_splitter=RecursiveCharacterTextSplitter(chunk_size=100, chunk_overlap=20)
    text_chunks=text_splitter.split_documents(extracted_data)
    return text_chunks



#Download the Embeddings from HuggingFace 
def download_hugging_face_embeddings():
    # Using all-MiniLM-L6-v2 for fully local setup (no internet required)
    embeddings = HuggingFaceEmbeddings(model_name='sentence-transformers/all-MiniLM-L6-v2')
    return embeddings


class ICDRecommender:
    """
    ICD Code Recommender using Chroma Vector Database
    Provides semantic search for medical impressions to find relevant ICD codes
    """
    
    def __init__(self, chroma_db_path="chroma_db"):
        """
        Initialize the recommender with Chroma database
        
        Args:
            chroma_db_path: Path to the Chroma database directory
        """
        try:
            from langchain_community.vectorstores import Chroma
            
            print(f"[RECOMMENDER] Loading embeddings...", flush=True)
            self.embeddings = download_hugging_face_embeddings()
            
            print(f"[RECOMMENDER] Loading Chroma database from: {chroma_db_path}", flush=True)
            
            # Load both collections
            self.normal_db = Chroma(
                collection_name="medicalbot_normal",
                embedding_function=self.embeddings,
                persist_directory=chroma_db_path
            )
            
            self.abnormal_db = Chroma(
                collection_name="medicalbot_abnormal",
                embedding_function=self.embeddings,
                persist_directory=chroma_db_path
            )
            
            print(f"[RECOMMENDER] OK Chroma database loaded successfully", flush=True)
            self.is_ready = True
            
        except Exception as e:
            print(f"[RECOMMENDER] ERROR Error initializing: {e}", flush=True)
            import traceback
            traceback.print_exc()
            self.is_ready = False
    
    def recommend(self, query: str, category: str = "both", k: int = 5) -> list:
        """
        Get ICD code recommendations for a medical query
        
        Args:
            query: Medical impression text
            category: "both", "normal", or "abnormal"
            k: Number of results to return per collection
        
        Returns:
            List of recommended codes with metadata
        """
        if not self.is_ready:
            print(f"[RECOMMENDER] ERROR Recommender not initialized", flush=True)
            return []
        
        recommendations = []
        
        try:
            print(f"[RECOMMENDER] Searching for: {query[:50]}...", flush=True)
            
            # Search abnormal cases if category includes them
            if category in ["both", "abnormal"]:
                try:
                    abnormal_results = self.abnormal_db.similarity_search(query, k=k)
                    for doc in abnormal_results:
                        icd_field = doc.metadata.get("icd", "").strip()
                        if icd_field:
                            # Keep ICD codes as-is, don't split by comma
                            recommendations.append({
                                "code": icd_field,
                                "description": doc.page_content,
                                "type": "abnormal"
                            })
                except Exception as e:
                    print(f"[RECOMMENDER] Error searching abnormal: {e}", flush=True)
            
            # Search normal cases if category includes them
            if category in ["both", "normal"]:
                try:
                    normal_results = self.normal_db.similarity_search(query, k=k)
                    for doc in normal_results:
                        icd_field = doc.metadata.get("icd", "").strip()
                        if icd_field:
                            # Keep ICD codes as-is, don't split by comma
                            recommendations.append({
                                "code": icd_field,
                                "description": doc.page_content,
                                "type": "normal"
                            })
                except Exception as e:
                    print(f"[RECOMMENDER] Error searching normal: {e}", flush=True)
            
            # Remove duplicates while preserving order
            seen = set()
            unique_recommendations = []
            for rec in recommendations:
                code = rec["code"]
                # Use the full code (unsplit) as the key for deduplication
                if code not in seen:
                    seen.add(code)
                    unique_recommendations.append(rec)
            
            print(f"[RECOMMENDER] OK Found {len(unique_recommendations)} unique recommendations", flush=True)
            return unique_recommendations
            
        except Exception as e:
            print(f"[RECOMMENDER] ERROR Error during recommendation: {e}", flush=True)
            import traceback
            traceback.print_exc()
            return []