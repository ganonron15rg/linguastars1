# LinguaStars Server MVP

FastAPI + SQLAlchemy server for auth, parent/admin APIs, and offline-first sync.

## Requirements

- Python 3.11+
- SQLite (default) or Postgres via `DATABASE_URL`

## Setup

```bash
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```

## Configuration

Environment variables:

- `DATABASE_URL` (default: `sqlite:///./linguastars.db`)
- `JWT_SECRET` (default: `dev-secret-change-me`)
- `ACCESS_TOKEN_EXPIRE_MINUTES` (default: `60`)

## Migrations

```bash
alembic upgrade head
```

## Run locally

```bash
uvicorn app.main:app --reload
```

## Tests

```bash
pytest
```
