from datetime import datetime, timedelta

from fastapi.testclient import TestClient
from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker
from sqlalchemy.pool import StaticPool

from app.db import Base, get_db
from app.main import app


def setup_test_db():
    engine = create_engine(
        "sqlite://",
        connect_args={"check_same_thread": False},
        poolclass=StaticPool,
    )
    TestingSessionLocal = sessionmaker(bind=engine, expire_on_commit=False)
    Base.metadata.create_all(bind=engine)
    return TestingSessionLocal


def test_parent_flow_and_sync():
    TestingSessionLocal = setup_test_db()

    def override_get_db():
        db = TestingSessionLocal()
        try:
            yield db
        finally:
            db.close()

    app.dependency_overrides[get_db] = override_get_db

    client = TestClient(app)

    register_response = client.post(
        "/auth/register",
        json={"email": "parent@example.com", "password": "strongpass1", "role": "parent"},
    )
    assert register_response.status_code == 201

    login_response = client.post(
        "/auth/login",
        json={"email": "parent@example.com", "password": "strongpass1"},
    )
    assert login_response.status_code == 200
    token = login_response.json()["access_token"]

    headers = {"Authorization": f"Bearer {token}"}

    child_response = client.post(
        "/parent/children",
        json={"name": "Ava", "age": 7},
        headers=headers,
    )
    assert child_response.status_code == 200
    child_id = child_response.json()["id"]

    sync_payload = {
        "idempotency_key": "sync-1",
        "child_id": child_id,
        "progress": [
            {
                "level_id": "level-1",
                "mastery": 80,
                "attempts": 10,
                "errors": 2,
                "last_seen": datetime.utcnow().isoformat(),
            }
        ],
        "sessions": [
            {
                "date": (datetime.utcnow() - timedelta(days=1)).isoformat(),
                "duration_seconds": 900,
                "success_rate": 85,
                "activities_done": 5,
            }
        ],
        "economy_transactions": [
            {
                "type": "earn",
                "amount": 20,
                "meta_json": {"reason": "lesson"},
            }
        ],
        "inventory": {
            "coins_balance": 20,
            "owned_items_json": {"hat": True},
            "equipped_json": {"hat": True},
        },
    }

    sync_response = client.post("/sync/upload", json=sync_payload, headers=headers)
    assert sync_response.status_code == 200
    assert sync_response.json()["status"] == "processed"

    summary_response = client.get(f"/parent/child/{child_id}/summary", headers=headers)
    assert summary_response.status_code == 200
    assert summary_response.json()["progress_count"] == 1

    progress_response = client.get(f"/parent/child/{child_id}/progress", headers=headers)
    assert progress_response.status_code == 200
    assert len(progress_response.json()) == 1

    sessions_response = client.get(f"/parent/child/{child_id}/sessions", headers=headers)
    assert sessions_response.status_code == 200
    assert len(sessions_response.json()) == 1

    app.dependency_overrides.clear()


def test_admin_overview():
    TestingSessionLocal = setup_test_db()

    def override_get_db():
        db = TestingSessionLocal()
        try:
            yield db
        finally:
            db.close()

    app.dependency_overrides[get_db] = override_get_db

    client = TestClient(app)

    client.post(
        "/auth/register",
        json={"email": "admin@example.com", "password": "adminpass1", "role": "admin"},
    )

    login_response = client.post(
        "/auth/login",
        json={"email": "admin@example.com", "password": "adminpass1"},
    )
    token = login_response.json()["access_token"]
    headers = {"Authorization": f"Bearer {token}"}

    overview = client.get("/admin/overview", headers=headers)
    assert overview.status_code == 200
    assert "total_parents" in overview.json()

    app.dependency_overrides.clear()
