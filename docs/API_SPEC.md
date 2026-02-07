# API Spec (FastAPI)

## Core Entities / Tables
- users: id, email, password_hash, role (child, parent, admin), created_at
- child_profiles: id, user_id, display_name, avatar_id, created_at
- parent_child_links: id, parent_user_id, child_profile_id
- sessions: id, child_profile_id, started_at, ended_at, offline_session_id
- skills: id, code, name_en, name_he, world, stage
- mastery: id, child_profile_id, skill_id, score, updated_at
- levels: id, world, stage, level_index, title_en, title_he
- level_results: id, session_id, level_id, stars, coins, accuracy, duration_ms, created_at
- review_results: id, session_id, skill_id, items_total, items_correct, created_at
- inventory: id, child_profile_id, item_id, owned_at
- economy_ledger: id, child_profile_id, reason, delta_coins, balance_after, created_at
- shop_items: id, set_name, name_en, name_he, category, price_coins
- sync_queue: id, child_profile_id, payload_json, status, created_at, processed_at

## Auth & Roles
- POST /auth/login
  - Body: email, password
  - Response: access_token, role, user_id
- POST /auth/parent/register
  - Body: email, password, child_display_name
  - Creates parent + child profile
- POST /auth/admin/register
  - Body: email, password, admin_invite_code

## Parent Endpoints
- GET /parent/children
- GET /parent/children/{child_id}/summary
- GET /parent/children/{child_id}/mastery
- GET /parent/children/{child_id}/inventory
- PATCH /parent/children/{child_id}/limits

## Admin Endpoints
- GET /admin/users
- GET /admin/skills
- POST /admin/skills
- GET /admin/levels
- POST /admin/levels
- POST /admin/content/version
- GET /admin/sync/queue

## Sync Endpoints
- POST /sync/upload
  - Body: offline_session_id, level_results[], review_results[], mastery_updates[], inventory_updates[], economy_events[]
  - Server validates and applies idempotently
- GET /sync/pull
  - Query: child_profile_id, since_timestamp
  - Returns: mastery, inventory, economy_balance, content_version

## Inventory & Economy
- GET /shop/items
- POST /shop/purchase
  - Body: child_profile_id, item_id
  - Validates balance, adds inventory, writes ledger
- GET /economy/balance

## Response Standards
- Use JSON with snake_case fields.
- Include `he_title` and `he_prompt` in content payloads as fallback fields.
