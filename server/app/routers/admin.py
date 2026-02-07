from fastapi import APIRouter, Depends
from sqlalchemy import func
from sqlalchemy.orm import Session

from app.db import get_db
from app.deps import require_role
from app.models import Child, EconomyTransaction, Progress, Session, User
from app.schemas import (
    AdminEconomyHealthResponse,
    AdminFunnelResponse,
    AdminLevelsResponse,
    AdminOverviewResponse,
)

router = APIRouter(prefix="/admin", tags=["admin"])


@router.get("/overview", response_model=AdminOverviewResponse)
def overview(
    _: User = Depends(require_role("admin")),
    db: Session = Depends(get_db),
) -> AdminOverviewResponse:
    total_parents = db.query(User).filter(User.role == "parent").count()
    total_admins = db.query(User).filter(User.role == "admin").count()
    total_children = db.query(Child).count()
    total_sessions = db.query(Session).count()
    total_transactions = db.query(EconomyTransaction).count()
    return AdminOverviewResponse(
        total_parents=total_parents,
        total_admins=total_admins,
        total_children=total_children,
        total_sessions=total_sessions,
        total_transactions=total_transactions,
    )


@router.get("/funnel", response_model=AdminFunnelResponse)
def funnel(
    _: User = Depends(require_role("admin")),
    db: Session = Depends(get_db),
) -> AdminFunnelResponse:
    registrations = db.query(User).count()
    parents_with_children = (
        db.query(Child.parent_id).distinct().count()
        if db.query(Child).count() > 0
        else 0
    )
    children_with_sessions = (
        db.query(Session.child_id).distinct().count()
        if db.query(Session).count() > 0
        else 0
    )
    return AdminFunnelResponse(
        registrations=registrations,
        parents_with_children=parents_with_children,
        children_with_sessions=children_with_sessions,
    )


@router.get("/levels/problems", response_model=list[AdminLevelsResponse])
def levels_problems(
    _: User = Depends(require_role("admin")),
    db: Session = Depends(get_db),
) -> list[AdminLevelsResponse]:
    rows = (
        db.query(
            Progress.level_id,
            func.avg(Progress.mastery).label("avg_mastery"),
            func.sum(Progress.attempts).label("total_attempts"),
            func.sum(Progress.errors).label("total_errors"),
        )
        .group_by(Progress.level_id)
        .all()
    )
    return [
        AdminLevelsResponse(
            level_id=row.level_id,
            avg_mastery=float(row.avg_mastery or 0),
            total_attempts=int(row.total_attempts or 0),
            total_errors=int(row.total_errors or 0),
        )
        for row in rows
    ]


@router.get("/economy/health", response_model=AdminEconomyHealthResponse)
def economy_health(
    _: User = Depends(require_role("admin")),
    db: Session = Depends(get_db),
) -> AdminEconomyHealthResponse:
    total_earned = (
        db.query(func.sum(EconomyTransaction.amount))
        .filter(EconomyTransaction.type == "earn")
        .scalar()
        or 0
    )
    total_spent = (
        db.query(func.sum(EconomyTransaction.amount))
        .filter(EconomyTransaction.type == "spend")
        .scalar()
        or 0
    )
    active_children = db.query(EconomyTransaction.child_id).distinct().count()
    return AdminEconomyHealthResponse(
        total_earned=int(total_earned),
        total_spent=int(total_spent),
        active_children=active_children,
    )
