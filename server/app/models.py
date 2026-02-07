from __future__ import annotations

from datetime import datetime
from typing import Optional

from sqlalchemy import JSON, Date, DateTime, ForeignKey, Integer, String, UniqueConstraint
from sqlalchemy.orm import Mapped, mapped_column, relationship

from app.db import Base


class User(Base):
    __tablename__ = "users"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    email: Mapped[str] = mapped_column(String(320), unique=True, index=True)
    password_hash: Mapped[str] = mapped_column(String(255))
    role: Mapped[str] = mapped_column(String(20))
    created_at: Mapped[datetime] = mapped_column(DateTime, default=datetime.utcnow)

    children: Mapped[list["Child"]] = relationship("Child", back_populates="parent")


class Child(Base):
    __tablename__ = "children"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    parent_id: Mapped[int] = mapped_column(ForeignKey("users.id"), index=True)
    name: Mapped[str] = mapped_column(String(120))
    age: Mapped[int] = mapped_column(Integer)
    created_at: Mapped[datetime] = mapped_column(DateTime, default=datetime.utcnow)

    parent: Mapped["User"] = relationship("User", back_populates="children")
    progress_entries: Mapped[list["Progress"]] = relationship(
        "Progress", back_populates="child"
    )
    sessions: Mapped[list["Session"]] = relationship("Session", back_populates="child")
    economy_transactions: Mapped[list["EconomyTransaction"]] = relationship(
        "EconomyTransaction", back_populates="child"
    )
    inventory: Mapped[Optional["Inventory"]] = relationship(
        "Inventory", back_populates="child", uselist=False
    )


class Progress(Base):
    __tablename__ = "progress"
    __table_args__ = (UniqueConstraint("child_id", "level_id", name="uq_progress"),)

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    child_id: Mapped[int] = mapped_column(ForeignKey("children.id"), index=True)
    level_id: Mapped[str] = mapped_column(String(64))
    mastery: Mapped[int] = mapped_column(Integer, default=0)
    attempts: Mapped[int] = mapped_column(Integer, default=0)
    errors: Mapped[int] = mapped_column(Integer, default=0)
    last_seen: Mapped[Optional[datetime]] = mapped_column(DateTime, nullable=True)

    child: Mapped["Child"] = relationship("Child", back_populates="progress_entries")


class Session(Base):
    __tablename__ = "sessions"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    child_id: Mapped[int] = mapped_column(ForeignKey("children.id"), index=True)
    date: Mapped[datetime] = mapped_column(DateTime)
    duration_seconds: Mapped[int] = mapped_column(Integer)
    success_rate: Mapped[int] = mapped_column(Integer)
    activities_done: Mapped[int] = mapped_column(Integer)

    child: Mapped["Child"] = relationship("Child", back_populates="sessions")


class EconomyTransaction(Base):
    __tablename__ = "economy_transactions"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    child_id: Mapped[int] = mapped_column(ForeignKey("children.id"), index=True)
    type: Mapped[str] = mapped_column(String(20))
    amount: Mapped[int] = mapped_column(Integer)
    meta_json: Mapped[dict] = mapped_column(JSON)
    timestamp: Mapped[datetime] = mapped_column(DateTime, default=datetime.utcnow)

    child: Mapped["Child"] = relationship("Child", back_populates="economy_transactions")


class Inventory(Base):
    __tablename__ = "inventory"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    child_id: Mapped[int] = mapped_column(ForeignKey("children.id"), unique=True)
    coins_balance: Mapped[int] = mapped_column(Integer, default=0)
    owned_items_json: Mapped[dict] = mapped_column(JSON, default=dict)
    equipped_json: Mapped[dict] = mapped_column(JSON, default=dict)

    child: Mapped["Child"] = relationship("Child", back_populates="inventory")


class SyncQueue(Base):
    __tablename__ = "sync_queue"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    idempotency_key: Mapped[str] = mapped_column(String(120), unique=True)
    payload: Mapped[dict] = mapped_column(JSON)
    status: Mapped[str] = mapped_column(String(20), default="pending")
    created_at: Mapped[datetime] = mapped_column(DateTime, default=datetime.utcnow)
    processed_at: Mapped[Optional[datetime]] = mapped_column(DateTime, nullable=True)
    error_message: Mapped[Optional[str]] = mapped_column(String(255), nullable=True)
