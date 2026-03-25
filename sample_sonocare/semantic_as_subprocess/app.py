#!/usr/bin/env python3
"""
ICD Recommender Application - Using FAISS
This module provides ICD code recommendations without requiring an HTTP server.

For C# through PythonNET, simply import this and call:
    from icd_recommender_service import initialize, get_icd_codes, get_health_status
    
    initialize()
    result = get_icd_codes("patient with missed abortion")
    status = get_health_status()
"""

# Import the FAISS-based recommender
from icd_recommender_service import initialize, get_icd_codes, get_health_status

# Optional: FastAPI server (for testing)
# Uncomment below if you want to run as HTTP server
# Otherwise, use the Python functions directly from C#

try:
    from fastapi import FastAPI, HTTPException
    from fastapi.middleware.cors import CORSMiddleware
    from pydantic import BaseModel
    from typing import Optional
    import json as json_lib
    
    # Create FastAPI app (optional, only if you want HTTP endpoints)
    app = FastAPI(
        title="ICD Code Prediction API (FAISS)",
        description="API to predict ICD codes from medical descriptions using FAISS",
        version="2.0.0"
    )
    
    # Add CORS middleware
    app.add_middleware(
        CORSMiddleware,
        allow_origins=["*"],
        allow_credentials=True,
        allow_methods=["*"],
        allow_headers=["*"],
    )
    
    # Pydantic models
    class ICDQuery(BaseModel):
        query: str
        num_recommendations: Optional[int] = 5
    
    class ICDCode(BaseModel):
        code: str
        impression: Optional[str] = None
        status: Optional[str] = None
        description: Optional[str] = None
        similarity_score: Optional[float] = None
    
    class ICDPredictionResponse(BaseModel):
        success: bool
        query: str
        icd_codes: list[ICDCode]
        count: int
    
    # Initialize recommender on app startup
    @app.on_event("startup")
    async def startup_event():
        """Initialize recommender when FastAPI starts"""
        print("[APP] Initializing FAISS recommender...")
        if initialize():
            print("[APP] ✓ Recommender initialized successfully")
        else:
            print("[APP] ✗ Failed to initialize recommender")
    
    # Health check endpoint
    @app.get("/", tags=["Health"])
    def root():
        """Health check endpoint"""
        return {
            "status": "running",
            "service": "ICD Code Prediction API (FAISS-based)",
            "version": "2.0.0"
        }
    
    @app.get("/health", tags=["Health"])
    def health():
        """Health check endpoint"""
        import json
        status = json.loads(get_health_status())
        return status
    
    # Prediction endpoint
    @app.post("/predict-icd", tags=["Predictions"])
    def predict_icd(request: ICDQuery):
        """
        Predict ICD codes from a medical description.
        
        Args:
            query: Medical description or clinical impression
            num_recommendations: Number of top results to return (default: 5)
        
        Returns:
            JSON with recommended ICD codes
        """
        try:
            if not request.query or request.query.strip() == "":
                raise HTTPException(status_code=400, detail="Query cannot be empty")
            
            result_json = get_icd_codes(request.query, request.num_recommendations)
            result = json_lib.loads(result_json)
            
            if not result.get("success", False):
                raise HTTPException(status_code=500, detail=result.get("error", "Unknown error"))
            
            icd_codes = [
                ICDCode(
                    code=item["code"],
                    impression=item.get("impression", ""),
                    status=item.get("status", ""),
                    description=item.get("description", ""),
                    similarity_score=item.get("similarity_score", 0)
                )
                for item in result.get("codes", [])
            ]
            
            return {
                "success": True,
                "query": request.query,
                "icd_codes": icd_codes,
                "count": len(icd_codes)
            }
            
        except HTTPException:
            raise
        except Exception as e:
            print(f"Error: {str(e)}")
            raise HTTPException(status_code=500, detail=f"Error processing request: {str(e)}")
    
    # Alternative endpoint that returns raw JSON
    @app.post("/recommend", tags=["Predictions"])
    def recommend(request: ICDQuery):
        """
        Get ICD recommendations (returns raw JSON).
        
        Args:
            query: Medical description
            num_recommendations: Number of top results to return (default: 5)
        
        Returns:
            Raw JSON result from FAISS recommender
        """
        try:
            if not request.query or request.query.strip() == "":
                raise HTTPException(status_code=400, detail="Query cannot be empty")
            
            result_json = get_icd_codes(request.query, request.num_recommendations)
            return json_lib.loads(result_json)
            
        except Exception as e:
            print(f"Error: {str(e)}")
            return {
                "success": False,
                "error": str(e),
                "codes": []
            }

except ImportError:
    print("[APP] FastAPI not available - running in library mode only")
    print("[APP] Use: from icd_recommender_service import initialize, get_icd_codes")
    app = None


# Standalone mode - for testing without FastAPI
if __name__ == "__main__":
    print("ICD Recommender Service (FAISS-based)")
    print("=" * 50)
    
    # Initialize
    print("\n[INIT] Initializing recommender...")
    if initialize():
        print("[INIT] ✓ Recommender ready!")
        
        # Health check
        print("\n[HEALTH] Checking status...")
        status = get_health_status()
        print(f"[HEALTH] {status}")
        
        # Test query
        print("\n[TEST] Sample query...")
        test_query = "normal fetal development with good amniotic fluid"
        result = get_icd_codes(test_query, num_recommendations=3)
        print(f"[TEST] Query: {test_query}")
        print(f"[TEST] Result: {result}")
    else:
        print("[INIT] ✗ Initialization failed!")
    
    # Optional: Run FastAPI server
    print("\n" + "=" * 50)
    print("To run as HTTP server, use:")
    print("  uvicorn app:app --host 0.0.0.0 --port 8000")

        "service": "ICD Code Prediction API",
        "version": "1.0.0"
    }


@app.get("/health", tags=["Health"])
def health():
    """Health check endpoint"""
    return {
        "status": "healthy",
        "service": "ICD Code Prediction API",
        "collections": {
            "medicalbot_normal": {
                "loaded": normal_retriever is not None,
                "records": normal_count,
                "error": normal_error
            },
            "medicalbot_abnormal": {
                "loaded": abnormal_retriever is not None,
                "records": abnormal_count,
                "error": abnormal_error
            }
        }
    }


@app.post("/predict-icd", response_model=ICDPredictionResponse, tags=["Predictions"])
def predict_icd(request: ICDQuery):
    """
    Predict ICD codes from a medical description using RAG retrieval only.
    
    Args:
        query: Medical description or clinical impression
        description: Optional additional description
    
    Returns:
        ICDPredictionResponse with recommended ICD codes from similar cases
    """
    try:
        if not request.query or request.query.strip() == "":
            raise HTTPException(status_code=400, detail="Query cannot be empty")
        
        input_text = request.query
        if request.description:
            input_text += f"\n\nAdditional context: {request.description}"
        
        result = _recommend_icd_codes(input_text, category=request.category)

        icd_codes = [
            ICDCode(
                code=item["code"],
                description=item.get("description")
            )
            for item in result["all_recommendations"]
        ]
        
        return ICDPredictionResponse(
            query=request.query,
            icd_codes=icd_codes,
            context=(
                f"Found {len(result['abnormal_findings'])} abnormal recommendations and "
                f"{len(result['normal_findings'])} normal recommendation"
            )
        )
        
    except Exception as e:
        print(f"Error: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Error processing request: {str(e)}")


@app.post("/chat", tags=["Chat"])
def chat(request: ICDQuery):
    """
    Retrieve similar medical cases for a query (no LLM processing).
    
    Args:
        query: Medical question or description
    
    Returns:
        Retrieved similar cases with ICD codes
    """
    try:
        if not request.query or request.query.strip() == "":
            raise HTTPException(status_code=400, detail="Query cannot be empty")
        
        result = _recommend_icd_codes(request.query)

        return {
            "query": request.query,
            "normal_findings": result["normal_findings"],
            "abnormal_findings": result["abnormal_findings"],
            "all_recommendations": result["all_recommendations"],
            "similar_cases": result["similar_cases"]
        }
        
    except Exception as e:
        print(f"Error: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Error processing request: {str(e)}")


def _recommend_icd_codes(query: str, category: str = "both") -> dict:
    """Recommend ICD codes using abnormal + normal retrievers."""
    result = {
        "abnormal_findings": [],
        "normal_findings": [],
        "all_recommendations": [],
        "similar_cases": {
            "normal": [],
            "abnormal": []
        }
    }

    # Normalize category
    category = category.lower() if category else "both"

    if abnormal_retriever and category in ["both", "abnormal"]:
        abnormal_docs = abnormal_retriever.invoke(query)
        result["abnormal_findings"] = _extract_recommendations(
            abnormal_docs,
            allow_multiple=True
        )
        result["similar_cases"]["abnormal"] = _format_cases(abnormal_docs, limit=3)

    if normal_retriever and category in ["both", "normal"]:
        normal_docs = normal_retriever.invoke(query)
        normal_codes = _extract_recommendations(normal_docs, allow_multiple=False)
        if normal_codes:
            result["normal_findings"] = [normal_codes[0]]
        result["similar_cases"]["normal"] = _format_cases(normal_docs, limit=2)

    result["all_recommendations"] = result["abnormal_findings"] + result["normal_findings"]
    return result


def _extract_recommendations(docs: list, allow_multiple: bool = True) -> list[dict]:
    """Extract ICD recommendations from retrieved document metadata."""
    codes = {}

    for doc in docs:
        if not hasattr(doc, "metadata"):
            continue

        metadata = doc.metadata or {}
        
        # Try to get ICD code from different possible keys
        icd_code = (
            metadata.get("icd", "").strip() or 
            metadata.get("icd_code", "").strip() or
            (metadata.get("icd_codes", "").strip().split(",")[0] if metadata.get("icd_codes") else "")
        ).strip()
        
        # If not found in metadata, try to extract from page_content
        if not icd_code and hasattr(doc, "page_content"):
            content = doc.page_content
            if "ICD Code:" in content:
                parts = content.split("ICD Code:")
                if len(parts) > 1:
                    icd_code = parts[1].split("|")[0].strip()
            elif "ICD:" in content:
                parts = content.split("ICD:")
                if len(parts) > 1:
                    icd_code = parts[1].split("|")[0].strip()
        
        # Get description from multiple possible sources
        description = (
            metadata.get("icd_description", "").strip() or
            metadata.get("description", "").strip() or 
            "No description"
        )
        
        if not description and hasattr(doc, "page_content"):
            content = doc.page_content
            if "Description:" in content:
                parts = content.split("Description:")
                if len(parts) > 1:
                    description = parts[1].split("|")[0].strip()
        
        # Extract scan type and indication
        scan_type = metadata.get("scan_type", "")
        indication = metadata.get("indication", "")
        
        # Store the code if valid and unique
        if icd_code and icd_code != "Unknown":
            if icd_code not in codes:
                codes[icd_code] = {
                    "code": icd_code,
                    "description": description,
                    "scan_type": scan_type,
                    "indication": indication
                }

    recommendations = list(codes.values())
    if not allow_multiple and recommendations:
        return recommendations[:1]
    return recommendations


def _format_cases(docs: list, limit: int = 3) -> list[dict]:
    """Format a small set of retrieved cases for response payloads."""
    cases = []
    for doc in docs[:limit]:
        metadata = doc.metadata if hasattr(doc, "metadata") else {}
        
        # Try to get ICD code from different keys
        icd_code = (
            metadata.get("icd", "") or 
            metadata.get("icd_code", "")
        ).strip()
        
        case = {
            "impression": doc.page_content[:150] if hasattr(doc, "page_content") else "",
            "icd_code": icd_code,
            "icd_description": metadata.get("icd_description", "").strip() or metadata.get("description", ""),
        }
        cases.append(case)
    return cases


if __name__ == '__main__':
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8080)
