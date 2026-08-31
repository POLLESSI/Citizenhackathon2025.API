from fastapi import FastAPI

app = FastAPI(
    title="OutZen AI",
    version="1.0.0",
)


@app.get("/")
async def root():
    return {
        "service": "OutZen.AI",
        "status": "running",
    }

@app.get("/health")
async def health():
    return {
         "service": "OutZen.AI",
         "status": "healthy",
    }
