from fastapi import FastAPI
from pydantic import BaseModel

from app.routers import admin, auth, parent, sync

app = FastAPI(title="LinguaStars API", version="0.2.0")


class HealthResponse(BaseModel):
    status: str
    service: str


@app.get("/health", response_model=HealthResponse)
def health_check() -> HealthResponse:
    return HealthResponse(status="ok", service="server")


app.include_router(auth.router)
app.include_router(parent.router)
app.include_router(admin.router)
app.include_router(sync.router)
