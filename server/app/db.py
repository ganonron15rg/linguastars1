from collections.abc import Generator
import os

from sqlalchemy import create_engine
from sqlalchemy.engine import Engine
from sqlalchemy.orm import DeclarativeBase, Session, sessionmaker

DEFAULT_SQLITE_URL = "sqlite:///./linguastars.db"
DATABASE_URL = os.getenv("DATABASE_URL", DEFAULT_SQLITE_URL)


def _create_engine() -> Engine:
    connect_args = {}
    if DATABASE_URL.startswith("sqlite"):
        connect_args = {"check_same_thread": False}
    return create_engine(DATABASE_URL, connect_args=connect_args, future=True)


engine = _create_engine()
SessionLocal = sessionmaker(bind=engine, class_=Session, expire_on_commit=False)


class Base(DeclarativeBase):
    pass


def get_db() -> Generator[Session, None, None]:
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()
