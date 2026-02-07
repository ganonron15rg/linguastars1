from datetime import datetime
from typing import Any, Optional

from pydantic import BaseModel, ConfigDict, EmailStr, Field


class TokenResponse(BaseModel):
    access_token: str
    token_type: str = "bearer"


class UserCreate(BaseModel):
    email: EmailStr
    password: str = Field(min_length=8)
    role: str


class UserLogin(BaseModel):
    email: EmailStr
    password: str


class UserResponse(BaseModel):
    id: int
    email: EmailStr
    role: str
    created_at: datetime
    model_config = ConfigDict(from_attributes=True)


class ChildTokenResponse(BaseModel):
    child_id: int
    access_token: str
    token_type: str = "bearer"


class ChildCreate(BaseModel):
    name: str
    age: int = Field(ge=3, le=18)


class ChildResponse(BaseModel):
    id: int
    parent_id: int
    name: str
    age: int
    created_at: datetime
    model_config = ConfigDict(from_attributes=True)


class ProgressResponse(BaseModel):
    child_id: int
    level_id: str
    mastery: int
    attempts: int
    errors: int
    last_seen: Optional[datetime]
    model_config = ConfigDict(from_attributes=True)


class SessionResponse(BaseModel):
    id: int
    child_id: int
    date: datetime
    duration_seconds: int
    success_rate: int
    activities_done: int
    model_config = ConfigDict(from_attributes=True)


class EconomyTransactionResponse(BaseModel):
    id: int
    child_id: int
    type: str
    amount: int
    meta_json: dict[str, Any]
    timestamp: datetime
    model_config = ConfigDict(from_attributes=True)


class InventoryResponse(BaseModel):
    child_id: int
    coins_balance: int
    owned_items_json: dict[str, Any]
    equipped_json: dict[str, Any]
    model_config = ConfigDict(from_attributes=True)


class ChildSummaryResponse(BaseModel):
    child: ChildResponse
    inventory: Optional[InventoryResponse]
    progress_count: int
    sessions_count: int
    total_coins_earned: int


class AdminOverviewResponse(BaseModel):
    total_parents: int
    total_admins: int
    total_children: int
    total_sessions: int
    total_transactions: int


class AdminFunnelResponse(BaseModel):
    registrations: int
    parents_with_children: int
    children_with_sessions: int


class AdminLevelsResponse(BaseModel):
    level_id: str
    avg_mastery: float
    total_attempts: int
    total_errors: int


class AdminEconomyHealthResponse(BaseModel):
    total_earned: int
    total_spent: int
    active_children: int


class SyncProgressPayload(BaseModel):
    level_id: str
    mastery: int
    attempts: int
    errors: int
    last_seen: Optional[datetime]


class SyncSessionPayload(BaseModel):
    date: datetime
    duration_seconds: int
    success_rate: int
    activities_done: int


class SyncTransactionPayload(BaseModel):
    type: str
    amount: int
    meta_json: dict[str, Any] = Field(default_factory=dict)
    timestamp: Optional[datetime] = None


class SyncInventoryPayload(BaseModel):
    coins_balance: int
    owned_items_json: dict[str, Any] = Field(default_factory=dict)
    equipped_json: dict[str, Any] = Field(default_factory=dict)


class SyncUploadRequest(BaseModel):
    idempotency_key: str
    child_id: int
    progress: list[SyncProgressPayload] = Field(default_factory=list)
    sessions: list[SyncSessionPayload] = Field(default_factory=list)
    economy_transactions: list[SyncTransactionPayload] = Field(default_factory=list)
    inventory: Optional[SyncInventoryPayload] = None


class SyncUploadResponse(BaseModel):
    status: str
    processed_at: Optional[datetime]
    idempotency_key: str
    errors: Optional[str] = None
