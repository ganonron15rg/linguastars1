from datetime import datetime

from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session

from app.db import get_db
from app.deps import SyncPrincipal, get_sync_principal
from app.models import Child, EconomyTransaction, Inventory, Progress, Session as StudySession
from app.models import SyncQueue
from app.schemas import SyncUploadRequest, SyncUploadResponse

router = APIRouter(prefix="/sync", tags=["sync"])


@router.post("/upload", response_model=SyncUploadResponse)
def sync_upload(
    payload: SyncUploadRequest,
    principal: SyncPrincipal = Depends(get_sync_principal),
    db: Session = Depends(get_db),
) -> SyncUploadResponse:
    child = db.query(Child).filter(Child.id == payload.child_id).first()
    if not child:
        raise HTTPException(status_code=404, detail="Child not found")
    if principal.role == "child" and principal.child and principal.child.id != child.id:
        raise HTTPException(status_code=403, detail="Forbidden")
    if principal.role == "parent" and principal.user and child.parent_id != principal.user.id:
        raise HTTPException(status_code=403, detail="Forbidden")

    existing = db.query(SyncQueue).filter(SyncQueue.idempotency_key == payload.idempotency_key).first()
    if existing:
        return SyncUploadResponse(
            status=existing.status,
            processed_at=existing.processed_at,
            idempotency_key=existing.idempotency_key,
            errors=existing.error_message,
        )

    queue_item = SyncQueue(idempotency_key=payload.idempotency_key, payload=payload.model_dump())
    db.add(queue_item)
    db.commit()
    db.refresh(queue_item)

    try:
        for entry in payload.progress:
            progress = (
                db.query(Progress)
                .filter(Progress.child_id == child.id, Progress.level_id == entry.level_id)
                .first()
            )
            if progress:
                progress.mastery = entry.mastery
                progress.attempts = entry.attempts
                progress.errors = entry.errors
                progress.last_seen = entry.last_seen
            else:
                db.add(
                    Progress(
                        child_id=child.id,
                        level_id=entry.level_id,
                        mastery=entry.mastery,
                        attempts=entry.attempts,
                        errors=entry.errors,
                        last_seen=entry.last_seen,
                    )
                )

        for entry in payload.sessions:
            db.add(
                StudySession(
                    child_id=child.id,
                    date=entry.date,
                    duration_seconds=entry.duration_seconds,
                    success_rate=entry.success_rate,
                    activities_done=entry.activities_done,
                )
            )

        for entry in payload.economy_transactions:
            db.add(
                EconomyTransaction(
                    child_id=child.id,
                    type=entry.type,
                    amount=entry.amount,
                    meta_json=entry.meta_json,
                    timestamp=entry.timestamp or datetime.utcnow(),
                )
            )

        if payload.inventory:
            inventory = db.query(Inventory).filter(Inventory.child_id == child.id).first()
            if inventory:
                inventory.coins_balance = payload.inventory.coins_balance
                inventory.owned_items_json = payload.inventory.owned_items_json
                inventory.equipped_json = payload.inventory.equipped_json
            else:
                db.add(
                    Inventory(
                        child_id=child.id,
                        coins_balance=payload.inventory.coins_balance,
                        owned_items_json=payload.inventory.owned_items_json,
                        equipped_json=payload.inventory.equipped_json,
                    )
                )

        queue_item.status = "processed"
        queue_item.processed_at = datetime.utcnow()
        db.commit()
    except Exception as exc:
        db.rollback()
        queue_item.status = "failed"
        queue_item.error_message = str(exc)
        queue_item.processed_at = datetime.utcnow()
        db.commit()
        return SyncUploadResponse(
            status=queue_item.status,
            processed_at=queue_item.processed_at,
            idempotency_key=queue_item.idempotency_key,
            errors=queue_item.error_message,
        )

    return SyncUploadResponse(
        status=queue_item.status,
        processed_at=queue_item.processed_at,
        idempotency_key=queue_item.idempotency_key,
    )
