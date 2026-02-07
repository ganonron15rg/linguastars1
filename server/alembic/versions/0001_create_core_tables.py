"""create core tables

Revision ID: 0001_create_core_tables
Revises: 
Create Date: 2024-09-20 00:00:00.000000
"""

from alembic import op
import sqlalchemy as sa

revision = "0001_create_core_tables"
down_revision = None
branch_labels = None
depends_on = None


def upgrade() -> None:
    op.create_table(
        "users",
        sa.Column("id", sa.Integer(), primary_key=True),
        sa.Column("email", sa.String(length=320), nullable=False),
        sa.Column("password_hash", sa.String(length=255), nullable=False),
        sa.Column("role", sa.String(length=20), nullable=False),
        sa.Column("created_at", sa.DateTime(), nullable=False),
    )
    op.create_index(op.f("ix_users_email"), "users", ["email"], unique=True)

    op.create_table(
        "children",
        sa.Column("id", sa.Integer(), primary_key=True),
        sa.Column("parent_id", sa.Integer(), nullable=False),
        sa.Column("name", sa.String(length=120), nullable=False),
        sa.Column("age", sa.Integer(), nullable=False),
        sa.Column("created_at", sa.DateTime(), nullable=False),
        sa.ForeignKeyConstraint(["parent_id"], ["users.id"]),
    )
    op.create_index(op.f("ix_children_parent_id"), "children", ["parent_id"], unique=False)

    op.create_table(
        "progress",
        sa.Column("id", sa.Integer(), primary_key=True),
        sa.Column("child_id", sa.Integer(), nullable=False),
        sa.Column("level_id", sa.String(length=64), nullable=False),
        sa.Column("mastery", sa.Integer(), nullable=False),
        sa.Column("attempts", sa.Integer(), nullable=False),
        sa.Column("errors", sa.Integer(), nullable=False),
        sa.Column("last_seen", sa.DateTime(), nullable=True),
        sa.ForeignKeyConstraint(["child_id"], ["children.id"]),
        sa.UniqueConstraint("child_id", "level_id", name="uq_progress"),
    )
    op.create_index(op.f("ix_progress_child_id"), "progress", ["child_id"], unique=False)

    op.create_table(
        "sessions",
        sa.Column("id", sa.Integer(), primary_key=True),
        sa.Column("child_id", sa.Integer(), nullable=False),
        sa.Column("date", sa.DateTime(), nullable=False),
        sa.Column("duration_seconds", sa.Integer(), nullable=False),
        sa.Column("success_rate", sa.Integer(), nullable=False),
        sa.Column("activities_done", sa.Integer(), nullable=False),
        sa.ForeignKeyConstraint(["child_id"], ["children.id"]),
    )
    op.create_index(op.f("ix_sessions_child_id"), "sessions", ["child_id"], unique=False)

    op.create_table(
        "economy_transactions",
        sa.Column("id", sa.Integer(), primary_key=True),
        sa.Column("child_id", sa.Integer(), nullable=False),
        sa.Column("type", sa.String(length=20), nullable=False),
        sa.Column("amount", sa.Integer(), nullable=False),
        sa.Column("meta_json", sa.JSON(), nullable=False),
        sa.Column("timestamp", sa.DateTime(), nullable=False),
        sa.ForeignKeyConstraint(["child_id"], ["children.id"]),
    )
    op.create_index(
        op.f("ix_economy_transactions_child_id"),
        "economy_transactions",
        ["child_id"],
        unique=False,
    )

    op.create_table(
        "inventory",
        sa.Column("id", sa.Integer(), primary_key=True),
        sa.Column("child_id", sa.Integer(), nullable=False),
        sa.Column("coins_balance", sa.Integer(), nullable=False),
        sa.Column("owned_items_json", sa.JSON(), nullable=False),
        sa.Column("equipped_json", sa.JSON(), nullable=False),
        sa.ForeignKeyConstraint(["child_id"], ["children.id"]),
        sa.UniqueConstraint("child_id"),
    )

    op.create_table(
        "sync_queue",
        sa.Column("id", sa.Integer(), primary_key=True),
        sa.Column("idempotency_key", sa.String(length=120), nullable=False),
        sa.Column("payload", sa.JSON(), nullable=False),
        sa.Column("status", sa.String(length=20), nullable=False),
        sa.Column("created_at", sa.DateTime(), nullable=False),
        sa.Column("processed_at", sa.DateTime(), nullable=True),
        sa.Column("error_message", sa.String(length=255), nullable=True),
        sa.UniqueConstraint("idempotency_key"),
    )


def downgrade() -> None:
    op.drop_table("sync_queue")
    op.drop_table("inventory")
    op.drop_index(op.f("ix_economy_transactions_child_id"), table_name="economy_transactions")
    op.drop_table("economy_transactions")
    op.drop_index(op.f("ix_sessions_child_id"), table_name="sessions")
    op.drop_table("sessions")
    op.drop_index(op.f("ix_progress_child_id"), table_name="progress")
    op.drop_table("progress")
    op.drop_index(op.f("ix_children_parent_id"), table_name="children")
    op.drop_table("children")
    op.drop_index(op.f("ix_users_email"), table_name="users")
    op.drop_table("users")
