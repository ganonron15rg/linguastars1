from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy import func
from sqlalchemy.orm import Session

from app.auth import create_child_token
from app.db import get_db
from app.deps import require_role
from app.models import Child, EconomyTransaction, Inventory, Progress, Session, User
from app.schemas import (
    ChildCreate,
    ChildResponse,
    ChildSummaryResponse,
    ChildTokenResponse,
    EconomyTransactionResponse,
    ProgressResponse,
    SessionResponse,
)

router = APIRouter(prefix="/parent", tags=["parent"])


@router.get("/children", response_model=list[ChildResponse])
def list_children(
    parent: User = Depends(require_role("parent")),
    db: Session = Depends(get_db),
) -> list[ChildResponse]:
    return db.query(Child).filter(Child.parent_id == parent.id).all()


@router.post("/children", response_model=ChildResponse)
def create_child(
    payload: ChildCreate,
    parent: User = Depends(require_role("parent")),
    db: Session = Depends(get_db),
) -> ChildResponse:
    child = Child(parent_id=parent.id, name=payload.name, age=payload.age)
    db.add(child)
    db.commit()
    db.refresh(child)
    inventory = Inventory(child_id=child.id, coins_balance=0, owned_items_json={}, equipped_json={})
    db.add(inventory)
    db.commit()
    return child


@router.post("/child/{child_id}/token", response_model=ChildTokenResponse)
def create_child_token_for_child(
    child_id: int,
    parent: User = Depends(require_role("parent")),
    db: Session = Depends(get_db),
) -> ChildTokenResponse:
    child = (
        db.query(Child)
        .filter(Child.id == child_id, Child.parent_id == parent.id)
        .first()
    )
    if not child:
        raise HTTPException(status_code=404, detail="Child not found")

    token = create_child_token(child.id)
    return ChildTokenResponse(child_id=child.id, access_token=token)


@router.get("/child/{child_id}/summary", response_model=ChildSummaryResponse)
def child_summary(
    child_id: int,
    parent: User = Depends(require_role("parent")),
    db: Session = Depends(get_db),
) -> ChildSummaryResponse:
    child = (
        db.query(Child)
        .filter(Child.id == child_id, Child.parent_id == parent.id)
        .first()
    )
    if not child:
        raise HTTPException(status_code=404, detail="Child not found")
    inventory = db.query(Inventory).filter(Inventory.child_id == child.id).first()
    progress_count = db.query(Progress).filter(Progress.child_id == child.id).count()
    sessions_count = db.query(Session).filter(Session.child_id == child.id).count()
    total_coins = (
        db.query(func.sum(EconomyTransaction.amount))
        .filter(EconomyTransaction.child_id == child.id, EconomyTransaction.type == "earn")
        .scalar()
        or 0
    )
    return ChildSummaryResponse(
        child=child,
        inventory=inventory,
        progress_count=progress_count,
        sessions_count=sessions_count,
        total_coins_earned=total_coins,
    )


@router.get("/child/{child_id}/progress", response_model=list[ProgressResponse])
def child_progress(
    child_id: int,
    parent: User = Depends(require_role("parent")),
    db: Session = Depends(get_db),
) -> list[ProgressResponse]:
    child = (
        db.query(Child)
        .filter(Child.id == child_id, Child.parent_id == parent.id)
        .first()
    )
    if not child:
        raise HTTPException(status_code=404, detail="Child not found")
    return db.query(Progress).filter(Progress.child_id == child.id).all()


@router.get("/child/{child_id}/sessions", response_model=list[SessionResponse])
def child_sessions(
    child_id: int,
    parent: User = Depends(require_role("parent")),
    db: Session = Depends(get_db),
) -> list[SessionResponse]:
    child = (
        db.query(Child)
        .filter(Child.id == child_id, Child.parent_id == parent.id)
        .first()
    )
    if not child:
        raise HTTPException(status_code=404, detail="Child not found")
    return db.query(Session).filter(Session.child_id == child.id).all()


@router.get("/child/{child_id}/transactions", response_model=list[EconomyTransactionResponse])
def child_transactions(
    child_id: int,
    parent: User = Depends(require_role("parent")),
    db: Session = Depends(get_db),
) -> list[EconomyTransactionResponse]:
    child = (
        db.query(Child)
        .filter(Child.id == child_id, Child.parent_id == parent.id)
        .first()
    )
    if not child:
        raise HTTPException(status_code=404, detail="Child not found")
    return (
        db.query(EconomyTransaction)
        .filter(EconomyTransaction.child_id == child.id)
        .order_by(EconomyTransaction.timestamp.desc())
        .all()
    )
